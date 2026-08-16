using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Forms;

/// <summary>
/// Lets the user pick which of several in-memory Player objects is the active
/// character (save caches can produce extra candidates).
/// </summary>
public sealed class PlayerSelectDialog : Form
{
    private readonly ListView _list;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public uint SelectedBase { get; private set; }

    public PlayerSelectDialog(IEnumerable<(uint Base, string Name)> candidates)
    {
        Text = AppLocale.Get("MemEdit.SelectPlayer");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 280);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = ThemeManager.Typography.Body;
        BackColor = ThemeManager.SurfaceBackground;

        var hint = new Label
        {
            Text = AppLocale.Get("MemEdit.SelectPlayerHint"),
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = ThemeManager.TextSecondary,
            Tag = "secondary",
            Padding = new Padding(8, 6, 8, 0)
        };

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        _list.Columns.Add(AppLocale.Get("MemEdit.Player"), 160);
        _list.Columns.Add("Base", 130);
        foreach (var (baseAddr, name) in candidates)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(baseAddr.ToString("X8"));
            item.Tag = baseAddr;
            _list.Items.Add(item);
        }
        if (_list.Items.Count > 0) _list.Items[0].Selected = true;
        _list.DoubleClick += (_, _) => Confirm();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        _btnOk = new Button { Text = AppLocale.Get("UI.Ok"), Width = 90, DialogResult = DialogResult.OK };
        _btnCancel = new Button { Text = AppLocale.Get("UI.Cancel"), Width = 90, DialogResult = DialogResult.Cancel };
        buttons.Controls.AddRange([_btnOk, _btnCancel]);

        Controls.Add(_list);
        Controls.Add(buttons);
        Controls.Add(hint);

        _btnOk.Click += (_, _) => Confirm();
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void Confirm()
    {
        if (_list.SelectedItems.Count > 0 && _list.SelectedItems[0].Tag is uint baseAddr)
        {
            SelectedBase = baseAddr;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
