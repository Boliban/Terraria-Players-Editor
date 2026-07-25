using System.ComponentModel;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Section container with bold title, divider line, and content.
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

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = ThemeManager.Typography.BodyBold,
            ForeColor = ThemeManager.TextPrimary,
            Location = new Point(0, 0)
        };
        Controls.Add(_titleLabel);

        ThemeManager.ThemeChanged += () =>
        {
            _titleLabel.ForeColor = ThemeManager.TextPrimary;
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
            e.Control.Location = new Point(0, _titleLabel.Bottom + 3);
            _titleLabel.SendToBack();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_titleLabel.Visible)
        {
            int y = _titleLabel.Bottom + 1;
            using var pen = new Pen(DividerColor, 1f);
            e.Graphics.DrawLine(pen, 0, y, Width - 1, y);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Invalidate();
    }
}
