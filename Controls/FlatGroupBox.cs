using System.ComponentModel;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Section container with bold title, GroupBox-style border, and content.
/// Border is drawn as the outermost layer to prevent occlusion by children.
/// </summary>
public class FlatGroupBox : Panel
{
    private readonly Label _titleLabel;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DividerColor { get; set; } = Color.Black;

    public FlatGroupBox()
    {
        BorderStyle = BorderStyle.None;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(1);

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = ThemeManager.Typography.BodyBold,
            ForeColor = ThemeManager.TextPrimary,
            Location = new Point(1, 1)
        };
        Controls.Add(_titleLabel);

        // ThemeChanged fires in Program.Main BEFORE any control exists, so initialize colors
        // from the current theme here; the handler below covers any future runtime switches.
        DividerColor = ThemeManager.ControlInputBorder;
        _titleLabel.ForeColor = ThemeManager.TextPrimary;

        ThemeManager.ThemeChanged += () =>
        {
            _titleLabel.ForeColor = ThemeManager.TextPrimary;
            DividerColor = ThemeManager.ControlInputBorder;
            Invalidate();
        };
    }

    public override string Text
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value ?? "";
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control != _titleLabel)
        {
            e.Control.Location = new Point(1, _titleLabel.Bottom + 3);
            _titleLabel.SendToBack();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Draw border FIRST (behind children via Padding)
        base.OnPaint(e);

        var color = DividerColor;
        using var pen = new Pen(color, 1f);
        var titleSize = TextRenderer.MeasureText(_titleLabel.Text, _titleLabel.Font);
        int gapL = _titleLabel.Left, gapR = gapL + titleSize.Width + 4;
        int w = Width - 1, h = Height - 1, topY = _titleLabel.Top + titleSize.Height / 2;
        e.Graphics.DrawLine(pen, 0, topY, 0, h);
        e.Graphics.DrawLine(pen, w, topY, w, h);
        e.Graphics.DrawLine(pen, 0, h, w, h);
        e.Graphics.DrawLine(pen, 0, topY, gapL, topY);
        e.Graphics.DrawLine(pen, gapR, topY, w, topY);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Invalidate();
    }
}
