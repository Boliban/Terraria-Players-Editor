using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// PictureBox with a 1px theme-aware border.
/// BorderStyle.FixedSingle is drawn by the system (white in dark mode,
/// black in light mode) and cannot follow the app theme, so the border
/// is painted manually with ThemeManager.ControlInputBorder.
/// </summary>
public class FlatPictureBox : PictureBox
{
    public FlatPictureBox()
    {
        BorderStyle = BorderStyle.None;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Win11Renderer.DrawThemedBorder(e.Graphics, this);
    }
}
