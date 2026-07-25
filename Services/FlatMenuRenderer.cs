namespace Terraria_Players_Editor.Services;

/// <summary>
/// Flat ToolStrip renderer for Win11-style MenuStrip and StatusStrip.
/// Replaces the default 3D-rendered menus with clean flat surfaces.
/// </summary>
public class FlatMenuRenderer : ToolStripProfessionalRenderer
{
    public FlatMenuRenderer() : base(new FlatColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            var rect = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
            using var brush = new SolidBrush(
                e.Item.Pressed
                    ? ThemeManager.ControlButtonPressed
                    : ThemeManager.ControlButtonHover);
            e.Graphics.FillRectangle(brush, rect);
        }
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(ThemeManager.SurfaceBackground);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // No border for flat design
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(ThemeManager.ControlInputBorder, 1f);
        e.Graphics.DrawLine(pen, e.Item.Bounds.Left + 8, y, e.Item.Bounds.Right - 8, y);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Flat margin (no gradient)
    }
}

/// <summary>
/// Flat color table for ProfessionalColorTable overrides.
/// </summary>
internal class FlatColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => ThemeManager.ControlButtonHover;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuBorder => Color.Transparent;
    public override Color MenuStripGradientBegin => ThemeManager.SurfaceBackground;
    public override Color MenuStripGradientEnd => ThemeManager.SurfaceBackground;
    public override Color ToolStripBorder => ThemeManager.ControlInputBorder;
    public override Color ToolStripDropDownBackground => ThemeManager.SurfaceRaised;
    public override Color ToolStripGradientBegin => ThemeManager.SurfaceBackground;
    public override Color ToolStripGradientEnd => ThemeManager.SurfaceBackground;
    public override Color ToolStripGradientMiddle => ThemeManager.SurfaceBackground;
    public override Color ImageMarginGradientBegin => ThemeManager.SurfaceBackground;
    public override Color ImageMarginGradientEnd => ThemeManager.SurfaceBackground;
    public override Color ImageMarginGradientMiddle => ThemeManager.SurfaceBackground;
    public override Color StatusStripGradientBegin => ThemeManager.SurfaceBackground;
    public override Color StatusStripGradientEnd => ThemeManager.SurfaceBackground;
    public override Color ButtonSelectedHighlight => ThemeManager.ControlButtonHover;
    public override Color ButtonPressedHighlight => ThemeManager.ControlButtonPressed;
    public override Color ButtonCheckedHighlight => ThemeManager.ControlButtonHover;
    public override Color CheckBackground => ThemeManager.ControlButtonHover;
    public override Color CheckSelectedBackground => ThemeManager.AccentPrimary;
    public override Color CheckPressedBackground => ThemeManager.AccentPressed;
    public override Color SeparatorDark => ThemeManager.ControlInputBorder;
    public override Color SeparatorLight => ThemeManager.ControlInputBorder;
}
