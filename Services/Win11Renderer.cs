using System.Drawing.Drawing2D;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// GDI+ helper methods for flat Win11-style rendering.
/// All drawing uses rounded rectangles, solid colors, and anti-aliased edges.
/// No 3D bevels, gradients, or system-theme rendering.
/// </summary>
public static class Win11Renderer
{
    /// <summary>Get a rounded rectangle GraphicsPath.</summary>
    public static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        int r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
        if (r <= 0)
        {
            var path = new GraphicsPath();
            path.AddRectangle(rect);
            return path;
        }

        var diameter = r * 2;
        var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
        var gp = new GraphicsPath();

        // Top-left arc
        gp.AddArc(arc, 180, 90);
        // Top-right arc
        arc.X = rect.Right - diameter;
        gp.AddArc(arc, 270, 90);
        // Bottom-right arc
        arc.Y = rect.Bottom - diameter;
        gp.AddArc(arc, 0, 90);
        // Bottom-left arc
        arc.X = rect.X;
        gp.AddArc(arc, 90, 90);

        gp.CloseFigure();
        return gp;
    }

    /// <summary>Fill a rounded rectangle.</summary>
    public static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Brush brush)
    {
        using var path = GetRoundedRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    /// <summary>Draw a rounded rectangle border.</summary>
    public static void DrawRoundedRect(Graphics g, Rectangle rect, int radius, Pen pen)
    {
        using var path = GetRoundedRectPath(rect, radius);
        g.DrawPath(pen, path);
    }

    /// <summary>Set up high-quality GDI+ rendering (anti-aliased, high-quality pixel offset).</summary>
    public static void BeginHighQuality(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }

    /// <summary>Restore default rendering quality.</summary>
    public static void EndHighQuality(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
    }

    /// <summary>Draw a flat button surface with text centered.</summary>
    public static void DrawFlatButton(Graphics g, Rectangle rect, int radius,
        Brush background, Brush foreground, string text, Font font)
    {
        BeginHighQuality(g);
        FillRoundedRect(g, rect, radius, background);
        DrawRoundedRect(g, rect, radius, GetPen(ThemeManager.ControlInputBorder));
        EndHighQuality(g);

        TextRenderer.DrawText(g, text, font, rect,
            ThemeManager.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    /// <summary>Draw a card-style surface (used for GroupBox, ItemModifier backgrounds).</summary>
    public static void DrawCard(Graphics g, Rectangle rect, int radius, Color fill, Color border)
    {
        BeginHighQuality(g);
        using var fillBrush = new SolidBrush(fill);
        FillRoundedRect(g, rect, radius, fillBrush);

        using var borderPen = new Pen(border, 1f);
        DrawRoundedRect(g, rect, radius, borderPen);
        EndHighQuality(g);
    }

    /// <summary>Draw the selection accent rectangle (e.g., selected slot border).</summary>
    public static void DrawAccentSelection(Graphics g, Rectangle rect, int radius,
        Color accentColor, int thickness)
    {
        BeginHighQuality(g);
        using var pen = new Pen(accentColor, thickness);
        DrawRoundedRect(g, rect, radius, pen);
        EndHighQuality(g);
    }

    /// <summary>Draw a flat tab header with Win11-style selection indicator.</summary>
    public static void DrawTabHeader(Graphics g, Rectangle rect, string text, Font font,
        bool isSelected, float selectionIndicatorX, float selectionIndicatorWidth)
    {
        var bg = isSelected
            ? ThemeManager.SurfaceContainer
            : ThemeManager.SurfaceBackground;
        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, rect);

        // Selection indicator: 3px accent bar at bottom (animated position/width)
        if (isSelected)
        {
            int indicatorY = rect.Bottom - 4;
            var indicatorRect = new RectangleF(
                selectionIndicatorX,
                indicatorY,
                selectionIndicatorWidth,
                3f);
            using var accentBrush = new SolidBrush(ThemeManager.AccentPrimary);
            g.FillRectangle(accentBrush, indicatorRect);
        }

        TextRenderer.DrawText(g, text, font, rect,
            isSelected ? ThemeManager.TextPrimary : ThemeManager.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    /// <summary>Draw a 1px themed border around a control's client area (replaces system FixedSingle, which ignores the app theme).</summary>
    public static void DrawThemedBorder(Graphics g, Control control)
    {
        // Guards against layout passes that paint the control at 0 size,
        // where DrawRectangle would throw with negative width/height.
        if (control.Width <= 0 || control.Height <= 0) return;
        // Use a local pen — ThemeManager.GetPen returns a CACHED pen shared
        // across callers; disposing it here would poison the cache for everyone.
        using var pen = new Pen(ThemeManager.ControlInputBorder, 1f);
        g.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
    }

    /// <summary>Get a cached Pen from ThemeManager for common colors.</summary>
    private static Pen GetPen(Color color)
    {
        return ThemeManager.GetPen(color);
    }
}
