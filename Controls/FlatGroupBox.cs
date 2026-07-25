using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Flat Win11-style GroupBox with card appearance (rounded corners, flat border).
/// Replaces the default etched 3D border with clean card styling.
/// </summary>
public class FlatGroupBox : GroupBox
{
    public FlatGroupBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        Font = ThemeManager.Typography.BodyBold;
        ForeColor = ThemeManager.TextPrimary;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Win11Renderer.BeginHighQuality(e.Graphics);

        var rect = new Rectangle(0, 8, Width - 1, Height - 9);
        int radius = ThemeManager.Spacing.CornerRadius / 2;

        // Card background
        using var bgBrush = new SolidBrush(ThemeManager.SurfaceCard);
        Win11Renderer.FillRoundedRect(e.Graphics, rect, radius, bgBrush);

        // 1px border
        using var borderPen = new Pen(ThemeManager.ControlInputBorder, 1f);
        Win11Renderer.DrawRoundedRect(e.Graphics, rect, radius, borderPen);

        // Title text at top-left, with background to cover the border
        if (!string.IsNullOrEmpty(Text))
        {
            var titleSize = TextRenderer.MeasureText(e.Graphics, Text,
                ThemeManager.Typography.BodyBold, new Size(Width, 20));
            int titleX = ThemeManager.Spacing.PaddingStandard;
            int titleW = titleSize.Width + 10;

            // Cover the border behind the title text
            using var titleBgBrush = new SolidBrush(ThemeManager.SurfaceCard);
            e.Graphics.FillRectangle(titleBgBrush, titleX, 2, titleW, 14);

            TextRenderer.DrawText(e.Graphics, Text, ThemeManager.Typography.BodyBold,
                new Rectangle(titleX + 4, 0, titleW, 18), ThemeManager.TextPrimary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        Win11Renderer.EndHighQuality(e.Graphics);
    }
}
