using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Flat Win11-style Button with rounded corners and smooth hover/press states.
/// No 3D bevels, no gradients — clean flat design with theme-aware colors.
/// </summary>
public class FlatButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private Color _currentBg;
    private Animation? _bgAnimation;

    public FlatButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = ThemeManager.ControlButtonBg;
        ForeColor = ThemeManager.TextPrimary;
        Font = ThemeManager.Typography.Body;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        _currentBg = ThemeManager.ControlButtonBg;
        ThemeManager.ThemeChanged += () => ApplyTheme();
    }

    /// <summary>Re-apply theme colors and invalidate.</summary>
    public void ApplyTheme()
    {
        BackColor = ThemeManager.ControlButtonBg;
        ForeColor = ThemeManager.TextPrimary;
        _currentBg = ThemeManager.ControlButtonBg;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Win11Renderer.BeginHighQuality(e.Graphics);

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        int radius = ThemeManager.Spacing.CornerRadius / 2;

        using var bgBrush = new SolidBrush(_currentBg);
        Win11Renderer.FillRoundedRect(e.Graphics, rect, radius, bgBrush);

        using var borderPen = new Pen(ThemeManager.ControlInputBorder, 1f);
        Win11Renderer.DrawRoundedRect(e.Graphics, rect, radius, borderPen);

        Win11Renderer.EndHighQuality(e.Graphics);

        // Draw text centered
        TextRenderer.DrawText(e.Graphics, Text, ThemeManager.Typography.Body,
            ClientRectangle, ThemeManager.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void TransitionBg(Color target)
    {
        _bgAnimation?.Cancel();
        _bgAnimation = AnimationEngine.Instance.AnimateColor(
            _currentBg, target, 150, EasingFunction.EaseOutCubic,
            c => { _currentBg = c; Invalidate(); });
    }

    private Color GetTargetBg()
    {
        if (_isPressed) return ThemeManager.ControlButtonPressed;
        if (_isHovered) return ThemeManager.ControlButtonHover;
        return ThemeManager.ControlButtonBg;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        TransitionBg(GetTargetBg());
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        TransitionBg(GetTargetBg());
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _isPressed = true;
        TransitionBg(GetTargetBg());
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isPressed = false;
        TransitionBg(GetTargetBg());
    }
}
