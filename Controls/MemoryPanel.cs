using System.Diagnostics;
using Terraria_Players_Editor.Controls;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;
using Terraria_Players_Editor.Services.Memory;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// The "内存编辑" (memory editing) tab: finds the running game process, resolves
/// the live Player object through the pointer chain, and lets the user edit the
/// in-memory inventory visually — reusing the same SlotGrid / ItemModifier /
/// ItemBrowser controls as the file editor. Writes go straight into the game's
/// memory (type/stack/prefix/favorited). The SetDefaults refresh mechanism is a
/// later phase; see MemorySettings for the configurable pointer chain.
/// </summary>
public sealed class MemoryPanel : UserControl
{
    // === Top toolbar ===
    private Label _lblProcess = null!;
    private ComboBox _cmbProcess = null!;
    private Button _btnRefreshProcesses = null!;
    private Button _btnConnect = null!;
    private Button _btnDisconnect = null!;
    private Button _btnSettings = null!;
    private CheckBox _chkAutoRefresh = null!;
    private Label _lblStatus = null!;

    // === Left: item sections ===
    private Panel _scrollSections = null!;
    private SlotGrid _gridInv = null!;      // inventory[0..49]
    private SlotGrid _gridCoins = null!;    // inventory[50..53]
    private SlotGrid _gridAmmo = null!;     // inventory[54..57]
    private SlotGrid _gridTrash = null!;    // inventory[58]
    private SlotGrid _gridArmor = null!;    // armor[0..2]
    private SlotGrid _gridDye = null!;      // dye[0..9]
    private SlotGrid _gridMisc = null!;     // miscEquips[0..4]
    private SlotGrid _gridMiscDyes = null!; // miscDyes[0..4]
    private SlotGrid[] _bankGrids = null!;  // bank..bank4, 10x4 each

    // === Right: modifier + browser ===
    private ItemModifier _modifier = null!;
    private ItemBrowser _browser = null!;

    private PlayerMemorySession? _session;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly List<SlotGrid> _allGrids = new();
    private SlotGrid? _activeGrid;

    public MemoryPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();

