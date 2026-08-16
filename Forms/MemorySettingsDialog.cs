using Terraria_Players_Editor.Services;
using Terraria_Players_Editor.Services.Memory;

namespace Terraria_Players_Editor.Forms;

/// <summary>
/// Advanced settings for the memory editor: the Player pointer chain and the
/// CSX field offsets (defaults target vanilla Terraria 1.4.5.6). Saved to
/// %AppData%/TerrariaPlayersEditor/memory_settings.json.
/// </summary>
public sealed class MemorySettingsDialog : Form
{
    private readonly bool _connected;
    private TextBox _txtStackSubtract = null!;
    private TextBox _txtChain = null!;
    private CheckBox _chkFinalDeref = null!;
    private readonly Dictionary<string, TextBox> _offsetBoxes = new();
    private Label _lblBase = null!;

    public MemorySettingsDialog(bool connected)
    {
        _connected = connected;
        Text = AppLocale.Get("MemEdit.SettingsTitle");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(680, 780);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = ThemeManager.Typography.Body;
        BackColor = ThemeManager.SurfaceBackground;
        AutoScroll = true;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var hint = new Label
        {
            Text = AppLocale.Get("MemEdit.SettingsHint"),
            AutoSize = false,
            Height = 40,
            MaximumSize = new Size(600, 0),
            ForeColor = ThemeManager.TextSecondary,
            Tag = "secondary",
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(hint, 0, 0);
        layout.SetColumnSpan(hint, 2);

        _lblBase = new Label
        {
            Text = _connected ? AppLocale.Get("MemEdit.SettingsBaseLive") : AppLocale.Get("MemEdit.SettingsBaseOffline"),
            AutoSize = false,
            Height = 24,
            ForeColor = ThemeManager.TextSecondary,
            Tag = "secondary",
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(_lblBase, 0, 1);
        layout.SetColumnSpan(_lblBase, 2);

        // === Chain group ===
        var chainGroup = new FlatGroupBox2 { Text = AppLocale.Get("MemEdit.Chain"), Margin = new Padding(0, 0, 6, 10), Padding = new Padding(8) };
        var chainLayout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Dock = DockStyle.Top };
        chainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        chainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtStackSubtract = MakeHexBox(MemorySettings.ChainStackSubtract);
        _txtChain = MakeText(MemorySettings.ChainToString(MemorySettings.ChainOffsets));
        _chkFinalDeref = new CheckBox { Text = AppLocale.Get("MemEdit.ChainFinalDeref"), Checked = MemorySettings.ChainFinalDeref, AutoSize = true };

        chainLayout.Controls.Add(MakeFieldLabel(AppLocale.Get("MemEdit.ChainStackSubtract")), 0, 0);
        chainLayout.Controls.Add(_txtStackSubtract, 1, 0);
        chainLayout.Controls.Add(MakeFieldLabel(AppLocale.Get("MemEdit.ChainOffsets")), 0, 1);
        chainLayout.Controls.Add(_txtChain, 1, 1);
        chainLayout.Controls.Add(_chkFinalDeref, 1, 2);
        chainGroup.Controls.Add(chainLayout);
        layout.Controls.Add(chainGroup, 0, 2);

        // === Offsets group ===
        var offsetGroup = new FlatGroupBox2 { Text = AppLocale.Get("MemEdit.Offsets"), Margin = new Padding(6, 0, 0, 10), Padding = new Padding(8) };
        var offsetLayout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Dock = DockStyle.Top };
        offsetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        offsetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        void AddOffset(string key, uint value, string label)
        {
            _offsetBoxes[key] = MakeHexBox(value);
            offsetLayout.Controls.Add(MakeFieldLabel(label), 0, row);
            offsetLayout.Controls.Add(_offsetBoxes[key], 1, row);
            row++;
        }

        var o = MemorySettings.Offsets;
        AddOffset("inventory", o.Inventory, AppLocale.Get("MemEdit.Off.Inventory"));
        AddOffset("armor", o.Armor, AppLocale.Get("MemEdit.Off.Armor"));
        AddOffset("dye", o.Dye, AppLocale.Get("MemEdit.Off.Dye"));
        AddOffset("miscEquips", o.MiscEquips, AppLocale.Get("MemEdit.Off.MiscEquips"));
        AddOffset("miscDyes", o.MiscDyes, AppLocale.Get("MemEdit.Off.MiscDyes"));
        AddOffset("bank", o.Bank, AppLocale.Get("MemEdit.Off.Bank"));
        AddOffset("bank2", o.Bank2, AppLocale.Get("MemEdit.Off.Bank2"));
        AddOffset("bank3", o.Bank3, AppLocale.Get("MemEdit.Off.Bank3"));
        AddOffset("bank4", o.Bank4, AppLocale.Get("MemEdit.Off.Bank4"));
        AddOffset("name", o.Name, AppLocale.Get("MemEdit.Off.Name"));
        AddOffset("statLife", o.StatLife, AppLocale.Get("MemEdit.Off.StatLife"));
        AddOffset("statMana", o.StatMana, AppLocale.Get("MemEdit.Off.StatMana"));
        AddOffset("difficulty", o.Difficulty, AppLocale.Get("MemEdit.Off.Difficulty"));
        AddOffset("itemType", o.ItemType, AppLocale.Get("MemEdit.Off.ItemType"));
        AddOffset("itemStack", o.ItemStack, AppLocale.Get("MemEdit.Off.ItemStack"));
        AddOffset("itemPrefix", o.ItemPrefix, AppLocale.Get("MemEdit.Off.ItemPrefix"));
        AddOffset("itemFavorited", o.ItemFavorited, AppLocale.Get("MemEdit.Off.ItemFavorited"));
        AddOffset("chestItemArray", o.ChestItemArray, AppLocale.Get("MemEdit.Off.ChestItemArray"));
        offsetGroup.Controls.Add(offsetLayout);
        layout.Controls.Add(offsetGroup, 1, 2);

        // === Buttons ===
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var btnOk = new Button { Text = AppLocale.Get("UI.Ok"), Width = 90, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = AppLocale.Get("UI.Cancel"), Width = 90, DialogResult = DialogResult.Cancel };
        var btnReset = new Button { Text = AppLocale.Get("MemEdit.ResetDefaults"), Width = 120 };
        buttons.Controls.AddRange([btnOk, btnCancel, btnReset]);
        Controls.Add(buttons);
        Controls.Add(layout);

        btnReset.Click += (_, _) =>
        {
            MemorySettings.ChainStackSubtract = 0x3D8;
            MemorySettings.ChainOffsets = new List<uint> { 0x32C, 0x4, 0x550, 0x0, 0x0, 0xD8 };
            MemorySettings.ChainFinalDeref = false;
            _txtStackSubtract.Text = "3D8";
            _txtChain.Text = MemorySettings.ChainToString(MemorySettings.ChainOffsets);
            _chkFinalDeref.Checked = false;
            var def = new PlayerMemoryOffsets();
            foreach (var kv in _offsetBoxes)
            {
                uint v = kv.Key switch
                {
                    "inventory" => def.Inventory,
                    "armor" => def.Armor,
                    "dye" => def.Dye,
                    "miscEquips" => def.MiscEquips,
                    "miscDyes" => def.MiscDyes,
                    "bank" => def.Bank,
                    "bank2" => def.Bank2,
                    "bank3" => def.Bank3,
                    "bank4" => def.Bank4,
                    "name" => def.Name,
                    "statLife" => def.StatLife,
                    "statMana" => def.StatMana,
                    "difficulty" => def.Difficulty,
                    "itemType" => def.ItemType,
                    "itemStack" => def.ItemStack,
                    "itemPrefix" => def.ItemPrefix,
                    "itemFavorited" => def.ItemFavorited,
                    "chestItemArray" => def.ChestItemArray,
                    _ => 0
                };
                kv.Value.Text = v.ToString("X");
            }
        };

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    /// <summary>Show the dialog and apply the settings if confirmed. Returns true when saved.</summary>
    public static bool ShowAndApply(IWin32Window owner, bool connected)
    {
        using var dlg = new MemorySettingsDialog(connected);
        if (dlg.ShowDialog(owner) != DialogResult.OK)
            return false;

        try
        {
            MemorySettings.ChainStackSubtract = Convert.ToUInt32(dlg._txtStackSubtract.Text.Trim(), 16);
        }
        catch
        {
            MessageBox.Show(owner, AppLocale.Get("MemEdit.InvalidHex"), Application.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (!MemorySettings.TryParseChain(dlg._txtChain.Text, out var chain))
        {
            MessageBox.Show(owner, AppLocale.Get("MemEdit.InvalidChain"), Application.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        MemorySettings.ChainOffsets = chain;
        MemorySettings.ChainFinalDeref = dlg._chkFinalDeref.Checked;

        var o = MemorySettings.Offsets;
        bool ok = true;
        foreach (var kv in dlg._offsetBoxes)
        {
            if (!TryHex(kv.Value.Text, out uint v)) { ok = false; break; }
            switch (kv.Key)
            {
                case "inventory": o.Inventory = v; break;
                case "armor": o.Armor = v; break;
                case "dye": o.Dye = v; break;
                case "miscEquips": o.MiscEquips = v; break;
                case "miscDyes": o.MiscDyes = v; break;
                case "bank": o.Bank = v; break;
                case "bank2": o.Bank2 = v; break;
                case "bank3": o.Bank3 = v; break;
                case "bank4": o.Bank4 = v; break;
                case "name": o.Name = v; break;
                case "statLife": o.StatLife = v; break;
                case "statMana": o.StatMana = v; break;
                case "difficulty": o.Difficulty = v; break;
                case "itemType": o.ItemType = v; break;
                case "itemStack": o.ItemStack = v; break;
                case "itemPrefix": o.ItemPrefix = v; break;
                case "itemFavorited": o.ItemFavorited = v; break;
                case "chestItemArray": o.ChestItemArray = v; break;
            }
        }
        if (!ok)
        {
            MessageBox.Show(owner, AppLocale.Get("MemEdit.InvalidHex"), Application.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        MemorySettings.Save();
        return true;
    }

    private static bool TryHex(string text, out uint value)
    {
        try
        {
            value = Convert.ToUInt32(text.Trim(), 16);
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = ThemeManager.TextSecondary,
        Tag = "secondary",
        Margin = new Padding(0, 6, 6, 0)
    };

    private static TextBox MakeHexBox(uint value) => new()
    {
        Text = value.ToString("X"),
        Width = 90,
        Margin = new Padding(0, 2, 0, 2)
    };

    private static TextBox MakeText(string text) => new()
    {
        Text = text,
        Width = 220,
        Margin = new Padding(0, 2, 0, 2)
    };
}

/// <summary>Minimal flat group box for the settings dialog.</summary>
internal sealed class FlatGroupBox2 : Control
{
    public FlatGroupBox2()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        AutoSize = true;
        Height = 60;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        using var border = new Pen(ThemeManager.IsDarkMode ? Color.FromArgb(70, 70, 80) : Color.FromArgb(210, 210, 215));
        var rect = new Rectangle(0, 8, Width - 1, Height - 9);
        g.DrawRectangle(border, rect);
        using var back = new SolidBrush(ThemeManager.SurfaceBackground);
        var titleRect = new Rectangle(10, 0, TextRenderer.MeasureText(Text, Font).Width + 10, 16);
        g.FillRectangle(back, titleRect);
        TextRenderer.DrawText(g, Text, Font, new Point(15, 1), ThemeManager.TextPrimary);
    }
}
