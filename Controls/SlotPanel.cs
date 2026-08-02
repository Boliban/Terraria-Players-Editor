using System.ComponentModel;
using System.Drawing.Drawing2D;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// A single inventory-style slot displaying an item icon, stack count, and selection state.
/// Uses clean square shape with minimal gray borders. Animates fill color on hover/select.
/// </summary>
public class SlotPanel : UserControl
{
    private Image? _iconImage;
    private bool _showStack;
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

        Click += (s, e) => DebugLog.Log($"[SlotPanel] Click slotIdx={_slotIndex}");
        MouseClick += (s, e) => DebugLog.Log($"[SlotPanel] MouseClick slotIdx={_slotIndex} btn={e.Button}");

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
            _iconImage = IconService.DefaultIcon;
            _showStack = false;
            TransitionFill(GetTargetFill());
            Invalidate();
            return;
        }

        if (!IsBuffSlot && SettingsManager.EnableAnimatedIcons)
        {
            var frames = IconService.GetItemFrames(_item.ItemId);
            if (frames != null && frames.Length > 1)
            {
                _animFrames = frames;
                _animFrameIdx = 0;
                _iconImage = frames[0];
                StartAnimation();
            }
            else
            {
                _iconImage = IconService.GetItemIcon(_item.ItemId) ?? IconService.DefaultIcon;
            }
        }
        else
        {
            _iconImage = IsBuffSlot
                ? (IconService.GetBuffIcon(_item.ItemId) ?? IconService.DefaultIcon)
                : (IconService.GetItemIcon(_item.ItemId) ?? IconService.DefaultIcon);
        }

        _showStack = _item.StackSize > 1;
        TransitionFill(GetTargetFill());
        Invalidate();
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
        _iconImage = _animFrames[_animFrameIdx];
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        DebugLog.Log($"[SlotPanel] OnClick override slotIdx={_slotIndex}");
        base.OnClick(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        DebugLog.Log($"[SlotPanel] OnMouseDown slotIdx={_slotIndex} btn={e.Button}");
        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        BackColor = _currentFill;

        // Fill entire control with solid color
        using (var fillBrush = new SolidBrush(_currentFill))
            e.Graphics.FillRectangle(fillBrush, ClientRectangle);

        // Item icon (32x32 box at top-left)
        if (_iconImage != null)
            e.Graphics.DrawImage(_iconImage, 8, 8, 32, 32);

        // Stack count drawn directly over the icon with NO background, plus a
        // smooth 2px round-join outline so the number stays readable in both
        // light and dark themes over any icon color.
        if (_showStack && _item != null && _item.StackSize > 1)
        {
            var text = _item.StackSize.ToString();
            var font = _item.StackSize >= 1000
                ? ThemeManager.Typography.SlotStackSmall
                : ThemeManager.Typography.SlotStack;
            // Outline/fill colors swapped per request: dark = black text with
            // white outline, light = white text with black outline
            var fillColor = ThemeManager.IsDarkMode ? Color.Black : Color.White;
            var outlineColor = ThemeManager.IsDarkMode ? Color.White : Color.Black;

            // Build the glyph outline path (emSize in pixels), then stroke + fill
            using var path = new GraphicsPath();
            float emSize = font.SizeInPoints * e.Graphics.DpiY / 72f;
            path.AddString(text, font.FontFamily, (int)font.Style, emSize,
                PointF.Empty, StringFormat.GenericTypographic);
            var bounds = path.GetBounds();
            // Bottom-left corner, clear of the border ring: the 2px outline
            // extends 1px past the glyph bounds, and the selected border is 2px,
            // so use a 3px left / 4px bottom margin.
            using var tx = new Matrix();
            tx.Translate(3 - bounds.X, Height - bounds.Height - 4 - bounds.Y);
            path.Transform(tx);

            var oldMode = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var outlinePen = new Pen(outlineColor, 2f) { LineJoin = LineJoin.Round };
            e.Graphics.DrawPath(outlinePen, path);
            using var fillBrush = new SolidBrush(fillColor);
            e.Graphics.FillPath(fillBrush, path);
            e.Graphics.SmoothingMode = oldMode;
        }

        // Simple rect border — gray normally, themed yellow when selected.
        // The pen is centered on the rectangle line: the 2px selected border is
        // drawn on an inset rect so all four sides get a full 2px (an edge rect
        // would clip the left/top halves, making them 1px vs 2px on right/bottom).
        var rect = _selected
            ? new Rectangle(1, 1, Width - 2, Height - 2)
            : new Rectangle(0, 0, Width - 1, Height - 1);
        float borderW = _selected ? 2f : 1f;
        var borderColor = _selected ? ThemeManager.SlotSelectedBorder : ThemeManager.ControlInputBorder;
        using var borderPen = new Pen(borderColor, borderW);
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
