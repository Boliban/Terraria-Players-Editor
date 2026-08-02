using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Panel with a 1px theme-aware border, used to wrap controls whose native
/// borders (e.g. DataGridView BorderStyle.FixedSingle) are system-drawn and
/// ignore the app theme. Keep 1px padding so children don't cover the border.
/// </summary>
public class ThemedBorderPanel : Panel
{
    public ThemedBorderPanel()
    {
        BorderStyle = BorderStyle.None;
        Padding = new Padding(1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Win11Renderer.DrawThemedBorder(e.Graphics, this);
    }
}
