using System.Diagnostics;
using Terraria_Players_Editor.Controls;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Forms;

/// <summary>
/// Lists running Terraria / tModLoader processes; double-click or press
/// "连接" to pick one. The chosen process is exposed via <see cref="SelectedProcess"/>.
/// </summary>
public sealed class ProcessSelectDialog : Form
{
    private readonly ListView _list;
    private readonly Button _btnRefresh;
    private readonly Button _btnConnect;
    private readonly Button _btnCancel;

    public Process? SelectedProcess { get; private set; }

    public ProcessSelectDialog()
    {
        Text = AppLocale.Get("MemEdit.SelectProcess");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 320);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = ThemeManager.Typography.Body;
        BackColor = ThemeManager.SurfaceBackground;

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        _list.Columns.Add(AppLocale.Get("MemEdit.Process"), 210);
        _list.Columns.Add("PID", 70);
        _list.Columns.Add(AppLocale.Get("MemEdit.WindowTitle"), 180);
        _list.Columns.Add(AppLocale.Get("MemEdit.Bits"), 60);
        _list.DoubleClick += (_, _) => Confirm();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        _btnConnect = new Button { Text = AppLocale.Get("MemEdit.Connect"), Width = 90, Enabled = false };
        _btnCancel = new Button { Text = AppLocale.Get("UI.Cancel"), Width = 90 };
        _btnRefresh = new Button { Text = AppLocale.Get("MemEdit.RefreshList"), Width = 110 };
        buttons.Controls.AddRange([_btnConnect, _btnCancel, _btnRefresh]);

        Controls.Add(_list);
        Controls.Add(buttons);

        _btnRefresh.Click += (_, _) => RefreshList();
        _btnConnect.Click += (_, _) => Confirm();
        _btnCancel.Click += (_, _) => Close();
        _list.SelectedIndexChanged += (_, _) => _btnConnect.Enabled = _list.SelectedItems.Count > 0;

        RefreshList();
    }

    private void RefreshList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var p in MemoryPanel.FindGameProcesses())
        {
            string title = "";
            try { title = p.MainWindowTitle; } catch { }
            string bits = "";
            try
            {
                var h = Terraria_Players_Editor.Services.Memory.Win32Api.OpenProcess(
                    Terraria_Players_Editor.Services.Memory.Win32Api.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)p.Id);
                if (h != IntPtr.Zero)
                {
                    bits = Terraria_Players_Editor.Services.Memory.Win32Api.IsWow64Process(h, out bool wow) && wow ? "32" : "64";
                    Terraria_Players_Editor.Services.Memory.Win32Api.CloseHandle(h);
                }
            }
            catch { bits = "?"; }

            var item = new ListViewItem(p.ProcessName);
            item.SubItems.Add(p.Id.ToString());
            item.SubItems.Add(title);
            item.SubItems.Add(bits);
            item.Tag = p;
            _list.Items.Add(item);
        }
        _list.EndUpdate();
    }

    private void Confirm()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is Process p)
        {
            SelectedProcess = p;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
