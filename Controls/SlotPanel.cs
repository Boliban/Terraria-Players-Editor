using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// A single inventory-style slot displaying an item icon, stack count, and selection state.
/// Mimics Terraria's slot appearance with dark backgrounds and color-coded borders.
/// Supports animated item icons via a built-in timer.
/// </summary>
public class SlotPanel : UserControl
{
    private readonly PictureBox _icon;
    private readonly Label _stackLabel;
    private bool _selected;
    private int _slotIndex;
    private ItemData? _item;
    private Color _normalBackColor;
    private readonly Color _emptyBackColor;

    // Animation support
    private System.Windows.Forms.Timer? _animTimer;
    private Bitmap[]? _animFrames;
    private int _animFrameIdx;

    public SlotPanel(int slotIndex = 0, bool isHotbar = false)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();

        _slotIndex = slotIndex;
        _normalBackColor = isHotbar
            ? Color.FromArgb(80, 60, 40)   // Warm brown - hotbar
            : Color.FromArgb(40, 35, 45);  // Dark purple-gray - normal
        _emptyBackColor = _normalBackColor; // Empty slots match filled slot colors

        Size = new Size(48, 48);
        BackColor = _normalBackColor;
        BorderStyle = BorderStyle.FixedSingle;
        Cursor = Cursors.Hand;
        Margin = new Padding(1);

        _icon = new PictureBox
        {
            Size = new Size(32, 32),
            Location = new Point(8, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Enabled = false
        };

        _stackLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomRight,
            Visible = false
        };

        Controls.Add(_icon);
        Controls.Add(_stackLabel);

        _icon.MouseClick += (s, e) => OnMouseClick(e);
        _icon.MouseDoubleClick += (s, e) => OnMouseDoubleClick(e);
        _stackLabel.MouseClick += (s, e) => OnMouseClick(e);
        _stackLabel.MouseDoubleClick += (s, e) => OnMouseDoubleClick(e);

        Disposed += (s, e) => StopAnimation();
    }

    /// <summary>Index of this slot within its parent grid.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SlotIndex
    {
        get => _slotIndex;
        set => _slotIndex = value;
    }

    /// <summary>Whether this slot is currently selected (gold border).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            BackColor = value ? Color.Gold : _normalBackColor;
        }
    }

    /// <summary>The item data displayed in this slot.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ItemData? Item
    {
        get => _item;
        set
        {
            _item = value;
            RefreshDisplay();
        }
    }

    /// <summary>Whether this is a hotbar slot (special background color).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsHotbar
    {
        set
        {
            _normalBackColor = value
                ? Color.FromArgb(80, 60, 40)
                : Color.FromArgb(40, 35, 45);
            if (!_selected) BackColor = _normalBackColor;
        }
    }

    /// <summary>Whether this slot displays a buff (uses buff icons instead of item icons).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBuffSlot { get; set; }

    /// <summary>Update the icon and stack label from current Item data.</summary>
    public void RefreshDisplay()
    {
        StopAnimation();

        if (_item == null || _item.IsEmpty)
        {
            _icon.Image = IconService.DefaultIcon;
            _stackLabel.Visible = false;
            BackColor = _selected ? Color.Gold : _emptyBackColor;
            return;
        }

        // Check for animation frames first
        if (!IsBuffSlot && SettingsManager.EnableAnimatedIcons)
        {
            var frames = IconService.GetItemFrames(_item.ItemId);
            if (frames != null && frames.Length > 1)
            {
                DebugLog.Log(
                    $"[SlotPanel] Anim start ID={_item.ItemId}, frames={frames.Length}, enabled={SettingsManager.EnableAnimatedIcons}");
                _animFrames = frames;
                _animFrameIdx = 0;
                _icon.Image = frames[0];
                StartAnimation();
            }
            else
            {
                DebugLog.Log(
                    $"[SlotPanel] No anim ID={_item.ItemId}, frames={frames?.Length ?? 0}, enabled={SettingsManager.EnableAnimatedIcons}");
                _icon.Image = IconService.GetItemIcon(_item.ItemId) ?? IconService.DefaultIcon;
            }
        }
        else
        {
            _icon.Image = IsBuffSlot
                ? (IconService.GetBuffIcon(_item.ItemId) ?? IconService.DefaultIcon)
                : (IconService.GetItemIcon(_item.ItemId) ?? IconService.DefaultIcon);
        }

        _stackLabel.Visible = _item.StackSize > 1;
        if (_item.StackSize >= 1000)
        {
            _stackLabel.Font = new Font("Segoe UI", 5.5f, FontStyle.Bold);
            _stackLabel.Text = _item.StackSize.ToString();
        }
        else
        {
            _stackLabel.Font = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            _stackLabel.Text = _item.StackSize.ToString();
        }
        _stackLabel.Location = new Point(Width - _stackLabel.PreferredWidth - 2,
            Height - _stackLabel.PreferredHeight);
        if (!_selected) BackColor = _normalBackColor;
    }

    /// <summary>Clear this slot to empty state.</summary>
    public void Clear()
    {
        _item = null;
        RefreshDisplay();
    }

    private void StartAnimation()
    {
        if (_animTimer != null) return;
        _animTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _animTimer.Tick += AnimTick;
        _animTimer.Start();
        DebugLog.Log($"[SlotPanel] Timer started, interval=150ms");
    }

    private void StopAnimation()
    {
        if (_animTimer != null)
        {
            _animTimer.Stop();
            _animTimer.Dispose();
            _animTimer = null;
            DebugLog.Log($"[SlotPanel] Timer stopped");
        }
        _animFrames = null;
        _animFrameIdx = 0;
    }

    private void AnimTick(object? sender, EventArgs e)
    {
        if (_animFrames == null || _animFrames.Length == 0) return;
        _animFrameIdx = (_animFrameIdx + 1) % _animFrames.Length;
        _icon.Image = _animFrames[_animFrameIdx];
        DebugLog.Log($"[SlotPanel] Frame {_animFrameIdx}/{_animFrames.Length}");
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!_selected)
            BackColor = Color.FromArgb(60, 80, 100); // Blue hover
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_selected)
        {
            // Only restore color if mouse truly left the control bounds
            var mousePos = PointToClient(MousePosition);
            if (!ClientRectangle.Contains(mousePos))
                BackColor = _item != null && !_item.IsEmpty ? _normalBackColor : _emptyBackColor;
        }
    }
}
