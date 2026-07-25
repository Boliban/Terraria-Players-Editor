using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Minimal section container — inherits Panel for compatibility but adds no border.
/// Provides a bold title label above the content.
/// </summary>
public class FlatGroupBox : Panel
{
    private readonly Label _titleLabel;

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
            // Position added content below the title label
            e.Control.Location = new Point(0, _titleLabel.Bottom + 2);
            // Push title to back so it doesn't cover content
            _titleLabel.SendToBack();
        }
    }
}