        BuildUi();
        WireStaticEvents();
        RefreshProcessList();

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _refreshTimer.Tick += (_, _) => AutoRefreshTick();
    }

    #region UI construction

    private void BuildUi()
    {
        // === Top toolbar ===
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(6, 5, 6, 0),
            WrapContents = false,
            AutoScroll = false
        };

        _lblProcess = new Label
        {
            Text = AppLocale.Get("MemEdit.Process"),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "secondary",
            Margin = new Padding(0, 6, 4, 0)
        };
        _cmbProcess = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 260,
            Margin = new Padding(0, 2, 6, 0)
        };
        _btnRefreshProcesses = new Button { Text = AppLocale.Get("MemEdit.RefreshList"), AutoSize = true, Margin = new Padding(0, 1, 6, 0) };
        _btnConnect = new Button { Text = AppLocale.Get("MemEdit.Connect"), AutoSize = true, Margin = new Padding(0, 1, 6, 0), Enabled = false };
        _btnDisconnect = new Button { Text = AppLocale.Get("MemEdit.Disconnect"), AutoSize = true, Margin = new Padding(0, 1, 6, 0), Enabled = false };
        _btnSettings = new Button { Text = AppLocale.Get("MemEdit.Settings"), AutoSize = true, Margin = new Padding(0, 1, 6, 0) };
        _chkAutoRefresh = new CheckBox
        {
            Text = AppLocale.Get("MemEdit.AutoRefresh"),
            AutoSize = true,
            Checked = MemorySettings.AutoRefresh,
            Margin = new Padding(0, 6, 6, 0)
        };
        topBar.Controls.AddRange([_lblProcess, _cmbProcess, _btnRefreshProcesses, _btnConnect, _btnDisconnect, _btnSettings, _chkAutoRefresh]);

        _lblStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = AppLocale.Get("MemEdit.Status.Disconnected"),
            ForeColor = ThemeManager.TextSecondary,
            Padding = new Padding(8, 5, 0, 0),
            Tag = "secondary"
        };

        // === Main split ===
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2
        };

        // --- Left: scrollable sections ---
        _scrollSections = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        split.Panel1.Controls.Add(_scrollSections);
        RebuildSections();

        // --- Right: modifier (top) + item browser (fill) ---
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(4) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _modifier = new ItemModifier { Dock = DockStyle.Top, ShowStack = true, ShowPrefix = true, ShowFavorite = true };
        _browser = new ItemBrowser { Dock = DockStyle.Fill };
        right.Controls.Add(_modifier, 0, 0);
        right.Controls.Add(_browser, 0, 1);
        split.Panel2.Controls.Add(right);

        Controls.Add(split);
        Controls.Add(_lblStatus);
        Controls.Add(topBar);

        _modifier.PopulateItems();
        _modifier.PopulatePrefixes();

        // Load the browser data only once the control is laid out: calling
        // LoadItems before the panel has a size throws in ItemBrowser's
        // ApplyFilter (DataGridView has no display rows yet).
        Load += (_, _) =>
        {
            _browser.LoadItems();
            _browser.SetView(SettingsManager.BrowserViewMode, SettingsManager.BrowserIconSize);
        };

        Load += (_, _) =>
        {
            if (split.Width > 500)
                split.SplitterDistance = Math.Max(200, split.Width - 360);
        };
    }

    /// <summary>Rebuild the left section panels (used at startup and on language change).</summary>
    private void RebuildSections()
    {
        _scrollSections.Controls.Clear();
        _allGrids.Clear();
        _activeGrid = null;

        var sectionsLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6)
        };
        sectionsLayout.Controls.Add(BuildInventorySection());
        sectionsLayout.Controls.Add(BuildEquipSection());
        sectionsLayout.Controls.Add(BuildBankSection());

        _scrollSections.SizeChanged += (s, e) =>
        {
            int w = _scrollSections.ClientSize.Width - 28;
            if (w > 250)
            {
                foreach (Control c in sectionsLayout.Controls)
                    if (c is FlatGroupBox grp) grp.Width = w;
            }
        };
        _scrollSections.Controls.Add(sectionsLayout);
    }

    private void RegisterGrid(SlotGrid grid)
    {
        _allGrids.Add(grid);
        grid.SlotSelected += (_, idx) => OnSlotSelected(grid, idx);
    }

    private FlatGroupBox BuildInventorySection()
    {
        var grp = new FlatGroupBox
        {
            Text = AppLocale.Get("Tab.Inventory"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10)
        };

        _gridInv = new SlotGrid(10, 5, enableHotbarColor: true, gridTitle: AppLocale.Get("Grid.MainInventory")) { Tag = "MemInv" };
        _gridCoins = new SlotGrid(1, 4, gridTitle: AppLocale.Get("Grid.Coins")) { Tag = "MemCoins" };
        _gridAmmo = new SlotGrid(1, 4, gridTitle: AppLocale.Get("Grid.Ammo")) { Tag = "MemAmmo" };
        _gridTrash = new SlotGrid(1, 1, gridTitle: AppLocale.Get("MemEdit.Trash")) { Tag = "MemTrash" };

        // Same layout as the file editor: 10x5 main grid, coins + ammo columns, trash at the end.
        var invGrid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 14,
            RowCount = 6,
            Margin = new Padding(5, 5, 10, 10)
        };
        for (int c = 0; c < 10; c++)
            invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); // gap
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // coins
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // ammo
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // trash
        invGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        for (int r = 0; r < 5; r++)
            invGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        _gridInv.Margin = new Padding(0);
        _gridCoins.Margin = new Padding(0);
        _gridAmmo.Margin = new Padding(0);
        _gridTrash.Margin = new Padding(0);

        invGrid.Controls.Add(_gridInv, 0, 0);
        invGrid.SetColumnSpan(_gridInv, 10);
        invGrid.SetRowSpan(_gridInv, 6);
        invGrid.Controls.Add(_gridCoins, 11, 0);
        invGrid.SetRowSpan(_gridCoins, 5);
        invGrid.Controls.Add(_gridAmmo, 12, 0);
        invGrid.SetRowSpan(_gridAmmo, 5);
        invGrid.Controls.Add(_gridTrash, 13, 0);
        invGrid.SetRowSpan(_gridTrash, 5);

        var inner = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };
        inner.Controls.Add(invGrid);
        grp.Controls.Add(inner);

        RegisterGrid(_gridInv);
        RegisterGrid(_gridCoins);
        RegisterGrid(_gridAmmo);
        RegisterGrid(_gridTrash);
        return grp;
    }

    private FlatGroupBox BuildEquipSection()
    {
        var grp = new FlatGroupBox
        {
            Text = AppLocale.Get("Tab.Equipment"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10)
        };
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        _gridArmor = new SlotGrid(1, 3, gridTitle: AppLocale.Get("Equip.Armor")) { Tag = "MemArmor" };
        _gridDye = new SlotGrid(10, 1, gridTitle: AppLocale.Get("MemEdit.Dyes")) { Tag = "MemDye" };
        _gridMisc = new SlotGrid(1, 5, gridTitle: AppLocale.Get("Equip.Misc")) { Tag = "MemMisc" };
        _gridMiscDyes = new SlotGrid(1, 5, gridTitle: AppLocale.Get("MemEdit.MiscDyes")) { Tag = "MemMiscDyes" };

        _gridMisc.SetCellLabels([
            AppLocale.Get("Misc.Pet"), AppLocale.Get("Misc.LightPet"), AppLocale.Get("Misc.Minecart"),
            AppLocale.Get("Misc.Mount"), AppLocale.Get("Misc.Hook")
        ]);

        layout.Controls.Add(_gridArmor);
        layout.Controls.Add(_gridDye);
        layout.Controls.Add(_gridMisc);
        layout.Controls.Add(_gridMiscDyes);
        grp.Controls.Add(layout);

        RegisterGrid(_gridArmor);
        RegisterGrid(_gridDye);
        RegisterGrid(_gridMisc);
        RegisterGrid(_gridMiscDyes);
        return grp;
    }

    private FlatGroupBox BuildBankSection()
    {
        var grp = new FlatGroupBox
        {
            Text = AppLocale.Get("Tab.Storage"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10)
        };
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        string[] names =
        [
            AppLocale.Get("Storage.PiggyBank"),
            AppLocale.Get("Storage.Safe"),
            AppLocale.Get("Storage.DefenderForge"),
            AppLocale.Get("Storage.VoidVault")
        ];
        _bankGrids = new SlotGrid[4];
        for (int i = 0; i < 4; i++)
        {
            _bankGrids[i] = new SlotGrid(10, 4, gridTitle: names[i]) { Tag = "MemBank" + i };
            layout.Controls.Add(_bankGrids[i]);
            RegisterGrid(_bankGrids[i]);
        }
        grp.Controls.Add(layout);
        return grp;
    }

    /// <summary>One-time event wiring (toolbar, modifier, browser, global grid deselection).</summary>
    private void WireStaticEvents()
    {
        _btnRefreshProcesses.Click += (_, _) => RefreshProcessList();
        _btnConnect.Click += (_, _) => ConnectSelected();
        _btnDisconnect.Click += (_, _) => Disconnect();
        _btnSettings.Click += (_, _) => ShowSettingsDialog();
        _chkAutoRefresh.CheckedChanged += (_, _) =>
        {
            MemorySettings.AutoRefresh = _chkAutoRefresh.Checked;
            MemorySettings.Save();
            _refreshTimer.Enabled = _chkAutoRefresh.Checked && _session != null;
        };

        // Global deselection: when ANY memory grid selects a slot, clear all other memory grids.
        SlotGrid.BeforeAnySlotSelected += selected =>
        {
            if (!_allGrids.Contains(selected)) return;
            foreach (var g in _allGrids)
                if (g != selected)
                    g.ClearSelection();
        };

        _modifier.SetClicked += OnModifierSet;
        _modifier.ClearClicked += OnModifierClear;
        _browser.ItemSelected += (_, itemId) => OnBrowserSelect(itemId);

        _cmbProcess.SelectedIndexChanged += (_, _) => _btnConnect.Enabled = _cmbProcess.SelectedItem != null;
    }

    #endregion

    #region Process / connection

    /// <summary>List candidate game processes (Terraria / tModLoader).</summary>
    public static List<Process> FindGameProcesses()
    {
        var result = new List<Process>();
        foreach (var name in new[] { "Terraria", "tModLoader", "TerrariaServer" })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                    result.Add(p);
            }
            catch
            {
                // process list race — ignore
            }
        }
        return result.OrderBy(p => p.ProcessName).ThenBy(p => p.Id).ToList();
    }

    private void RefreshProcessList()
    {
        _cmbProcess.Items.Clear();
        foreach (var p in FindGameProcesses())
        {
            _cmbProcess.Items.Add($"{p.ProcessName} (PID {p.Id})");
        }
        _cmbProcess.SelectedIndex = _cmbProcess.Items.Count > 0 ? 0 : -1;
        if (_cmbProcess.Items.Count == 0)
            SetStatus(AppLocale.Get("MemEdit.Status.NoProcess"), false);
    }

    private async void ConnectSelected()
    {
        if (_cmbProcess.SelectedIndex < 0) return;
        var processes = FindGameProcesses();
        if (_cmbProcess.SelectedIndex >= processes.Count) { RefreshProcessList(); return; }
        await ConnectToAsync(processes[_cmbProcess.SelectedIndex]);
    }

    /// <summary>Connect to a game process and resolve the Player base. Returns false on failure.</summary>
    public async Task<bool> ConnectToAsync(Process process)
    {
        Disconnect();

        var mp = await Task.Run(() => MemoryProcess.TryOpen(process, out _));
        if (mp == null)
        {
            SetStatus(string.Format(AppLocale.Get("MemEdit.Status.OpenFailed"), process.Id), true);
            return false;
        }

        var session = new PlayerMemorySession(mp);
        bool resolved = session.ResolvePlayerBase() && session.PlayerBase != 0;
        string method = "chain";
        if (!resolved && MemorySettings.AutoScanFallback)
        {
            SetStatus(AppLocale.Get("MemEdit.Status.Scanning"), false);
            _btnConnect.Enabled = false;
            resolved = await Task.Run(() => session.FindPlayerByScan());
            method = "scan";
        }
        if (!resolved || session.PlayerBase == 0)
        {
            SetStatus(AppLocale.Get("MemEdit.Status.ResolveFailed"), true);
            session.Dispose();
            _btnConnect.Enabled = _cmbProcess.SelectedItem != null;
            return false;
        }

        var info = session.ReadPlayerInfo();
        if (info == null)
        {
            SetStatus(AppLocale.Get("MemEdit.Status.VerifyFailed"), true);
            session.Dispose();
            _btnConnect.Enabled = _cmbProcess.SelectedItem != null;
            return false;
        }

        _session = session;
        _btnDisconnect.Enabled = true;
        _btnConnect.Enabled = false;
        _btnSettings.Enabled = true;

        SetStatus(string.Format(AppLocale.Get("MemEdit.Status.Connected"),
            process.ProcessName, process.Id, session.PlayerBase.ToString("X8"), info.Name)
            + (method == "scan" ? AppLocale.Get("MemEdit.Status.Scanned") : ""), false);

        RefreshAllSections();
        _refreshTimer.Enabled = _chkAutoRefresh.Checked;
        return true;
    }

    public void Disconnect()
    {
        _refreshTimer.Enabled = false;
        _session?.Dispose();
        _session = null;
        _btnDisconnect.Enabled = false;
        _btnConnect.Enabled = _cmbProcess.SelectedItem != null;
        foreach (var g in _allGrids)
            g.ClearAll();
        _modifier.LoadFromSlot(-1, new ItemData());
        SetStatus(AppLocale.Get("MemEdit.Status.Disconnected"), false);
    }

    /// <summary>Whether a session is currently connected.</summary>
    public bool IsConnected => _session != null;

    /// <summary>Open the advanced settings dialog; re-resolves the base when connected.</summary>
    public void ShowSettingsDialog()
    {
        if (Forms.MemorySettingsDialog.ShowAndApply(this, _session != null))
        {
            if (_session != null && !_session.ResolvePlayerBase())
            {
                SetStatus(AppLocale.Get("MemEdit.Status.ResolveFailed"), true);
                Disconnect();
            }
            else if (_session != null)
            {
                RefreshAllSections();
            }
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool AutoRefreshEnabled
    {
        get => _chkAutoRefresh.Checked;
        set => _chkAutoRefresh.Checked = value;
    }

    private void SetStatus(string text, bool error)
    {
        _lblStatus.Text = text;
        _lblStatus.ForeColor = error ? Color.FromArgb(220, 90, 90) : ThemeManager.TextSecondary;
    }

    #endregion

    #region Reading / writing

    private (MemoryItemSection section, int index) SectionOf(SlotGrid grid, int slotIdx)
    {
        if (grid == _gridInv) return (MemoryItemSection.Inventory, slotIdx);
        if (grid == _gridCoins) return (MemoryItemSection.Inventory, 50 + slotIdx);
        if (grid == _gridAmmo) return (MemoryItemSection.Inventory, 54 + slotIdx);
        if (grid == _gridTrash) return (MemoryItemSection.Inventory, 58);
        if (grid == _gridArmor) return (MemoryItemSection.Armor, slotIdx);
        if (grid == _gridDye) return (MemoryItemSection.Dye, slotIdx);
        if (grid == _gridMisc) return (MemoryItemSection.MiscEquips, slotIdx);
        if (grid == _gridMiscDyes) return (MemoryItemSection.MiscDyes, slotIdx);
        for (int i = 0; i < _bankGrids.Length; i++)
            if (grid == _bankGrids[i])
                return ((MemoryItemSection)((int)MemoryItemSection.Bank + i), slotIdx);
        return (MemoryItemSection.Inventory, 0);
    }

    private void OnSlotSelected(SlotGrid grid, int slotIdx)
    {
        if (_session == null) return;
        _activeGrid = grid;

        // Adjust modifier fields like the file editor does per context.
        bool isEquip = grid == _gridArmor || grid == _gridDye || grid == _gridMisc || grid == _gridMiscDyes;
        _modifier.ShowStack = !isEquip;
        _modifier.ShowPrefix = true;
        _modifier.ShowFavorite = !isEquip && grid != _gridTrash;

        var item = grid.GetItem(slotIdx) ?? new ItemData();
        _modifier.LoadFromSlot(slotIdx, item);
    }

    private void OnModifierSet(object? sender, int slotIdx)
    {
        if (_session == null || _activeGrid == null) return;
        var item = _modifier.BuildItemData();
        var (section, index) = SectionOf(_activeGrid, slotIdx);
        if (_session.WriteItem(section, index, item))
        {
            _activeGrid.SetSlot(slotIdx, item);
            _modifier.LoadFromSlot(slotIdx, item);
        }
        else
        {
            SetStatus(AppLocale.Get("MemEdit.Status.WriteFailed"), true);
        }
    }

    private void OnModifierClear(object? sender, int slotIdx)
    {
        if (_session == null || _activeGrid == null) return;
        var (section, index) = SectionOf(_activeGrid, slotIdx);
        if (_session.WriteItem(section, index, new ItemData()))
        {
            _activeGrid.SetSlot(slotIdx, new ItemData());
            _modifier.LoadFromSlot(slotIdx, new ItemData());
        }
        else
        {
            SetStatus(AppLocale.Get("MemEdit.Status.WriteFailed"), true);
        }
    }

    private void OnBrowserSelect(int itemId)
    {
        if (_session == null || _activeGrid == null || _activeGrid.SelectedIndex < 0) return;
        var idx = _activeGrid.SelectedIndex;
        var item = new ItemData { ItemId = itemId, StackSize = 1 };
        var (section, index) = SectionOf(_activeGrid, idx);
        if (_session.WriteItem(section, index, item))
        {
            _activeGrid.SetSlot(idx, item);
            _modifier.LoadFromSlot(idx, item);
        }
        else
        {
            SetStatus(AppLocale.Get("MemEdit.Status.WriteFailed"), true);
        }
    }

    private void AutoRefreshTick()
    {
        if (_session == null) return;
        try
        {
            if (_session.Process.Process.HasExited)
            {
                SetStatus(AppLocale.Get("MemEdit.Status.ProcessExited"), true);
                Disconnect();
                return;
            }
        }
        catch
        {
            Disconnect();
            return;
        }
        RefreshAllSections();
    }

    private void RefreshAllSections()
    {
        if (_session == null) return;

        // Health check: if the player info can no longer be read, the Player
        // object was recreated (left the world / respawned) or the process is
        // in a weird state — ask the user to reconnect instead of showing an
        // empty inventory.
        var info = _session.ReadPlayerInfo();
        if (info == null)
        {
            SetStatus(AppLocale.Get("MemEdit.Status.ReadFailed"), true);
            _refreshTimer.Enabled = false;
            return;
        }

        _gridInv.SetItems(_session.ReadItemSection(MemoryItemSection.Inventory, 50));
        _gridCoins.SetItems(_session.ReadItemSection(MemoryItemSection.Inventory, 4, 50));
        _gridAmmo.SetItems(_session.ReadItemSection(MemoryItemSection.Inventory, 4, 54));
        var trash = _session.ReadItem(MemoryItemSection.Inventory, 58) ?? new ItemData();
        _gridTrash.SetItems([trash]);

        _gridArmor.SetItems(_session.ReadItemSection(MemoryItemSection.Armor, 3));
        _gridDye.SetItems(_session.ReadItemSection(MemoryItemSection.Dye, 10));
        _gridMisc.SetItems(_session.ReadItemSection(MemoryItemSection.MiscEquips, 5));
        _gridMiscDyes.SetItems(_session.ReadItemSection(MemoryItemSection.MiscDyes, 5));

        for (int i = 0; i < 4; i++)
            _bankGrids[i].SetItems(_session.ReadItemSection((MemoryItemSection)((int)MemoryItemSection.Bank + i), 40));

        SetStatus(string.Format(AppLocale.Get("MemEdit.Status.Connected"),
            _session.Process.Process.ProcessName, _session.Process.ProcessId,
            _session.PlayerBase.ToString("X8"), info.Name), false);
    }

    #endregion

    /// <summary>Refresh localized texts (called on language change).</summary>
    public void RefreshLocale()
    {
        _lblProcess.Text = AppLocale.Get("MemEdit.Process");
        _btnRefreshProcesses.Text = AppLocale.Get("MemEdit.RefreshList");
        _btnConnect.Text = AppLocale.Get("MemEdit.Connect");
        _btnDisconnect.Text = AppLocale.Get("MemEdit.Disconnect");
        _btnSettings.Text = AppLocale.Get("MemEdit.Settings");
        _chkAutoRefresh.Text = AppLocale.Get("MemEdit.AutoRefresh");
        _modifier.RefreshLocale();
        _browser.RefreshDisplayText();

        RebuildSections();
        if (_session != null)
            RefreshAllSections();
        else
            SetStatus(AppLocale.Get("MemEdit.Status.Disconnected"), false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Dispose();
            _session?.Dispose();
            _session = null;
        }
        base.Dispose(disposing);
    }
}
