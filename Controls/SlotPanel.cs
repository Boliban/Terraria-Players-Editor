using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// A single inventory-style slot displaying an item icon, stack count, and selection state.
/// Uses clean square shape with minimal gray borders. Animates fill color on hover/select.
/// </summary>
public class SlotPanel : UserControl
{
    private readonly PictureBox _icon;
    private readonly Label _stackLabel;
    private bool _selected;
    private int _slotIndex;
    private ItemData? _item;
    private Color _normalBackColor;
    private Color _emptyBackColor;

    // Animation support
    private System.Windows.Forms.Timer? _animTimer;
    private Bitmap[]? _animFrames;
    private int _animFrameIdx;

    // Hover state
    private bool _hovered;
    private bool _isHotbar;

    // Smooth color transition
    private Color _currentFill;
    private Animation? _fillAnimation;

    public SlotPanel(int slotIndex = 0, bool isHotbar = false)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();

        _slotIndex = slotIndex;
        _isHotbar = isHotbar;
        _normalBackColor = isHotbar
            ? ThemeManager.SlotHotbar
            : ThemeManager.SlotNormal;
        _emptyBackColor = _normalBackColor;
        _currentFill = _normalBackColor;

        Size = new Size(48, 48);
        BackColor = _normalBackColor;
        BorderStyle = BorderStyle.None;
        Cursor = Cursors.Hand;
        Margin = new Padding(0);

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
            Font = ThemeManager.Typography.SlotStack,
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
        ThemeManager.ThemeChanged += () => ApplyTheme();
    }

    /// <summary>Re-read cached theme colors and invalidate for repaint.</summary>
    public void ApplyTheme()
    {
        _normalBackColor = _isHotbar
            ? ThemeManager.SlotHotbar
            : ThemeManager.SlotNormal;
        _emptyBackColor = _normalBackColor;
        BackColor = _normalBackColor;
        _currentFill = GetTargetFill();
        Invalidate();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SlotIndex
    {
        get => _slotIndex;
        set => _slotIndex = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            TransitionFill(GetTargetFill());
        }
    }

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsHotbar
    {
        set
        {
            _isHotbar = value;
            _normalBackColor = value
                ? ThemeManager.SlotHotbar
                : ThemeManager.SlotNormal;
            _emptyBackColor = _normalBackColor;
            if (!_selected) TransitionFill(GetTargetFill());
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBuffSlot { get; set; }

    public void RefreshDisplay()
    {
        StopAnimation();

        if (_item == null || _item.IsEmpty)
        {
            _icon.Image = IconService.DefaultIcon;
            _stackLabel.Visible = false;
            TransitionFill(GetTargetFill());
            return;
        }

        if (!IsBuffSlot && SettingsManager.EnableAnimatedIcons)
        {
            var frames = IconService.GetItemFrames(_item.ItemId);
            if (frames != null && frames.Length > 1)
            {
                _animFrames = frames;
                _animFrameIdx = 0;
                _icon.Image = frames[0];
                StartAnimation();
            }
            else
            {
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
        _stackLabel.Font = _item.StackSize >= 1000
            ? ThemeManager.Typography.SlotStackSmall
            : ThemeManager.Typography.SlotStack;
        _stackLabel.Text = _item.StackSize.ToString();
        _stackLabel.Location = new Point(Width - _stackLabel.PreferredWidth - 2,
            Height - _stackLabel.PreferredHeight);
        TransitionFill(GetTargetFill());
    }

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
    }

    private void StopAnimation()
    {
        if (_animTimer != null)
        {
            _animTimer.Stop();
            _animTimer.Dispose();
            _animTimer = null;
        }
        _animFrames = null;
        _animFrameIdx = 0;
    }

    private void AnimTick(object? sender, EventArgs e)
    {
        if (_animFrames == null || _animFrames.Length == 0) return;
        _animFrameIdx = (_animFrameIdx + 1) % _animFrames.Length;
        _icon.Image = _animFrames[_animFrameIdx];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        BackColor = _currentFill;

        // Fill entire control with solid color
        using (var fillBrush = new SolidBrush(_currentFill))
            e.Graphics.FillRectangle(fillBrush, ClientRectangle);

        // Child controls on top
        base.OnPaint(e);

        // Simple rect border — gray, thicker when selected
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        float borderW = _selected ? 2f : 1f;
        using var borderPen = new Pen(ThemeManager.ControlInputBorder, borderW);
        e.Graphics.DrawRectangle(borderPen, rect);
    }

    private void TransitionFill(Color target)
    {
        _fillAnimation?.Cancel();
        _fillAnimation = AnimationEngine.Instance.AnimateColor(
            _currentFill, target, 200, EasingFunction.EaseOutCubic,
            c => { _currentFill = c; Invalidate(); });
    }

    private Color GetTargetFill()
    {
        if (_selected) return ThemeManager.SlotSelectedFill;
        if (_hovered) return ThemeManager.SlotHover;
        if (_item != null && !_item.IsEmpty) return _normalBackColor;
        return _emptyBackColor;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        TransitionFill(GetTargetFill());
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        TransitionFill(GetTargetFill());
    }
}
