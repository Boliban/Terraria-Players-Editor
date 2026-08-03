using System.IO;
using Terraria_Players_Editor.Controls;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor;

public partial class MainForm : Form
{
    private PlayerData? _player;
    private string? _filePath;

    private ToolStripMenuItem? _langEnItem, _langZhItem, _animIconItem, _verticalLabelsItem, _refreshMenuItem, _contextRefreshItem;
    private ToolStripMenuItem? _browserViewDetailsItem, _browserViewLargeItem, _iconSize32Item, _iconSize48Item, _iconSize64Item;

    // Right-click context menu
    private ContextMenuStrip? _contextMenu;

    // Localized label arrays — used during tab construction
    private static string[] DifficultyNames() => [AppLocale.Get("Diff.Softcore"), AppLocale.Get("Diff.Mediumcore"), AppLocale.Get("Diff.Hardcore"), AppLocale.Get("Diff.Journey")];
    private static string[] LoadoutNames() => [AppLocale.Get("Loadout.Select1"), AppLocale.Get("Loadout.Select2"), AppLocale.Get("Loadout.Select3")];
    private static string[] GenderNames() => [AppLocale.Get("Gender.Female"), AppLocale.Get("Gender.Male")];
    private static string[] HideVisualNames() => [AppLocale.Get("Appearance.Head"), AppLocale.Get("Appearance.Body"), AppLocale.Get("Appearance.Legs"), AppLocale.Get("Appearance.VanityHead"), AppLocale.Get("Appearance.VanityBody"), AppLocale.Get("Appearance.VanityLegs"), AppLocale.Get("Appearance.Acc1"), AppLocale.Get("Appearance.Acc2"), AppLocale.Get("Appearance.Acc3"), AppLocale.Get("Appearance.Acc4")];
    private static string[] HideMiscNames() => [AppLocale.Get("Appearance.Pet"), AppLocale.Get("Appearance.LightPet"), AppLocale.Get("Appearance.Minecart"), AppLocale.Get("Appearance.Mount"), AppLocale.Get("Appearance.Hook")];
    private static string[] HideInfoNames() => [AppLocale.Get("Info.Watch"), AppLocale.Get("Info.Weather"), AppLocale.Get("Info.Depth"), AppLocale.Get("Info.Compass"), AppLocale.Get("Info.Sextant"), AppLocale.Get("Info.Tally"), AppLocale.Get("Info.Stopwatch"), AppLocale.Get("Info.MetalDetector"), AppLocale.Get("Info.DPS"), AppLocale.Get("Info.RareCreature"), AppLocale.Get("Info.FishingPower"), AppLocale.Get("Info.MoonPhase"), AppLocale.Get("Info.Speed")];
    private static string[] ColorNames() => [AppLocale.Get("Color.Hair"), AppLocale.Get("Color.Skin"), AppLocale.Get("Color.Eyes"), AppLocale.Get("Color.Shirt"), AppLocale.Get("Color.UnderShirt"), AppLocale.Get("Color.Pants"), AppLocale.Get("Color.Shoes")];


    // Temporary color storage during editing
    private byte[][] _tempColors = Array.Empty<byte[]>();

    public MainForm()
    {
        InitializeComponent();
        BuildForm();
        AppLocale.LanguageChanged += RefreshAllUI;
        RefreshAllUI(); // Apply current language to all UI elements on startup

        // TEMP debug: autoload via env var for display verification — REMOVE
        var autoLoad = Environment.GetEnvironmentVariable("TPE_AUTOLOAD");
        if (!string.IsNullOrEmpty(autoLoad) && File.Exists(autoLoad))
        {
            Shown += async (s, e) =>
            {
                try
                {
                    SetLoading(true);
                    var bytes = await Task.Run(() => File.ReadAllBytes(autoLoad));
                    var player = await Task.Run(() => PlrFileReader.Read(PlrCrypto.Decrypt(bytes)));
                    _player = player;
                    _filePath = autoLoad;
                    PopulateAllTabs();
                    SetLoading(false);
                    TraceLog($"[AUTOLOAD] loaded {autoLoad}");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
        }
    }

    #region Form Construction

    private void BuildForm()
    {
        Text = "Terraria Players Editor";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = ThemeManager.Typography.Body;
        MinimumSize = new Size(800, 500);

        // Enable double buffering to reduce flicker during resize and child repaints
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();

        BuildMenu();
        BuildStatusBar();
        BuildTabControl();

        // Theme change handler
        ThemeManager.ThemeChanged += OnThemeChanged;

        // Apply initial theme
        RefreshThemeColors(this);

        // Set SplitterDistance after form is shown (controls have proper sizes)
        Shown += (s, e) =>
        {
            _splitItems.SplitterDistance = 300;
            _splitBuffs.SplitterDistance = 280;
        };
    }

    /// <summary>Recursively set Label backgrounds to transparent only.</summary>
    private static void RefreshThemeColors(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is Label lbl)
                c.BackColor = Color.Transparent;

            if (ThemeManager.IsDarkMode)
            {
                if (c is TextBox tb)
                {
                    tb.BackColor = ThemeManager.ControlInputBg;
                    tb.ForeColor = ThemeManager.TextPrimary;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                if (c is ComboBox cmb)
                {
                    // Flat style makes the closed box AND dropdown popup honor
                    // BackColor/ForeColor (the native themed combo ignores them).
                    cmb.FlatStyle = FlatStyle.Flat;
                    cmb.BackColor = ThemeManager.ControlInputBg;
                    cmb.ForeColor = ThemeManager.TextPrimary;
                }
                if (c is NumericUpDown nud)
                {
                    nud.BackColor = ThemeManager.ControlInputBg;
                    nud.ForeColor = ThemeManager.TextPrimary;
                }
            }

            if (c.HasChildren)
                RefreshThemeColors(c);
        }
    }

    /// <summary>Handle theme change.</summary>
    private void OnThemeChanged()
    {
        BackColor = ThemeManager.SurfaceBackground;
        statusStrip.BackColor = ThemeManager.SurfaceBackground;

        RefreshThemeColors(this);
        Invalidate(true);
    }

    private void BuildMenu()
    {
        menuStrip = new MenuStrip
        {
            Renderer = new FlatMenuRenderer()
        };
        fileMenu = new ToolStripMenuItem(AppLocale.Get("Menu.File"));
        openMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Open"), null, OnOpen) { ShortcutKeys = Keys.Control | Keys.O };
        saveMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Save"), null, OnSave) { ShortcutKeys = Keys.Control | Keys.S };
        saveAsMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.SaveAs"), null, OnSaveAs) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S };
        _refreshMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Refresh"), null, OnRefresh) { ShortcutKeys = Keys.Control | Keys.R };
        exitMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Exit"), null, (_, _) => Close());

        var langMenu = new ToolStripMenuItem(AppLocale.Get("Menu.Language"));
        _langEnItem = new ToolStripMenuItem(AppLocale.Get("Menu.LangEN"), null, (_, _) => AppLocale.SetLanguage(AppLocale.Lang.EN));
        _langZhItem = new ToolStripMenuItem(AppLocale.Get("Menu.LangZH"), null, (_, _) => AppLocale.SetLanguage(AppLocale.Lang.ZH));
        langMenu.DropDownItems.AddRange([_langEnItem, _langZhItem]);

        var settingsMenu = new ToolStripMenuItem(AppLocale.Get("Menu.Settings"));
        settingsMenu.DropDownItems.Add(langMenu);

        _animIconItem = new ToolStripMenuItem(AppLocale.Get("Menu.AnimatedIcons"))
        {
            Checked = SettingsManager.EnableAnimatedIcons,
            CheckOnClick = true
        };
        _animIconItem.Click += (_, _) =>
        {
            SettingsManager.EnableAnimatedIcons = _animIconItem.Checked;
            SettingsManager.Save();
        };
        settingsMenu.DropDownItems.Add(_animIconItem);

        _verticalLabelsItem = new ToolStripMenuItem(AppLocale.Get("Menu.VerticalLabels"))
        {
            Checked = SettingsManager.VerticalEquipLabels,
            CheckOnClick = true
        };
        _verticalLabelsItem.Click += (_, _) =>
        {
            SettingsManager.VerticalEquipLabels = _verticalLabelsItem.Checked;
            SettingsManager.Save();
            RebuildEquipmentRow();
        };
        settingsMenu.DropDownItems.Add(_verticalLabelsItem);

        settingsMenu.DropDownItems.Add(new ToolStripSeparator());

        // Browser display mode: details rows or large-icon card grid
        var browserViewMenu = new ToolStripMenuItem(AppLocale.Get("Menu.BrowserView"));
        _browserViewDetailsItem = new ToolStripMenuItem(AppLocale.Get("Menu.ViewDetails"),
            null, (_, _) => ApplyBrowserViewMode(BrowserViewMode.Details));
        _browserViewLargeItem = new ToolStripMenuItem(AppLocale.Get("Menu.ViewLargeIcons"),
            null, (_, _) => ApplyBrowserViewMode(BrowserViewMode.LargeIcons));
        browserViewMenu.DropDownItems.AddRange([_browserViewDetailsItem, _browserViewLargeItem]);
        settingsMenu.DropDownItems.Add(browserViewMenu);

        // Large-icon card size
        var iconSizeMenu = new ToolStripMenuItem(AppLocale.Get("Menu.IconSize"));
        _iconSize32Item = new ToolStripMenuItem(AppLocale.Get("Menu.IconSize32"), null, (_, _) => ApplyBrowserIconSize(32));
        _iconSize48Item = new ToolStripMenuItem(AppLocale.Get("Menu.IconSize48"), null, (_, _) => ApplyBrowserIconSize(48));
        _iconSize64Item = new ToolStripMenuItem(AppLocale.Get("Menu.IconSize64"), null, (_, _) => ApplyBrowserIconSize(64));
        iconSizeMenu.DropDownItems.AddRange([_iconSize32Item, _iconSize48Item, _iconSize64Item]);
        settingsMenu.DropDownItems.Add(iconSizeMenu);
        ApplyBrowserViewCheckState();

        var darkModeItem = new ToolStripMenuItem(AppLocale.Get("Menu.DarkMode"))
        {
            Checked = SettingsManager.DarkMode,
            CheckOnClick = true
        };
        darkModeItem.Click += (_, _) =>
        {
            SettingsManager.DarkMode = darkModeItem.Checked;
            SettingsManager.Save();
            MessageBox.Show(AppLocale.Get("Dialog.RestartTheme"),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Restart();
            Environment.Exit(0);
        };
        settingsMenu.DropDownItems.Add(darkModeItem);

        var debugItem = new ToolStripMenuItem(AppLocale.Get("Menu.DebugLog"));
        debugItem.Click += (_, _) =>
        {
            DebugLog.Enabled = !DebugLog.Enabled;
            debugItem.Checked = DebugLog.Enabled;
            statusLabel.Text = DebugLog.Enabled ? "Debug: ON (see debug_plr.log)" : "Debug: OFF";
        };
        settingsMenu.DropDownItems.Add(debugItem);

        fileMenu.DropDownItems.AddRange([openMenuItem, _refreshMenuItem!, saveMenuItem, new ToolStripSeparator(), saveAsMenuItem, new ToolStripSeparator(), exitMenuItem]);
        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(settingsMenu);
        Controls.Add(menuStrip);

        // Right-click context menu with Refresh
        _contextMenu = new ContextMenuStrip();
        _contextRefreshItem = new ToolStripMenuItem(AppLocale.Get("Menu.Refresh"), null, OnRefresh) { ShortcutKeys = Keys.Control | Keys.R };
        _contextMenu.Items.Add(_contextRefreshItem);
        ContextMenuStrip = _contextMenu;
    }

    /// <summary>Switch both browsers' view mode (details / large icons).</summary>
    private void ApplyBrowserViewMode(BrowserViewMode mode)
    {
        SettingsManager.BrowserViewMode = mode;
        SettingsManager.Save();
        ApplyBrowserViewCheckState();
        SetBrowserView();
    }

    /// <summary>Change the large-icon card size for both browsers.</summary>
    private void ApplyBrowserIconSize(int size)
    {
        SettingsManager.BrowserIconSize = size;
        SettingsManager.Save();
        ApplyBrowserViewCheckState();
        SetBrowserView();
    }

    /// <summary>Apply the view mode and icon size to both browsers.</summary>
    private void SetBrowserView()
    {
        _browserItems.SetView(SettingsManager.BrowserViewMode, SettingsManager.BrowserIconSize);
        _browserBuffs.SetView(SettingsManager.BrowserViewMode, SettingsManager.BrowserIconSize);
    }

    /// <summary>Synchronize the check state of the browser view menu items with settings.</summary>
    private void ApplyBrowserViewCheckState()
    {
        _browserViewDetailsItem!.Checked = SettingsManager.BrowserViewMode == BrowserViewMode.Details;
        _browserViewLargeItem!.Checked = SettingsManager.BrowserViewMode == BrowserViewMode.LargeIcons;
        _iconSize32Item!.Checked = SettingsManager.BrowserIconSize == 32;
        _iconSize48Item!.Checked = SettingsManager.BrowserIconSize == 48;
        _iconSize64Item!.Checked = SettingsManager.BrowserIconSize == 64;
    }

    private void BuildStatusBar()
    {
        statusStrip = new StatusStrip
        {
            Renderer = new FlatMenuRenderer(),
            BackColor = ThemeManager.SurfaceBackground
        };
        statusLabel = new ToolStripStatusLabel(AppLocale.Get("Status.Ready"));
        statusProgress = new ToolStripProgressBar { Visible = false, Width = 120 };
        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(statusProgress);
        Controls.Add(statusStrip);
    }

    private void BuildTabControl()
    {
        tabControl = new TabControl { Dock = DockStyle.Fill };

        var pages = new TabPage[]
        {
            BuildPlayerInfoTab(),
            BuildAppearanceTab(),
            BuildItemsTab(),
            BuildBuffsTab(),
            BuildUpgradesMiscTab(),
            BuildSpawnPointsTab()
        };

        tabControl.TabPages.AddRange(pages);

        // Load browser data immediately (no player needed)
        _browserItems.LoadItems();
        _browserBuffs.LoadItems();
        _buffMod.PopulateBuffs();

        Controls.Add(tabControl);
        tabControl.BringToFront();
    }

    #endregion

    #region Tab Pages Construction

    private TabPage BuildPlayerInfoTab()
    {
        tabPlayerInfo = new TabPage(AppLocale.Get("Tab.PlayerInfo")) { UseVisualStyleBackColor = true };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };

        // ── Basic Info Table ──
        var infoTbl = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(4) };
        infoTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        infoTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        lblPlayerName = new Label { Text = AppLocale.Get("Info.Name"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayerName = new TextBox { Dock = DockStyle.Left, Width = 280 };
        lblDifficulty = new Label { Text = AppLocale.Get("Info.Difficulty"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbDifficulty = new ComboBox { Dock = DockStyle.Left, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDifficulty.Items.AddRange(DifficultyNames());
        lblPlayTime = new Label { Text = AppLocale.Get("Info.PlayTime"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayTime = new TextBox { Dock = DockStyle.Left, Width = 150, ReadOnly = true };
        lblFileVersion = new Label { Text = AppLocale.Get("Info.FileVersion"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtFileVersion = new TextBox { Dock = DockStyle.Left, Width = 100, ReadOnly = true };
        lblLoadout = new Label { Text = AppLocale.Get("Info.Loadout"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbCurrentLoadout = new ComboBox { Dock = DockStyle.Left, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCurrentLoadout.Items.AddRange(LoadoutNames());

        AddRow(infoTbl, 0, lblPlayerName, txtPlayerName);
        AddRow(infoTbl, 1, lblDifficulty, cmbDifficulty);
        AddRow(infoTbl, 2, lblPlayTime, txtPlayTime);
        AddRow(infoTbl, 3, lblFileVersion, txtFileVersion);
        AddRow(infoTbl, 4, lblLoadout, cmbCurrentLoadout);

        // ── Health ──
        grpHealth = new FlatGroupBox { Text = AppLocale.Get("Stats.Health"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var healthTbl = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, Padding = new Padding(10, 20, 10, 6) };
        healthTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        healthTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        healthTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        healthTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        lblHealth = new Label { Text = AppLocale.Get("Stats.Current") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudHealth = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 600 };
        lblMaxHealth = new Label { Text = AppLocale.Get("Stats.Max") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudMaxHealth = new NumericUpDown { Width = 80, Minimum = 100, Maximum = 600, Increment = 20 };
        healthTbl.Controls.Add(lblHealth, 0, 0);
        healthTbl.Controls.Add(nudHealth, 1, 0);
        healthTbl.Controls.Add(lblMaxHealth, 2, 0);
        healthTbl.Controls.Add(nudMaxHealth, 3, 0);
        grpHealth.Controls.Add(healthTbl);

        // ── Mana ──
        grpMana = new FlatGroupBox { Text = AppLocale.Get("Stats.Mana"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var manaTbl = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, Padding = new Padding(10, 20, 10, 6) };
        manaTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        manaTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        manaTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        manaTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        lblMana = new Label { Text = AppLocale.Get("Stats.Current") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudMana = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 400 };
        lblMaxMana = new Label { Text = AppLocale.Get("Stats.Max") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudMaxMana = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 400, Increment = 20 };
        manaTbl.Controls.Add(lblMana, 0, 0);
        manaTbl.Controls.Add(nudMana, 1, 0);
        manaTbl.Controls.Add(lblMaxMana, 2, 0);
        manaTbl.Controls.Add(nudMaxMana, 3, 0);
        grpMana.Controls.Add(manaTbl);

        // Health + Mana side by side
        var hpMpRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        hpMpRow.Controls.Add(grpHealth);
        grpMana.Margin = new Padding(16, 0, 0, 0);
        hpMpRow.Controls.Add(grpMana);

        // ── Counters ──
        grpCounters = new FlatGroupBox { Text = AppLocale.Get("Stats.Counters"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var countersTbl = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(10, 20, 10, 6) };
        countersTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        countersTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        for (int c = 0; c < 5; c++) countersTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblDeathsPvE = new Label { Text = AppLocale.Get("Stats.DeathsPvE") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudDeathsPvE = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblDeathsPvP = new Label { Text = AppLocale.Get("Stats.DeathsPvP") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudDeathsPvP = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblTaxMoney = new Label { Text = AppLocale.Get("Stats.TaxMoney") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudTaxMoney = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblAnglerQuests = new Label { Text = AppLocale.Get("Stats.AnglerQuests") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudAnglerQuests = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblGolferScore = new Label { Text = AppLocale.Get("Stats.GolferScore") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudGolferScore = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };

        countersTbl.Controls.Add(lblDeathsPvE, 0, 0);
        countersTbl.Controls.Add(nudDeathsPvE, 1, 0);
        countersTbl.Controls.Add(lblDeathsPvP, 0, 1);
        countersTbl.Controls.Add(nudDeathsPvP, 1, 1);
        countersTbl.Controls.Add(lblTaxMoney, 0, 2);
        countersTbl.Controls.Add(nudTaxMoney, 1, 2);
        countersTbl.Controls.Add(lblAnglerQuests, 0, 3);
        countersTbl.Controls.Add(nudAnglerQuests, 1, 3);
        countersTbl.Controls.Add(lblGolferScore, 0, 4);
        countersTbl.Controls.Add(nudGolferScore, 1, 4);
        grpCounters.Controls.Add(countersTbl);

        layout.Controls.Add(infoTbl);
        layout.Controls.Add(hpMpRow);
        layout.Controls.Add(grpCounters);

        tabPlayerInfo.Controls.Add(layout);
        return tabPlayerInfo;
    }

    private TabPage BuildAppearanceTab()
    {
        tabAppearance = new TabPage("Appearance");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        // Hair & Skin row
        var topRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Width = 1100 };
        lblHairStyle = new Label { Text = AppLocale.Get("Appearance.HairStyle") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudHairStyle = new NumericUpDown { Width = 80, Minimum = 0, Maximum = int.MaxValue };
        lblHairDye = new Label { Text = "  " + AppLocale.Get("Appearance.HairDye") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudHairDye = new NumericUpDown { Width = 80, Minimum = 0, Maximum = int.MaxValue };
        lblSkinVariant = new Label { Text = "  " + AppLocale.Get("Appearance.Skin") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        cmbSkinVariant = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSkinVariant.Items.AddRange(GenderNames());
        topRow.Controls.AddRange([lblHairStyle, nudHairStyle, lblHairDye, nudHairDye, lblSkinVariant, cmbSkinVariant]);

        // Color pickers — each row has 4 label+swatch+button groups inside FlowLayoutPanels
        grpColors = new FlatGroupBox { Text = AppLocale.Get("Appearance.Colors"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var colorsPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Padding = new Padding(10, 20, 10, 10) };
        colorButtons = new Button[7];
        colorPanels = new Panel[7];
        lblColors = new Label[7];
        _tempColors = new byte[7][];
        for (int row = 0; row < 2; row++)
        {
            var rowPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            int colsInRow = row == 0 ? 4 : 3;
            for (int c = 0; c < colsInRow; c++)
            {
                int i = row * 4 + c;
                _tempColors[i] = new byte[3];
                var group = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 12, 4) };
                lblColors[i] = new Label { Text = ColorNames()[i] + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
                colorPanels[i] = new Panel { Width = 36, Height = 22, BorderStyle = BorderStyle.None, BackColor = Color.White, Margin = new Padding(2, 0, 2, 0) };
                colorButtons[i] = new Button { Text = AppLocale.Get("Appearance.Pick"), AutoSize = true, Margin = new Padding(2, 0, 0, 0) };
                int idx = i;
                colorButtons[i].Click += (_, _) => PickColor(idx);
                group.Controls.AddRange([lblColors[i], colorPanels[i], colorButtons[i]]);
                rowPanel.Controls.Add(group);
            }
            colorsPanel.Controls.Add(rowPanel);
        }
        grpColors.Controls.Add(colorsPanel);

        // Visibility toggles
        grpVisibility = new FlatGroupBox { Text = AppLocale.Get("Appearance.Visibility"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var visGrid = new TableLayoutPanel { AutoSize = true, ColumnCount = 5, Padding = new Padding(10, 20, 10, 10) };
        for (int c = 0; c < 5; c++) visGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        chkHideVisual = new CheckBox[10];
        for (int i = 0; i < 10; i++)
        {
            chkHideVisual[i] = new CheckBox { Text = i < HideVisualNames().Length ? HideVisualNames()[i] : $"Visual{i}", AutoSize = true };
            visGrid.Controls.Add(chkHideVisual[i], i % 5, i / 5);
        }
        grpVisibility.Controls.Add(visGrid);

        mainPanel.Controls.AddRange([topRow, grpColors, grpVisibility]);
        tabAppearance.Controls.Add(mainPanel);
        return tabAppearance;
    }

    private TabPage BuildItemsTab()
    {
        tabItems = new TabPage("Items");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };

        _splitItems = split;
        _allItemGrids = [];

        // === LEFT: Shared ItemBrowser ===
        _browserItems = new ItemBrowser();
        split.Panel1.Controls.Add(_browserItems);

        // === RIGHT: Fixed Modifier + Scrollable 3-section panel ===
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(5) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _modItems = new ItemModifier { Dock = DockStyle.Top, ShowStack = true, ShowPrefix = true, ShowFavorite = true };
        right.Controls.Add(_modItems, 0, 0);

        // Scrollable container with all 3 sections
        _scrollPanelItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var sectionsLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };

        _grpInventorySection = BuildInventorySection();
        _grpEquipmentSection = BuildEquipmentSection();
        _grpStorageSection = BuildStorageSection();

        sectionsLayout.Controls.Add(_grpInventorySection);
        sectionsLayout.Controls.Add(_grpEquipmentSection);
        sectionsLayout.Controls.Add(_grpStorageSection);

        // Stretch section panels to fill available width
        _scrollPanelItems.SizeChanged += (s, e) =>
        {
            int w = _scrollPanelItems.ClientSize.Width - 20;
            if (w > 200)
            {
                _grpInventorySection.Width = w;
                _grpEquipmentSection.Width = w;
                _grpStorageSection.Width = w;
            }
        };
        _scrollPanelItems.Controls.Add(sectionsLayout);
        right.Controls.Add(_scrollPanelItems, 0, 1);
        split.Panel2.Controls.Add(right);

        // === Events ===
        _modItems.SetClicked += OnModSet;
        _modItems.ClearClicked += OnModClear;
        _browserItems.ItemSelected += OnBrowserItemSelect;

        // Global deselection: when ANY SlotGrid selects a slot, clear all OTHER grids
        SlotGrid.BeforeAnySlotSelected += (selectedGrid) =>
        {
            foreach (var g in _allItemGrids)
            {
                if (g != selectedGrid)
                    g.ClearSelection();
            }
        };

        tabItems.Controls.Add(split);
        return tabItems;
    }

    private FlatGroupBox BuildInventorySection()
    {
        var grp = new FlatGroupBox { Text = AppLocale.Get("Tab.Inventory"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        _gridInventory = new SlotGrid(10, 5, enableHotbarColor: true, gridTitle: AppLocale.Get("Grid.MainInventory")) { Tag = "Inv" };
        _allItemGrids.Add(_gridInventory);
        _gridCoins = new SlotGrid(1, 4, gridTitle: AppLocale.Get("Grid.Coins")) { Tag = "Coins" };
        _allItemGrids.Add(_gridCoins);
        _gridAmmo = new SlotGrid(1, 4, gridTitle: AppLocale.Get("Grid.Ammo")) { Tag = "Ammo" };
        _allItemGrids.Add(_gridAmmo);

        // Layout like the game: main inventory (10×5) on the left, coins and
        // ammo as vertical single-column grids (coins first, ammo second) on the
        // right, separated by a gap. A table layout with 50px cells places the
        // coins/ammo cells exactly on the inventory grid lines (titles share the
        // 16px first row).
        var invGrid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 13,
            RowCount = 6,
            // Box internal padding; left edge aligned with the other sections
            Margin = new Padding(5, 5, 10, 10)
        };
        for (int c = 0; c < 10; c++)
            invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); // gap before coins/ammo
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // coins
        invGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // ammo
        invGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16)); // grid titles
        for (int r = 0; r < 5; r++)
            invGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        // Zero margins so the table cells land exactly on the 50px grid
        // (a default 3px margin would shrink the grids and misalign the cells).
        _gridInventory.Margin = new Padding(0);
        _gridCoins.Margin = new Padding(0);
        _gridAmmo.Margin = new Padding(0);

        invGrid.Controls.Add(_gridInventory, 0, 0);
        invGrid.SetColumnSpan(_gridInventory, 10);
        invGrid.SetRowSpan(_gridInventory, 6);
        invGrid.Controls.Add(_gridCoins, 11, 0);
        invGrid.SetRowSpan(_gridCoins, 5);
        invGrid.Controls.Add(_gridAmmo, 12, 0);
        invGrid.SetRowSpan(_gridAmmo, 5);

        var innerLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
        innerLayout.Controls.Add(invGrid);
        grp.Controls.Add(innerLayout);
        // Events
        _gridInventory.SlotSelected += (s, idx) => OnGridSlotSelected(_gridInventory, idx, _player?.MainInventory, "inv");
        _gridCoins.SlotSelected += (s, idx) => OnGridSlotSelected(_gridCoins, idx, _player?.Coins, "coins");
        _gridAmmo.SlotSelected += (s, idx) => OnGridSlotSelected(_gridAmmo, idx, _player?.Ammo, "ammo");

        return grp;
    }

    private FlatGroupBox BuildEquipmentSection()
    {
        var grp = new FlatGroupBox { Text = AppLocale.Get("Tab.Equipment"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };

        _equipLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(5) };

        // Loadout selector
        _loadoutSelector = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 5) };
        _rbLoadout1 = new RadioButton { Text = AppLocale.Get("Loadout.Select1"), Width = 90, Checked = true };
        _rbLoadout2 = new RadioButton { Text = AppLocale.Get("Loadout.Select2"), Width = 90 };
        _rbLoadout3 = new RadioButton { Text = AppLocale.Get("Loadout.Select3"), Width = 90 };
        _rbLoadout1.CheckedChanged += (s, e) => { if (_rbLoadout1.Checked) OnLoadoutSwitch(0); };
        _rbLoadout2.CheckedChanged += (s, e) => { if (_rbLoadout2.Checked) OnLoadoutSwitch(1); };
        _rbLoadout3.CheckedChanged += (s, e) => { if (_rbLoadout3.Checked) OnLoadoutSwitch(2); };
        _loadoutSelector.Controls.AddRange([_rbLoadout1, _rbLoadout2, _rbLoadout3]);
        _equipLayout.Controls.Add(_loadoutSelector);

        // Vertical slot grids
        _armorDyeSlots = [new SlotGrid(1, 3)]; _armorDyeSlots[0].Tag = "DyeArmor"; _allItemGrids.Add(_armorDyeSlots[0]);
        _vanitySlots   = [new SlotGrid(1, 3)]; _vanitySlots[0].Tag = "EquipVanity"; _allItemGrids.Add(_vanitySlots[0]);
        _equipSlots    = [new SlotGrid(1, 3)]; _equipSlots[0].Tag = "EquipArmor"; _allItemGrids.Add(_equipSlots[0]);
        _accDyeSlots   = [new SlotGrid(1, 7)]; _accDyeSlots[0].Tag = "DyeAcc"; _allItemGrids.Add(_accDyeSlots[0]);
        _vaccSlots     = [new SlotGrid(1, 7)]; _vaccSlots[0].Tag = "EquipVAcc"; _allItemGrids.Add(_vaccSlots[0]);
        _accSlots      = [new SlotGrid(1, 7)]; _accSlots[0].Tag = "EquipAcc"; _allItemGrids.Add(_accSlots[0]);
        _miscDyeSlots  = [new SlotGrid(1, 5)]; _miscDyeSlots[0].Tag = "DyeMisc"; _allItemGrids.Add(_miscDyeSlots[0]);
        _miscSlots     = [new SlotGrid(1, 5)]; _miscSlots[0].Tag = "EquipMisc"; _allItemGrids.Add(_miscSlots[0]);

        // Mark each misc slot's content (pet, light pet, minecart, mount, hook)
        _miscSlots[0].SetCellLabels([
            AppLocale.Get("Misc.Pet"), AppLocale.Get("Misc.LightPet"),
            AppLocale.Get("Misc.Minecart"), AppLocale.Get("Misc.Mount"),
            AppLocale.Get("Misc.Hook")
        ]);

        // Build the equipment row using the user's label arrangement preference
        RebuildEquipmentRow();

        // Wire all equip slot grids + attach right-click context menu
        void WireEquipGrid(SlotGrid g) { g.SlotSelected += (s, idx) => OnGridSlotSelected(g, idx, null, "equip"); g.ContextMenuStrip = _contextMenu; }
        foreach (var grid in _equipSlots) WireEquipGrid(grid);
        foreach (var grid in _vanitySlots) WireEquipGrid(grid);
        foreach (var grid in _accSlots) WireEquipGrid(grid);
        foreach (var grid in _vaccSlots) WireEquipGrid(grid);
        foreach (var grid in _miscSlots) WireEquipGrid(grid);
        foreach (var grid in _armorDyeSlots) WireEquipGrid(grid);
        foreach (var grid in _accDyeSlots) WireEquipGrid(grid);
        foreach (var grid in _miscDyeSlots) WireEquipGrid(grid);

        grp.Controls.Add(_equipLayout);
        return grp;
    }

    /// <summary>
    /// Vertical column label text for the equipment grids: a trailing "(…)"
    /// annotation is stripped; CJK text is written one character per line,
    /// English is split by word.
    /// </summary>
    private static string VerticalText(string text)
    {
        var clean = System.Text.RegularExpressions.Regex.Replace(text, @"\s*\([^)]*\)$", "");
        return clean.Any(c => c > 127)
            ? string.Join("\n", clean.ToCharArray())
            : string.Join("\n", clean.Split(' '));
    }

    /// <summary>Equipment column label text per the user's arrangement preference.</summary>
    private static string EquipLabelText(string text)
    {
        return SettingsManager.VerticalEquipLabels
            ? VerticalText(text)
            : System.Text.RegularExpressions.Regex.Replace(text, @"\s*\([^)]*\)$", "");
    }

    private FlowLayoutPanel? _equipLayout;
    private Control? _equipRowContainer;

    /// <summary>Rebuild the equipment columns using the current label arrangement setting.</summary>
    private void RebuildEquipmentRow()
    {
        if (_equipLayout == null) return;
        if (_equipRowContainer != null)
            _equipLayout.Controls.Remove(_equipRowContainer);
        _equipRowContainer = BuildEquipmentRow(SettingsManager.VerticalEquipLabels);
        _equipLayout.Controls.Add(_equipRowContainer);
    }

    /// <summary>
    /// Build the equipment columns in three groups (armor | accessories | misc).
    /// In vertical mode the label sits on the grid's left (top-aligned, CJK one
    /// character per line, English by word) with a smaller column gap. In
    /// horizontal mode the labels sit in a row above the grids, so all grids
    /// share the same Y (below the tallest label); words that cannot fit on one
    /// line are truncated with "…" instead of wrapping.
    /// </summary>
    private Control BuildEquipmentRow(bool vertical)
    {
        int gap = vertical ? 4 : 8;

        static Label MakeVerticalLabel(string text)
        {
            return new Label
            {
                Text = VerticalText(text),
                AutoSize = true,
                Font = ThemeManager.Typography.Caption,
                ForeColor = ThemeManager.TextSecondary,
                Margin = new Padding(0, 0, 4, 0),
                TextAlign = ContentAlignment.TopLeft,
                Tag = "secondary"
            };
        }

        Label MakeHorizontalLabel(string text, int gridWidth)
        {
            var clean = System.Text.RegularExpressions.Regex.Replace(text, @"\s*\([^)]*\)$", "");
            return new Label
            {
                // Single line: overlong text is truncated with "…" instead of
                // wrapping, so all grids in the row stay aligned.
                Text = FitSingleLine(clean, gridWidth + 9, ThemeManager.Typography.Caption),
                AutoSize = true,
                Font = ThemeManager.Typography.Caption,
                ForeColor = ThemeManager.TextSecondary,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.TopLeft,
                Tag = "secondary"
            };
        }

        if (vertical)
        {
            // Each column: vertical label on the grid's left (top-aligned)
            FlowLayoutPanel Col(SlotGrid grid, string text, out Label lbl, int leftMargin)
            {
                lbl = MakeVerticalLabel(text);
                var col = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    Margin = new Padding(leftMargin, 0, gap, 0)
                };
                col.Controls.Add(lbl);
                col.Controls.Add(grid);
                return col;
            }

            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };

            // Group 1: Armor (dye + vanity + armor)
            var armorGroup = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            armorGroup.Controls.Add(Col(_armorDyeSlots[0], AppLocale.Get("Dyes.Armor"), out _lblArmorDye, 0));
            armorGroup.Controls.Add(Col(_vanitySlots[0], AppLocale.Get("Equip.VanityArmorRemap"), out _lblVanityArmor, 0));
            armorGroup.Controls.Add(Col(_equipSlots[0], AppLocale.Get("Equip.Armor"), out _lblArmor, 0));

            // Group 2: Accessories (dye + vanity accessories + accessories)
            var accGroup = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            accGroup.Controls.Add(Col(_accDyeSlots[0], AppLocale.Get("Dyes.Accessories"), out _lblAccDye, 20));
            accGroup.Controls.Add(Col(_vaccSlots[0], AppLocale.Get("Equip.VanityAccessories"), out _lblVAcc, 0));
            accGroup.Controls.Add(Col(_accSlots[0], AppLocale.Get("Equip.AccessoriesRemap"), out _lblAcc, 0));

            // Group 3: Misc (dyes + equipment)
            var miscGroup = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            miscGroup.Controls.Add(Col(_miscDyeSlots[0], AppLocale.Get("Dyes.Equipment"), out _lblMiscDye, 20));
            miscGroup.Controls.Add(Col(_miscSlots[0], AppLocale.Get("Equip.Misc"), out _lblMisc, 0));

            row.Controls.Add(armorGroup);
            row.Controls.Add(accGroup);
            row.Controls.Add(miscGroup);
            return row;
        }
        else
        {
            // Horizontal: labels in the first row, grids in the second — the
            // first row's height is the tallest label, so ALL grids share the
            // same Y and stay aligned even when one label wraps.
            _lblArmorDye = MakeHorizontalLabel(AppLocale.Get("Dyes.Armor"), _armorDyeSlots[0].Width);
            _lblVanityArmor = MakeHorizontalLabel(AppLocale.Get("Equip.VanityArmorRemap"), _vanitySlots[0].Width);
            _lblArmor = MakeHorizontalLabel(AppLocale.Get("Equip.Armor"), _equipSlots[0].Width);
            _lblAccDye = MakeHorizontalLabel(AppLocale.Get("Dyes.Accessories"), _accDyeSlots[0].Width);
            _lblVAcc = MakeHorizontalLabel(AppLocale.Get("Equip.VanityAccessories"), _vaccSlots[0].Width);
            _lblAcc = MakeHorizontalLabel(AppLocale.Get("Equip.AccessoriesRemap"), _accSlots[0].Width);
            _lblMiscDye = MakeHorizontalLabel(AppLocale.Get("Dyes.Equipment"), _miscDyeSlots[0].Width);
            _lblMisc = MakeHorizontalLabel(AppLocale.Get("Equip.Misc"), _miscSlots[0].Width);

            Label[] labels = [_lblArmorDye, _lblVanityArmor, _lblArmor, _lblAccDye, _lblVAcc, _lblAcc, _lblMiscDye, _lblMisc];
            SlotGrid[] grids = [_armorDyeSlots[0], _vanitySlots[0], _equipSlots[0], _accDyeSlots[0], _vaccSlots[0], _accSlots[0], _miscDyeSlots[0], _miscSlots[0]];
            int[] groupLefts = [0, 0, 0, 20, 0, 0, 20, 0]; // uniform gaps between the three groups

            var table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = labels.Length,
                RowCount = 2,
                Margin = new Padding(0)
            };
            for (int i = 0; i < labels.Length; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                labels[i].Margin = new Padding(groupLefts[i], 0, gap, 0);
                grids[i].Margin = new Padding(groupLefts[i], 0, gap, 0);
                table.Controls.Add(labels[i], i, 0);
                table.Controls.Add(grids[i], i, 1);
            }
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return table;
        }
    }

    /// <summary>
    /// Fit text onto a single line of at most <paramref name="maxWidth"/> pixels:
    /// overlong text is truncated with the localized ellipsis instead of wrapping.
    /// </summary>
    private static string FitSingleLine(string text, int maxWidth, Font font)
    {
        string ellipsis = AppLocale.Get("UI.Ellipsis");
        if (TextRenderer.MeasureText(text, font).Width <= maxWidth) return text;
        string result = "";
        for (int i = 0; i < text.Length; i++)
        {
            if (TextRenderer.MeasureText(result + text[i] + ellipsis, font).Width > maxWidth) break;
            result += text[i];
        }
        return result + ellipsis;
    }

    private FlatGroupBox BuildStorageSection()
    {
        var grp = new FlatGroupBox { Text = AppLocale.Get("Tab.Storage"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        var layout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, RowCount = 1, Padding = new Padding(5) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        tabStorageSub = new TabControl { Width = 512, Height = 230 };
        subPiggyBank = new TabPage(AppLocale.Get("Storage.PiggyBank"));
        subSafe = new TabPage(AppLocale.Get("Storage.Safe"));
        subDefenderForge = new TabPage(AppLocale.Get("Storage.DefenderForge"));
        subVoidVault = new TabPage(AppLocale.Get("Storage.VoidVault"));

        _gridPiggy = new SlotGrid(10, 4) { Tag = "Piggy" }; subPiggyBank.Controls.Add(_gridPiggy);
        _allItemGrids.Add(_gridPiggy);
        _gridSafe = new SlotGrid(10, 4) { Tag = "Safe" }; subSafe.Controls.Add(_gridSafe);
        _allItemGrids.Add(_gridSafe);
        _gridDefender = new SlotGrid(10, 4) { Tag = "Defender" }; subDefenderForge.Controls.Add(_gridDefender);
        _allItemGrids.Add(_gridDefender);
        _gridVoid = new SlotGrid(10, 4) { Tag = "Void" }; subVoidVault.Controls.Add(_gridVoid);
        _allItemGrids.Add(_gridVoid);

        // Storage slot handlers
        _gridPiggy.SlotSelected += (s, idx) => { _activeStorageIdx = idx; OnGridSlotSelected(_gridPiggy, idx, _player?.PiggyBank, "storage"); };
        _gridSafe.SlotSelected += (s, idx) => { _activeStorageIdx = idx; OnGridSlotSelected(_gridSafe, idx, _player?.Safe, "storage"); };
        _gridDefender.SlotSelected += (s, idx) => { _activeStorageIdx = idx; OnGridSlotSelected(_gridDefender, idx, _player?.DefenderForge, "storage"); };
        _gridVoid.SlotSelected += (s, idx) => { _activeStorageIdx = idx; OnGridSlotSelected(_gridVoid, idx, _player?.VoidVault, "storage"); };

        tabStorageSub.TabPages.AddRange([subPiggyBank, subSafe, subDefenderForge, subVoidVault]);
        tabStorageSub.SelectedIndexChanged += OnStorageSubTabChanged;
        layout.Controls.Add(tabStorageSub, 0, 0);

        grp.Controls.Add(layout);
        return grp;
    }

    private TabPage BuildBuffsTab()
    {
        tabBuffs = new TabPage("Buffs");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };

        _splitBuffs = split;

        _browserBuffs = new ItemBrowser { FilterMode = ItemFilterMode.BuffOnly };
        split.Panel1.Controls.Add(_browserBuffs);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(5) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblBuffTitle = new Label { Text = AppLocale.Get("Buffs.Title"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        right.Controls.Add(_lblBuffTitle, 0, 0);

        // Buff modifier (matches ItemModifier style)
        _buffMod = new Controls.BuffModifier { Dock = DockStyle.Top };
        _buffMod.SetClicked += (s, idx) => OnBuffModSet();
        _buffMod.ClearClicked += (s, idx) => OnBuffModClear();
        right.Controls.Add(_buffMod, 0, 1);

        _gridBuffs = new SlotGrid(11, 4) { IsBuffGrid = true, Tag = "Buffs" };
        _gridBuffs.SlotSelected += (s, idx) => OnBuffSlotSelected(idx);
        _browserBuffs.ItemSelected += (s, id) => OnBuffBrowserSelect(id);
        right.Controls.Add(_gridBuffs, 0, 2);

        split.Panel2.Controls.Add(right);
        tabBuffs.Controls.Add(split);
        return tabBuffs;
    }

    private TabPage BuildUpgradesMiscTab()
    {
        tabUpgrades = new TabPage(AppLocale.Get("Tab.UpgradesMisc"));
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        // ── Upgrades section ──
        var grpUpgrades = new FlatGroupBox { Text = AppLocale.Get("Tab.Upgrades"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        var upgradesPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(5) };

        chkExtraAccessory = new CheckBox { Text = AppLocale.Get("Upgrades.ExtraAccessory"), AutoSize = true, Margin = new Padding(5) };
        chkAegisCrystal = new CheckBox { Text = AppLocale.Get("Upgrades.AegisCrystal"), AutoSize = true, Margin = new Padding(5) };
        chkAegisFruit = new CheckBox { Text = AppLocale.Get("Upgrades.AegisFruit"), AutoSize = true, Margin = new Padding(5) };
        chkArcaneCrystal = new CheckBox { Text = AppLocale.Get("Upgrades.ArcaneCrystal"), AutoSize = true, Margin = new Padding(5) };
        chkGalaxyPearl = new CheckBox { Text = AppLocale.Get("Upgrades.GalaxyPearl"), AutoSize = true, Margin = new Padding(5) };
        chkGummyWorm = new CheckBox { Text = AppLocale.Get("Upgrades.GummyWorm"), AutoSize = true, Margin = new Padding(5) };
        chkAmbrosia = new CheckBox { Text = AppLocale.Get("Upgrades.Ambrosia"), AutoSize = true, Margin = new Padding(5) };
        chkArtisanBread = new CheckBox { Text = AppLocale.Get("Upgrades.ArtisanBread"), AutoSize = true, Margin = new Padding(5) };
        chkBiomeTorches = new CheckBox { Text = AppLocale.Get("Upgrades.BiomeTorches"), AutoSize = true, Margin = new Padding(5) };
        chkUsingBiomeTorches = new CheckBox { Text = AppLocale.Get("Upgrades.UsingBiomeTorches"), AutoSize = true, Margin = new Padding(5) };

        var cartPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(5) };
        lblSuperCart = new Label { Text = AppLocale.Get("Upgrades.SuperCart") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudSuperCart = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 2 };
        chkSuperCartEnabled = new CheckBox { Text = AppLocale.Get("Upgrades.SuperCartEnabled"), AutoSize = true };
        cartPanel.Controls.AddRange([lblSuperCart, nudSuperCart, chkSuperCartEnabled]);

        upgradesPanel.Controls.AddRange([
            chkExtraAccessory, chkAegisCrystal, chkAegisFruit, chkArcaneCrystal,
            chkGalaxyPearl, chkGummyWorm, chkAmbrosia, chkArtisanBread,
            chkBiomeTorches, chkUsingBiomeTorches, cartPanel
        ]);
        grpUpgrades.Controls.Add(upgradesPanel);

        // ── Misc section ──
        var grpMisc = new FlatGroupBox { Text = AppLocale.Get("Tab.Misc"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        var miscPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(5) };

        chkHotbarLocked = new CheckBox { Text = AppLocale.Get("Misc.HotbarLocked"), AutoSize = true, Margin = new Padding(5) };

        grpCooldowns = new FlatGroupBox { Text = AppLocale.Get("Misc.Cooldowns"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 5, 0, 0) };
        var cdTable = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, Padding = new Padding(10, 20, 10, 10) };
        cdTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cdTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        cdTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cdTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        lblPotionDelay = new Label { Text = AppLocale.Get("Misc.PotionDelay") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudPotionDelay = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblManaPotionDelay = new Label { Text = AppLocale.Get("Misc.ManaPotionDelay") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudManaPotionDelay = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblRestorationCd = new Label { Text = AppLocale.Get("Misc.RestorationCd") + " ", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right };
        nudRestorationCd = new NumericUpDown { Width = 100, Minimum = 0, Maximum = int.MaxValue };
        cdTable.Controls.Add(lblPotionDelay, 0, 0);
        cdTable.Controls.Add(nudPotionDelay, 1, 0);
        cdTable.Controls.Add(lblManaPotionDelay, 2, 0);
        cdTable.Controls.Add(nudManaPotionDelay, 3, 0);
        cdTable.Controls.Add(lblRestorationCd, 0, 1);
        cdTable.Controls.Add(nudRestorationCd, 1, 1);
        grpCooldowns.Controls.Add(cdTable);

        miscPanel.Controls.AddRange([chkHotbarLocked, grpCooldowns]);
        grpMisc.Controls.Add(miscPanel);

        // ── Hide Info section ──
        grpHideInfo = new FlatGroupBox { Text = AppLocale.Get("Misc.HideInfo"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        var hidePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(5) };
        var hideGrid = new TableLayoutPanel { ColumnCount = 4, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        chkHideInfo = new CheckBox[13];
        for (int i = 0; i < 13; i++)
        {
            chkHideInfo[i] = new CheckBox
            {
                Text = i < HideInfoNames().Length ? HideInfoNames()[i] : $"Info{i}",
                AutoSize = true,
                Margin = new Padding(5)
            };
            hideGrid.Controls.Add(chkHideInfo[i], i % 4, i / 4);
        }
        hidePanel.Controls.Add(hideGrid);
        grpHideInfo.Controls.Add(hidePanel);

        mainPanel.Controls.AddRange([grpUpgrades, grpMisc, grpHideInfo]);
        tabUpgrades.Controls.Add(mainPanel);
        return tabUpgrades;
    }

    private TabPage BuildSpawnPointsTab()
    {
        tabSpawnPoints = new TabPage("Spawn Points");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 85));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

        dgvSpawnPoints = new DataGridView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None, // themed border is painted by the wrapper panel
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            BackgroundColor = ThemeManager.SurfaceCard
        };
        dgvSpawnPoints.Columns.Add("WorldId", AppLocale.Get("Spawn.WorldId"));
        dgvSpawnPoints.Columns.Add("WorldName", AppLocale.Get("Spawn.WorldName"));
        dgvSpawnPoints.Columns.Add("X", AppLocale.Get("Spawn.X"));
        dgvSpawnPoints.Columns.Add("Y", AppLocale.Get("Spawn.Y"));

        // Match the item/buff browsers: themed colors + 1px themed border
        ApplySpawnGridTheme();
        ThemeManager.ThemeChanged += ApplySpawnGridTheme;
        var dgvWrap = new ThemedBorderPanel { Dock = DockStyle.Fill };
        dgvWrap.Controls.Add(dgvSpawnPoints);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5) };
        btnAddSpawn = new Button { Text = AppLocale.Get("Spawn.Add"), Width = 130 };
        btnRemoveSpawn = new Button { Text = AppLocale.Get("Spawn.Remove"), Width = 130 };
        btnAddSpawn.Click += OnAddSpawnPoint;
        btnRemoveSpawn.Click += OnRemoveSpawnPoint;
        btnPanel.Controls.AddRange([btnAddSpawn, btnRemoveSpawn]);

        layout.Controls.Add(dgvWrap, 0, 0);
        layout.Controls.Add(btnPanel, 0, 1);
        tabSpawnPoints.Controls.Add(layout);
        return tabSpawnPoints;
    }

    /// <summary>Apply theme colors to the spawn points grid (matches the item/buff browsers).</summary>
    private void ApplySpawnGridTheme()
    {
        dgvSpawnPoints.BackgroundColor = ThemeManager.SurfaceCard;
        dgvSpawnPoints.DefaultCellStyle.BackColor = ThemeManager.SurfaceCard;
        dgvSpawnPoints.DefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
        dgvSpawnPoints.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.SurfaceBackground;
        dgvSpawnPoints.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
        dgvSpawnPoints.GridColor = ThemeManager.ControlInputBorder;
        dgvSpawnPoints.EnableHeadersVisualStyles = false;
    }

    #endregion

    #region File Operations

    // Trace logging — writes directly to trace.log regardless of DebugLog setting
    private static readonly string TracePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trace.log");
    private static void TraceLog(string msg)
    {
        try { File.AppendAllText(TracePath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }

    private void OnRefresh(object? sender, EventArgs e)
    {
        TraceLog("[OnRefresh] === START ===");
        if (_player == null) { TraceLog("[OnRefresh] no player, abort"); return; }
        if (string.IsNullOrEmpty(_filePath)) { TraceLog("[OnRefresh] no file path, fallback to Open"); OnOpen(sender, e); return; }

        // Save current slot selection to restore after refresh
        var savedGrid = _activeModGrid;
        var savedSlotIdx = savedGrid?.SelectedIndex ?? -1;
        TraceLog($"[OnRefresh] saving selection: grid={savedGrid?.Tag} idx={savedSlotIdx}");

        // Sync any pending grid edits, then clear stale modifier references
        CollectEquipToLoadout();
        _activeModGrid = null;
        _activeModList = null;
        _activeModContext = "";
        TraceLog("[OnRefresh] state cleared");

        SetLoading(true);
        try
        {
            var fileBytes = File.ReadAllBytes(_filePath);
            byte[] decrypted;
            try
            {
                decrypted = PlrCrypto.Decrypt(fileBytes);
            }
            catch
            {
                decrypted = fileBytes;
            }
            PlayerData player;
            try
            {
                player = PlrFileReader.Read(decrypted);
            }
            catch
            {
                player = PlrFileReaderLegacy.Read(decrypted);
            }

            _player = player;
            PopulateAllTabs();
            // Force refresh all equip slot displays after repopulation
            foreach (var g in _equipSlots) g.RefreshAll();
            foreach (var g in _vanitySlots) g.RefreshAll();
            foreach (var g in _accSlots) g.RefreshAll();
            foreach (var g in _vaccSlots) g.RefreshAll();
            foreach (var g in _miscSlots) g.RefreshAll();
            foreach (var g in _armorDyeSlots) g.RefreshAll();
            foreach (var g in _accDyeSlots) g.RefreshAll();
            foreach (var g in _miscDyeSlots) g.RefreshAll();
            TraceLog("[OnRefresh] PopulateAllTabs + RefreshAll done");

            // Restore previous slot selection
            if (savedGrid != null && savedSlotIdx >= 0 && savedSlotIdx < savedGrid.Slots.Length)
            {
                savedGrid.SelectSlot(savedSlotIdx);
                TraceLog($"[OnRefresh] restored selection: grid={savedGrid.Tag} idx={savedSlotIdx}");
            }
            statusLabel.Text = string.Format(AppLocale.Get("Status.Loaded"), Path.GetFileName(_filePath), player.Name, player.FileVersion);
        }
        catch (Exception ex)
        {
            TraceLog($"[OnRefresh] ERROR: {ex.Message}");
            MessageBox.Show(string.Format(AppLocale.Get("Dialog.LoadError"), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = AppLocale.Get("Status.Failed");
        }
        finally
        {
            SetLoading(false);
            TraceLog("[OnRefresh] === END ===");
        }
    }

    private async void OnOpen(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = AppLocale.Get("Dialog.OpenTitle"),
            Filter = AppLocale.Get("Dialog.FileFilter"),
            InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "Players")
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        SetLoading(true);
        try
        {
            var fileBytes = await Task.Run(() => File.ReadAllBytes(dlg.FileName));
            DebugLog.Clear();
            DebugLog.Log($"File loaded: {dlg.FileName} ({fileBytes.Length} bytes)");
            DebugLog.LogHex("Raw file bytes", fileBytes);
            var player = await Task.Run(() =>
            {
                byte[] decrypted;
                try
                {
                    decrypted = PlrCrypto.Decrypt(fileBytes);
                    DebugLog.Log($"Decrypt OK: {decrypted.Length} bytes");
                    DebugLog.LogHex("Decrypted plaintext", decrypted);
                }
                catch (Exception ex)
                {
                    DebugLog.Log($"Decrypt failed: {ex.Message} — trying raw bytes");
                    decrypted = fileBytes;
                }
                // Try game format reader first, fall back to legacy format
                try
                {
                    DebugLog.Log("Trying game-format reader...");
                    return PlrFileReader.Read(decrypted);
                }
                catch (Exception ex)
                {
                    DebugLog.Log($"Game format failed: {ex.Message} — trying legacy format");
                    return PlrFileReaderLegacy.Read(decrypted);
                }
            });

            _player = player;
            _filePath = dlg.FileName;
            PopulateAllTabs();
            statusLabel.Text = string.Format(AppLocale.Get("Status.Loaded"), Path.GetFileName(dlg.FileName), player.Name, player.FileVersion);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(AppLocale.Get("Dialog.LoadError"), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = AppLocale.Get("Status.Failed");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_player == null) { MessageBox.Show(AppLocale.Get("Dialog.NoPlayer"), "Info"); return; }
        if (string.IsNullOrEmpty(_filePath)) { OnSaveAs(sender, e); return; }

        DoSave(_filePath);
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        if (_player == null) { MessageBox.Show(AppLocale.Get("Dialog.NoPlayer"), "Info"); return; }

        using var dlg = new SaveFileDialog
        {
            Title = AppLocale.Get("Dialog.SaveTitle"),
            Filter = "Terraria Player Files (*.plr)|*.plr",
            FileName = _player.Name + ".plr",
            InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "Players")
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;
        DoSave(dlg.FileName);
    }

    private async void DoSave(string path)
    {
        DebugLog.Log($"SAVING to: {path}");
        SetLoading(true);
        try
        {
            CollectAllTabs();
            var bytes = await Task.Run(() => PlrFileWriter.Write(_player!));
            DebugLog.Log($"SAVED {bytes.Length} bytes (encrypted) to: {path}");
            await Task.Run(() => File.WriteAllBytes(path, bytes));
            _filePath = path;
            statusLabel.Text = string.Format(AppLocale.Get("Status.Saved"), Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(AppLocale.Get("Dialog.SaveError"), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = AppLocale.Get("Status.SaveFailed");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading)
    {
        Enabled = !loading;
        statusProgress.Visible = loading;
        if (loading) statusLabel.Text = AppLocale.Get("Status.Working");
    }

    #endregion

    #region Unified Event Handlers (Items tab)

    /// <summary>Unified slot selection handler for all grids in the Items tab.</summary>
    private void OnGridSlotSelected(SlotGrid grid, int slotIdx, List<ItemData>? list, string context)
    {
        if (_player == null) return;

        _activeModGrid = grid;
        _activeModList = list;
        _activeModContext = context;

        // Adjust modifier visibility based on context
        switch (context)
        {
            case "inv":
            case "coins":
            case "ammo":
                _modItems.ShowStack = true;
                _modItems.ShowPrefix = true;
                _modItems.ShowFavorite = true;
                break;
            case "equip":
                _modItems.ShowStack = false;
                _modItems.ShowPrefix = true;
                _modItems.ShowFavorite = false;
                break;
            case "storage":
                _modItems.ShowStack = true;
                _modItems.ShowPrefix = true;
                _modItems.ShowFavorite = false;
                break;
        }

        // For equipment slots (list is null), read the current item from the grid slot
        var item = list != null && slotIdx < list.Count ? list[slotIdx]
            : (slotIdx < grid.Slots.Length ? (grid.Slots[slotIdx].Item ?? new ItemData()) : new ItemData());
        TraceLog($"[SlotSelect] ctx={context} grid={grid.Tag} idx={slotIdx} itemId={item.ItemId} prefix={item.Prefix} SLOT_Item={grid.Slots[slotIdx].Item?.ItemId}:{grid.Slots[slotIdx].Item?.Prefix}");
        _modItems.LoadFromSlot(slotIdx, item);
    }

    /// <summary>Unified Set handler for the shared modifier.</summary>
    private void OnModSet(object? sender, int slotIdx)
    {
        if (_player == null || _activeModGrid == null || slotIdx < 0) return;
        var item = _modItems.BuildItemData();
        TraceLog($"[OnModSet] grid={_activeModGrid.Tag} idx={slotIdx} itemId={item.ItemId} prefix={item.Prefix} listNull={_activeModList == null}");
        // Write to backing list if available (inv/coins/ammo/storage);
        // equipment slots have null list — write grid, then sync back to data model immediately
        if (_activeModList != null && slotIdx < _activeModList.Count)
            _activeModList[slotIdx] = item;
        _activeModGrid.SetSlot(slotIdx, item);
        if (_activeModList == null) { TraceLog("[OnModSet] sync -> CollectEquipToLoadout"); CollectEquipToLoadout(); }
    }

    /// <summary>Unified Clear handler for the shared modifier.</summary>
    private void OnModClear(object? sender, int slotIdx)
    {
        if (_player == null || _activeModGrid == null || slotIdx < 0) return;
        if (_activeModList != null && slotIdx < _activeModList.Count)
            _activeModList[slotIdx] = new ItemData();
        _activeModGrid.SetSlot(slotIdx, new ItemData());
        if (_activeModList == null) CollectEquipToLoadout();
    }

    /// <summary>Unified browser item select handler.</summary>
    private void OnBrowserItemSelect(object? sender, int itemId)
    {
        if (_activeModGrid == null || _activeModGrid.SelectedIndex < 0) return;
        var idx = _activeModGrid.SelectedIndex;
        var item = new ItemData { ItemId = itemId, StackSize = 1 };
        if (_activeModList != null && idx < _activeModList.Count)
            _activeModList[idx] = item;
        _activeModGrid.SetSlot(idx, item);
        _modItems.LoadFromSlot(idx, item);
        if (_activeModList == null) CollectEquipToLoadout();
    }

    private void OnLoadoutSwitch(int loadoutIdx)
    {
        if (_player == null) return;
        // Save current loadout edits before switching (skip during initial population)
        if (!_populating) CollectEquipToLoadout();
        _activeLoadout = loadoutIdx;
        // Equipment and dyes switch with the loadout; misc equips and their
        // dyes are GLOBAL (shared across all loadouts).
        switch (loadoutIdx)
        {
            case 0:
                var lo1 = _player.Loadout1;
                PopulateEquipFromData(lo1?.Armor ?? _player.Armor, lo1?.VanityArmor ?? _player.VanityArmor,
                    lo1?.Accessories ?? _player.Accessories, lo1?.VanityAccessories ?? _player.VanityAccessories,
                    _player.MiscEquips, lo1?.ArmorDyes ?? _player.ArmorDyes, _player.MiscEquipDyes);
                break;
            case 1:
                if (_player.Loadout2 != null)
                    PopulateEquipFromData(_player.Loadout2.Armor, _player.Loadout2.VanityArmor,
                        _player.Loadout2.Accessories, _player.Loadout2.VanityAccessories,
                        _player.MiscEquips, _player.Loadout2.ArmorDyes, _player.MiscEquipDyes);
                break;
            case 2:
                if (_player.Loadout3 != null)
                    PopulateEquipFromData(_player.Loadout3.Armor, _player.Loadout3.VanityArmor,
                        _player.Loadout3.Accessories, _player.Loadout3.VanityAccessories,
                        _player.MiscEquips, _player.Loadout3.ArmorDyes, _player.MiscEquipDyes);
                break;
        }
    }

    private void OnStorageSubTabChanged(object? sender, EventArgs e)
    {
        _activeStorageIdx = -1;
        _activeModGrid = null;
        _activeModList = null;
    }

    private SlotGrid? GetActiveStorageGrid()
    {
        return tabStorageSub.SelectedIndex switch
        {
            0 => _gridPiggy,
            1 => _gridSafe,
            2 => _gridDefender,
            3 => _gridVoid,
            _ => null
        };
    }

    #endregion

    #region Buff Event Handlers

    private void OnBuffSlotSelected(int idx)
    {
        var item = _gridBuffs.GetItem(idx);
        if (item != null)
        {
            _buffMod.LoadFromSlot(idx, item.ItemId, item.StackSize);
            _cachedBuffType = item.ItemId;
            _cachedBuffDur = item.StackSize;
        }
    }

    private void OnBuffBrowserSelect(int itemId)
    {
        if (_gridBuffs.SelectedIndex < 0) return;
        var idx = _gridBuffs.SelectedIndex;
        DebugLog.Log($"[Buff] BrowserSelect: slot={idx}, itemId={itemId}, cachedDur={_cachedBuffDur}");
        _player!.BuffTypes[idx] = itemId;
        _gridBuffs.SetSlot(idx, new ItemData { ItemId = itemId, StackSize = _cachedBuffDur });
        _buffMod.LoadFromSlot(idx, itemId, _cachedBuffDur);
    }

    private void OnBuffModSet()
    {
        if (_gridBuffs.SelectedIndex < 0) return;
        var idx = _gridBuffs.SelectedIndex;
        var type = _buffMod.CurrentBuffId;
        var dur = _buffMod.CurrentDuration;
        DebugLog.Log($"[Buff] ModSet: slot={idx}, type={type}, dur={dur}");
        _player!.BuffTypes[idx] = type;
        _player.BuffTimes[idx] = dur;
        _gridBuffs.SetSlot(idx, new ItemData { ItemId = type, StackSize = dur });
        _cachedBuffType = type;
        _cachedBuffDur = dur;
    }

    private void OnBuffModClear()
    {
        if (_gridBuffs.SelectedIndex < 0) return;
        var idx = _gridBuffs.SelectedIndex;
        _player!.BuffTypes[idx] = 0;
        _player.BuffTimes[idx] = 0;
        _gridBuffs.SetSlot(idx, new ItemData());
        _buffMod.LoadFromSlot(idx, 0, 0);
    }

    #endregion

    #region Populate / Collect

    private void PopulateAllTabs()
    {
        if (_player == null) return;

        _populating = true;
        try
        {

        // Tab 1: Player Info
        txtPlayerName.Text = _player.Name;
        cmbDifficulty.SelectedIndex = Math.Clamp((int)_player.Difficulty, 0, 3);
        txtPlayTime.Text = _player.PlayTimeFormatted;
        txtFileVersion.Text = VersionMapper.GetDisplayString(_player.FileVersion);
        cmbCurrentLoadout.SelectedIndex = Math.Clamp(_player.CurrentLoadout, 0, 2);

        // Tab 2: Stats
        nudHealth.Value = ClampNud(nudHealth, _player.Stats.Health);
        nudMaxHealth.Value = ClampNud(nudMaxHealth, _player.Stats.MaxHealth);
        nudMana.Value = ClampNud(nudMana, _player.Stats.Mana);
        nudMaxMana.Value = ClampNud(nudMaxMana, _player.Stats.MaxMana);
        nudDeathsPvE.Value = ClampNud(nudDeathsPvE, _player.NumberOfDeathsPvE);
        nudDeathsPvP.Value = ClampNud(nudDeathsPvP, _player.NumberOfDeathsPvP);
        nudTaxMoney.Value = ClampNud(nudTaxMoney, _player.TaxMoney);
        nudAnglerQuests.Value = ClampNud(nudAnglerQuests, _player.AnglerQuestsFinished);
        nudGolferScore.Value = ClampNud(nudGolferScore, _player.GolferScoreAccumulated);

        // Tab 3: Appearance
        nudHairStyle.Value = ClampNud(nudHairStyle, _player.Appearance.HairStyle);
        nudHairDye.Value = ClampNud(nudHairDye, _player.Appearance.HairDye);
        cmbSkinVariant.SelectedIndex = Math.Clamp(_player.Appearance.SkinVariant == 0 ? 0 : 1, 0, 1);
        var colors = new[] { _player.Appearance.HairColor, _player.Appearance.SkinColor, _player.Appearance.EyeColor, _player.Appearance.ShirtColor, _player.Appearance.UnderShirtColor, _player.Appearance.PantsColor, _player.Appearance.ShoeColor };
        for (int i = 0; i < 7; i++)
        {
            Array.Copy(colors[i], _tempColors[i], 3);
            if (colorPanels.Length > i) colorPanels[i].BackColor = Color.FromArgb(colors[i][0], colors[i][1], colors[i][2]);
        }
        for (int i = 0; i < 10; i++) if (i < _player.Appearance.HideVisual.Length) chkHideVisual[i].Checked = _player.Appearance.HideVisual[i];

        // Tab 4: Items — Inventory grids
        _gridInventory.SetItems(_player.MainInventory);
        _gridCoins.SetItems(_player.Coins);
        _gridAmmo.SetItems(_player.Ammo);

        // Tab 4: Items — Equipment (Loadout 1 = the saved loadout[0]; misc equips
        // and their dyes are GLOBAL, shared across all loadouts)
        _activeLoadout = 0;
        _rbLoadout1.Checked = true;
        var lo1 = _player.Loadout1;
        PopulateEquipFromData(lo1?.Armor ?? _player.Armor, lo1?.VanityArmor ?? _player.VanityArmor,
            lo1?.Accessories ?? _player.Accessories, lo1?.VanityAccessories ?? _player.VanityAccessories,
            _player.MiscEquips, lo1?.ArmorDyes ?? _player.ArmorDyes, _player.MiscEquipDyes);

        // Tab 4: Items — Storage
        _gridPiggy.SetItems(_player.PiggyBank);
        _gridSafe.SetItems(_player.Safe);
        _gridDefender.SetItems(_player.DefenderForge);
        _gridVoid.SetItems(_player.VoidVault);

        // Tab 5: Buffs
        var buffItems = new List<ItemData>(44);
        for (int i = 0; i < 44; i++)
        {
            buffItems.Add(new ItemData { ItemId = _player.BuffTypes[i], StackSize = _player.BuffTimes[i] });
        }
        _gridBuffs.SetItems(buffItems);

        // Tab 6: Upgrades + Misc
        chkExtraAccessory.Checked = _player.Upgrades.ExtraAccessory;
        chkAegisCrystal.Checked = _player.Upgrades.UsedAegisCrystal;
        chkAegisFruit.Checked = _player.Upgrades.UsedAegisFruit;
        chkArcaneCrystal.Checked = _player.Upgrades.UsedArcaneCrystal;
        chkGalaxyPearl.Checked = _player.Upgrades.UsedGalaxyPearl;
        chkGummyWorm.Checked = _player.Upgrades.UsedGummyWorm;
        chkAmbrosia.Checked = _player.Upgrades.UsedAmbrosia;
        chkArtisanBread.Checked = _player.Upgrades.AteArtisanBread;
        chkBiomeTorches.Checked = _player.Upgrades.UnlockedBiomeTorches;
        chkUsingBiomeTorches.Checked = _player.Upgrades.UsingBiomeTorches;
        nudSuperCart.Value = ClampNud(nudSuperCart, _player.Upgrades.UnlockedSuperCart);
        chkSuperCartEnabled.Checked = _player.Upgrades.EnabledSuperCart;
        chkHotbarLocked.Checked = _player.HotbarLocked;
        for (int i = 0; i < 13; i++) chkHideInfo[i].Checked = i < _player.HideInfo.Length && _player.HideInfo[i];
        nudPotionDelay.Value = ClampNud(nudPotionDelay, _player.PotionDelay);
        nudManaPotionDelay.Value = ClampNud(nudManaPotionDelay, _player.ManaPotionDelay);
        nudRestorationCd.Value = ClampNud(nudRestorationCd, _player.RestorationPotionCd);

        // Tab 7: Spawn Points
        dgvSpawnPoints.Rows.Clear();
        foreach (var sp in _player.SpawnPoints)
            dgvSpawnPoints.Rows.Add(sp.WorldId, sp.WorldName, sp.X, sp.Y);

        // Populate item combos in shared modifier
        _modItems.PopulateItems();
        _modItems.PopulatePrefixes();
        _browserItems.LoadItems();
        _browserBuffs.LoadItems();

        statusLabel.Text = string.Format(AppLocale.Get("Status.Loaded"),
            Path.GetFileName(_filePath ?? "file"), _player.Name,
            VersionMapper.GetDisplayString(_player.FileVersion));
        }
        finally
        {
            _populating = false;
        }
    }

    private void CollectEquipToLoadout()
    {
        if (_player == null) return;
        DebugLog.Log($"[CollectEquip] START loadout={_activeLoadout} populating={_populating}");
        // Equipment and dyes are per-loadout; misc equips and their dyes are
        // GLOBAL (shared across all loadouts).
        List<ItemData> armor, vanity, acc, vacc, armorDyes;
        if (_activeLoadout == 0)
        {
            var lo1 = _player.Loadout1 ??= new PlayerLoadout();
            armor = lo1.Armor; vanity = lo1.VanityArmor; acc = lo1.Accessories;
            vacc = lo1.VanityAccessories; armorDyes = lo1.ArmorDyes;
        }
        else if (_activeLoadout == 1 && _player.Loadout2 != null)
        {
            armor = _player.Loadout2.Armor; vanity = _player.Loadout2.VanityArmor; acc = _player.Loadout2.Accessories;
            vacc = _player.Loadout2.VanityAccessories; armorDyes = _player.Loadout2.ArmorDyes;
        }
        else if (_activeLoadout == 2 && _player.Loadout3 != null)
        {
            armor = _player.Loadout3.Armor; vanity = _player.Loadout3.VanityArmor; acc = _player.Loadout3.Accessories;
            vacc = _player.Loadout3.VanityAccessories; armorDyes = _player.Loadout3.ArmorDyes;
        }
        else return;
        List<ItemData> misc = _player.MiscEquips;
        List<ItemData> miscDyes = _player.MiscEquipDyes;

        // Columns map directly to the game's slot order (inverse of PopulateEquipFromData)
        for (int i = 0; i < _equipSlots[0].Slots.Length && i < armor.Count; i++) armor[i] = _equipSlots[0].Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _vanitySlots[0].Slots.Length && i < vanity.Count; i++) vanity[i] = _vanitySlots[0].Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _accSlots[0].Slots.Length && i < acc.Count; i++) acc[i] = _accSlots[0].Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _vaccSlots[0].Slots.Length && i < vacc.Count; i++) vacc[i] = _vaccSlots[0].Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _miscSlots[0].Slots.Length && i < misc.Count; i++) misc[i] = _miscSlots[0].Slots[i].Item ?? new ItemData();
        // Collect dyes
        for (int i = 0; i < _armorDyeSlots[0].Slots.Length && i < 3; i++) { if (i < armorDyes.Count) armorDyes[i] = _armorDyeSlots[0].Slots[i].Item ?? new ItemData(); }
        for (int i = 0; i < _accDyeSlots[0].Slots.Length && i < 7; i++) { var idx = i + 3; if (idx < armorDyes.Count) armorDyes[idx] = _accDyeSlots[0].Slots[i].Item ?? new ItemData(); }
        for (int i = 0; i < _miscDyeSlots[0].Slots.Length && i < miscDyes.Count; i++) miscDyes[i] = _miscDyeSlots[0].Slots[i].Item ?? new ItemData();
    }

    private void PopulateEquipFromData(List<ItemData> armor, List<ItemData> vanity, List<ItemData> acc,
        List<ItemData> vacc, List<ItemData> misc, List<ItemData> armorDyes, List<ItemData> miscDyes)
    {
        // Columns map directly to the game's slot order:
        // armor(3) + accessories(7) + vanity armor(3) + vanity accessories(7)
        _equipSlots[0].SetItems(armor);
        _vanitySlots[0].SetItems(vanity);
        _accSlots[0].SetItems(acc);
        _vaccSlots[0].SetItems(vacc);
        _miscSlots[0].SetItems(misc);
        _armorDyeSlots[0].SetItems(armorDyes.Take(3).ToList());
        _accDyeSlots[0].SetItems(armorDyes.Skip(3).Take(7).ToList());
        _miscDyeSlots[0].SetItems(miscDyes.Take(5).ToList());
    }

    private void CollectAllTabs()
    {
        if (_player == null) return;

        // Tab 1
        _player.Name = txtPlayerName.Text;
        _player.Difficulty = (byte)cmbDifficulty.SelectedIndex;
        _player.CurrentLoadout = cmbCurrentLoadout.SelectedIndex;

        // Tab 2
        _player.Stats.Health = (int)nudHealth.Value;
        _player.Stats.MaxHealth = (int)nudMaxHealth.Value;
        _player.Stats.Mana = (int)nudMana.Value;
        _player.Stats.MaxMana = (int)nudMaxMana.Value;
        _player.NumberOfDeathsPvE = (int)nudDeathsPvE.Value;
        _player.NumberOfDeathsPvP = (int)nudDeathsPvP.Value;
        _player.TaxMoney = (int)nudTaxMoney.Value;
        _player.AnglerQuestsFinished = (int)nudAnglerQuests.Value;
        _player.GolferScoreAccumulated = (int)nudGolferScore.Value;

        // Tab 3
        _player.Appearance.HairStyle = (int)nudHairStyle.Value;
        _player.Appearance.HairDye = (byte)nudHairDye.Value;
        _player.Appearance.SkinVariant = cmbSkinVariant.SelectedIndex == 0 ? (byte)0 : (byte)1;
        for (int i = 0; i < 10; i++) _player.Appearance.HideVisual[i] = chkHideVisual[i].Checked;
        var colorProps = new[] { _player.Appearance.HairColor, _player.Appearance.SkinColor, _player.Appearance.EyeColor, _player.Appearance.ShirtColor, _player.Appearance.UnderShirtColor, _player.Appearance.PantsColor, _player.Appearance.ShoeColor };
        for (int i = 0; i < 7; i++) Array.Copy(_tempColors[i], colorProps[i], 3);

        // Tab 4: Items — Inventory (read from SlotGrids)
        int invNonEmpty = _gridInventory.Slots.Count(s => s.Item != null && s.Item.ItemId > 0);
        DebugLog.Log($"CollectAllTabs: Grid inventory has {invNonEmpty} non-empty slots");
        for (int i = 0; i < _gridInventory.Slots.Length && i < _player.MainInventory.Count; i++)
            _player.MainInventory[i] = _gridInventory.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridCoins.Slots.Length && i < _player.Coins.Count; i++)
            _player.Coins[i] = _gridCoins.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridAmmo.Slots.Length && i < _player.Ammo.Count; i++)
            _player.Ammo[i] = _gridAmmo.Slots[i].Item ?? new ItemData();
        DebugLog.Log($"CollectAllTabs: After collect — MainInventory={_player.MainInventory.Count(x=>x.ItemId>0)} non-empty");

        // Tab 4: Items — Equipment (save to active loadout)
        CollectEquipToLoadout();

        // Tab 4: Items — Storage (collect all four containers)
        for (int i = 0; i < _gridPiggy.Slots.Length && i < _player.PiggyBank.Count; i++)
            _player.PiggyBank[i] = _gridPiggy.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridSafe.Slots.Length && i < _player.Safe.Count; i++)
            _player.Safe[i] = _gridSafe.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridDefender.Slots.Length && i < _player.DefenderForge.Count; i++)
            _player.DefenderForge[i] = _gridDefender.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridVoid.Slots.Length && i < _player.VoidVault.Count; i++)
            _player.VoidVault[i] = _gridVoid.Slots[i].Item ?? new ItemData();

        // Tab 5: Buffs
        for (int i = 0; i < 44 && i < _gridBuffs.Slots.Length; i++)
        {
            var bItem = _gridBuffs.Slots[i].Item;
            _player.BuffTypes[i] = bItem?.ItemId ?? 0;
            _player.BuffTimes[i] = bItem?.StackSize ?? 0;
        }

        // Tab 6: Upgrades + Misc
        _player.Upgrades.ExtraAccessory = chkExtraAccessory.Checked;
        _player.Upgrades.UsedAegisCrystal = chkAegisCrystal.Checked;
        _player.Upgrades.UsedAegisFruit = chkAegisFruit.Checked;
        _player.Upgrades.UsedArcaneCrystal = chkArcaneCrystal.Checked;
        _player.Upgrades.UsedGalaxyPearl = chkGalaxyPearl.Checked;
        _player.Upgrades.UsedGummyWorm = chkGummyWorm.Checked;
        _player.Upgrades.UsedAmbrosia = chkAmbrosia.Checked;
        _player.Upgrades.AteArtisanBread = chkArtisanBread.Checked;
        _player.Upgrades.UnlockedBiomeTorches = chkBiomeTorches.Checked;
        _player.Upgrades.UsingBiomeTorches = chkUsingBiomeTorches.Checked;
        _player.Upgrades.UnlockedSuperCart = (byte)nudSuperCart.Value;
        _player.Upgrades.EnabledSuperCart = chkSuperCartEnabled.Checked;
        _player.HotbarLocked = chkHotbarLocked.Checked;
        for (int i = 0; i < 13; i++) _player.HideInfo[i] = chkHideInfo[i].Checked;
        _player.PotionDelay = (int)nudPotionDelay.Value;
        _player.ManaPotionDelay = (int)nudManaPotionDelay.Value;
        _player.RestorationPotionCd = (int)nudRestorationCd.Value;

        // Tab 7: Spawn Points
        _player.SpawnPoints.Clear();
        foreach (DataGridViewRow row in dgvSpawnPoints.Rows)
        {
            if (row.IsNewRow) continue;
            _player.SpawnPoints.Add(new SpawnPointData { WorldId = GetCellInt(row, 0), WorldName = GetCellStr(row, 1), X = GetCellInt(row, 2), Y = GetCellInt(row, 3) });
        }
    }

    #endregion

    #region Misc Event Handlers

    private void OnAddSpawnPoint(object? sender, EventArgs e)
    {
        if (_player == null) return;
        _player.SpawnPoints.Add(new SpawnPointData());
        dgvSpawnPoints.Rows.Add(0, "", 0, 0);
    }

    private void OnRemoveSpawnPoint(object? sender, EventArgs e)
    {
        if (_player == null) return;
        var row = dgvSpawnPoints.CurrentRow;
        if (row == null || row.IsNewRow) return;
        if (row.Index < _player.SpawnPoints.Count) _player.SpawnPoints.RemoveAt(row.Index);
        dgvSpawnPoints.Rows.RemoveAt(row.Index);
    }

    private void PickColor(int index)
    {
        if (_player == null) return;
        using var dlg = new ColorDialog { Color = Color.FromArgb(_tempColors[index][0], _tempColors[index][1], _tempColors[index][2]) };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _tempColors[index][0] = dlg.Color.R;
            _tempColors[index][1] = dlg.Color.G;
            _tempColors[index][2] = dlg.Color.B;
            colorPanels[index].BackColor = dlg.Color;
        }
    }

    #endregion

    #region Helpers

    private static void AddRow(TableLayoutPanel layout, int row, Control label, Control ctrl)
    {
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(ctrl, 1, row);
    }

    private static int GetCellInt(DataGridViewRow row, int col) =>
        row.Cells[col].Value is int i ? i : int.TryParse(row.Cells[col].Value?.ToString(), out var v) ? v : 0;

    private static string GetCellStr(DataGridViewRow row, int col) =>
        row.Cells[col].Value?.ToString() ?? "";

    /// <summary>Clamp a value to a NumericUpDown's range.</summary>
    private static decimal ClampNud(NumericUpDown nud, decimal value) =>
        Math.Clamp(value, nud.Minimum, nud.Maximum);

    private static decimal ClampNud(NumericUpDown nud, int value) =>
        ClampNud(nud, (decimal)value);

    /// <summary>Refreshes all UI text when language changes.</summary>
    private void RefreshAllUI()
    {
        var L = (Func<string, string>)AppLocale.Get;

        // Form title
        Text = L("App.Title");

        // Menu
        fileMenu.Text = L("Menu.File");
        openMenuItem.Text = L("Menu.Open");
        saveMenuItem.Text = L("Menu.Save");
        saveAsMenuItem.Text = L("Menu.SaveAs");
        exitMenuItem.Text = L("Menu.Exit");
        if (_refreshMenuItem != null) _refreshMenuItem.Text = L("Menu.Refresh");
        if (_contextRefreshItem != null) _contextRefreshItem.Text = L("Menu.Refresh");
        if (menuStrip.Items.Count > 1)
        {
            var settingsMenu = (ToolStripMenuItem)menuStrip.Items[1];
            settingsMenu.Text = L("Menu.Settings");
            if (settingsMenu.DropDownItems.Count > 0)
            {
                var langMenu = (ToolStripMenuItem)settingsMenu.DropDownItems[0];
                langMenu.Text = L("Menu.Language");
                if (_langEnItem != null) _langEnItem.Text = L("Menu.LangEN");
                if (_langZhItem != null) _langZhItem.Text = L("Menu.LangZH");
            }
        }

        // Tab titles
        tabPlayerInfo.Text = L("Tab.PlayerInfo");
        tabAppearance.Text = L("Tab.Appearance");
        tabItems.Text = L("Tab.Items");
        tabBuffs.Text = L("Tab.Buffs");
        tabUpgrades.Text = L("Tab.UpgradesMisc");
        tabSpawnPoints.Text = L("Tab.SpawnPoints");

        // Tab 1: Player Info
        lblPlayerName.Text = L("Info.Name");
        lblDifficulty.Text = L("Info.Difficulty");
        lblPlayTime.Text = L("Info.PlayTime");
        lblFileVersion.Text = L("Info.FileVersion");
        lblLoadout.Text = L("Info.Loadout");

        // Refresh combo box items
        {
            int prevDiff = cmbDifficulty.SelectedIndex;
            cmbDifficulty.Items.Clear();
            cmbDifficulty.Items.AddRange(DifficultyNames());
            cmbDifficulty.SelectedIndex = Math.Clamp(prevDiff, 0, 3);

            int prevLoadout = cmbCurrentLoadout.SelectedIndex;
            cmbCurrentLoadout.Items.Clear();
            cmbCurrentLoadout.Items.AddRange(LoadoutNames());
            cmbCurrentLoadout.SelectedIndex = Math.Clamp(prevLoadout, 0, 2);

            int prevSkin = cmbSkinVariant.SelectedIndex;
            cmbSkinVariant.Items.Clear();
            cmbSkinVariant.Items.AddRange(GenderNames());
            cmbSkinVariant.SelectedIndex = Math.Clamp(prevSkin, 0, 1);
        }

        // Tab 1: Player Info — Stats
        grpHealth.Text = L("Stats.Health");
        lblHealth.Text = L("Stats.Current");
        lblMaxHealth.Text = L("Stats.Max");
        grpMana.Text = L("Stats.Mana");
        lblMana.Text = L("Stats.Current");
        lblMaxMana.Text = L("Stats.Max");
        grpCounters.Text = L("Stats.Counters");
        lblDeathsPvE.Text = L("Stats.DeathsPvE");
        lblDeathsPvP.Text = L("Stats.DeathsPvP");
        lblTaxMoney.Text = L("Stats.TaxMoney");
        lblAnglerQuests.Text = L("Stats.AnglerQuests");
        lblGolferScore.Text = L("Stats.GolferScore");

        // Tab 3: Appearance
        lblHairStyle.Text = L("Appearance.HairStyle");
        lblHairDye.Text = L("Appearance.HairDye");
        lblSkinVariant.Text = L("Appearance.Skin");
        grpColors.Text = L("Appearance.Colors");
        for (int i = 0; i < colorButtons.Length; i++) colorButtons[i].Text = L("Appearance.Pick");
        for (int i = 0; i < lblColors.Length; i++) lblColors[i].Text = ColorNames()[i] + ":";
        grpVisibility.Text = L("Appearance.Visibility");
        for (int i = 0; i < chkHideVisual.Length; i++)
            chkHideVisual[i].Text = L($"Appearance.{i switch {
                0 => "Head", 1 => "Body", 2 => "Legs", 3 => "VanityHead", 4 => "VanityBody",
                5 => "VanityLegs", 6 => "Acc1", 7 => "Acc2", 8 => "Acc3", 9 => "Acc4", _ => "Head"
            }}");

        // Tab 4: Items — section titles + modifier + loadout + grid titles
        _grpInventorySection.Text = L("Tab.Inventory");
        _grpEquipmentSection.Text = L("Tab.Equipment");
        // Equipment column labels (conversion follows the arrangement setting)
        _lblArmorDye.Text = EquipLabelText(L("Dyes.Armor"));
        _lblVanityArmor.Text = EquipLabelText(L("Equip.VanityArmorRemap"));
        _lblArmor.Text = EquipLabelText(L("Equip.Armor"));
        _lblMiscDye.Text = EquipLabelText(L("Dyes.Equipment"));
        _lblMisc.Text = EquipLabelText(L("Equip.Misc"));
        _lblAccDye.Text = EquipLabelText(L("Dyes.Accessories"));
        _lblVAcc.Text = EquipLabelText(L("Equip.VanityAccessories"));
        _lblAcc.Text = EquipLabelText(L("Equip.AccessoriesRemap"));
        _miscSlots[0].UpdateCellLabels([
            L("Misc.Pet"), L("Misc.LightPet"), L("Misc.Minecart"),
            L("Misc.Mount"), L("Misc.Hook")
        ]);
        _grpStorageSection.Text = L("Tab.Storage");
        _modItems.RefreshLocale();
        _gridInventory.GridTitle = L("Grid.MainInventory");
        _gridCoins.GridTitle = L("Grid.Coins");
        _gridAmmo.GridTitle = L("Grid.Ammo");
        _rbLoadout1.Text = L("Loadout.Select1");
        _rbLoadout2.Text = L("Loadout.Select2");
        _rbLoadout3.Text = L("Loadout.Select3");

        // Tab 4: Items — storage sub-tabs
        subPiggyBank.Text = L("Storage.PiggyBank");
        subSafe.Text = L("Storage.Safe");
        subDefenderForge.Text = L("Storage.DefenderForge");
        subVoidVault.Text = L("Storage.VoidVault");

        // Tab 5: Buffs
        _lblBuffTitle.Text = L("Buffs.Title");
        _buffMod.RefreshLocale();
        _buffMod.PopulateBuffs();

        // Tab 6: Upgrades + Misc
        chkExtraAccessory.Text = L("Upgrades.ExtraAccessory");
        chkAegisCrystal.Text = L("Upgrades.AegisCrystal");
        chkAegisFruit.Text = L("Upgrades.AegisFruit");
        chkArcaneCrystal.Text = L("Upgrades.ArcaneCrystal");
        chkGalaxyPearl.Text = L("Upgrades.GalaxyPearl");
        chkGummyWorm.Text = L("Upgrades.GummyWorm");
        chkAmbrosia.Text = L("Upgrades.Ambrosia");
        chkArtisanBread.Text = L("Upgrades.ArtisanBread");
        chkBiomeTorches.Text = L("Upgrades.BiomeTorches");
        chkUsingBiomeTorches.Text = L("Upgrades.UsingBiomeTorches");
        lblSuperCart.Text = L("Upgrades.SuperCart");
        chkSuperCartEnabled.Text = L("Upgrades.SuperCartEnabled");
        chkHotbarLocked.Text = L("Misc.HotbarLocked");
        grpHideInfo.Text = L("Misc.HideInfo");
        for (int i = 0; i < chkHideInfo.Length; i++)
            chkHideInfo[i].Text = L($"Info.{i switch {
                0 => "Watch", 1 => "Weather", 2 => "Depth", 3 => "Compass", 4 => "Sextant",
                5 => "Tally", 6 => "Stopwatch", 7 => "MetalDetector", 8 => "DPS",
                9 => "RareCreature", 10 => "FishingPower", 11 => "MoonPhase", 12 => "Speed", _ => "Watch"
            }}");
        grpCooldowns.Text = L("Misc.Cooldowns");
        lblPotionDelay.Text = L("Misc.PotionDelay");
        lblManaPotionDelay.Text = L("Misc.ManaPotionDelay");
        lblRestorationCd.Text = L("Misc.RestorationCd");

        // Tab 7: Spawn Points
        btnAddSpawn.Text = L("Spawn.Add");
        btnRemoveSpawn.Text = L("Spawn.Remove");
        dgvSpawnPoints.Columns["WorldId"]!.HeaderText = L("Spawn.WorldId");
        dgvSpawnPoints.Columns["WorldName"]!.HeaderText = L("Spawn.WorldName");
        dgvSpawnPoints.Columns["X"]!.HeaderText = L("Spawn.X");
        dgvSpawnPoints.Columns["Y"]!.HeaderText = L("Spawn.Y");

        // Refresh browser display text (always, even without loaded player)
        _browserItems.RefreshDisplayText();
        _browserBuffs.RefreshDisplayText();

        // Repopulate data if player is loaded (refresh display text with new language)
        if (_player != null)
        {
            SuspendLayout();
            try
            {
                // Refresh slot display text
                _gridInventory.RefreshAll();
                _gridCoins.RefreshAll();
                _gridAmmo.RefreshAll();
                _gridBuffs.RefreshAll();
                foreach (var g in _equipSlots) g.RefreshAll();
                foreach (var g in _vanitySlots) g.RefreshAll();
                foreach (var g in _accSlots) g.RefreshAll();
                foreach (var g in _vaccSlots) g.RefreshAll();
                foreach (var g in _miscSlots) g.RefreshAll();
                foreach (var g in _armorDyeSlots) g.RefreshAll();
                foreach (var g in _accDyeSlots) g.RefreshAll();
                foreach (var g in _miscDyeSlots) g.RefreshAll();
                _gridPiggy.RefreshAll();
                _gridSafe.RefreshAll();
                _gridDefender.RefreshAll();
                _gridVoid.RefreshAll();

                // Refresh modifier combo boxes
                _modItems.PopulatePrefixes();
            }
            finally
            {
                ResumeLayout();
            }
        }

        // Status bar
        if (_player != null)
            statusLabel.Text = string.Format(L("Status.Loaded"), Path.GetFileName(_filePath ?? "file"), _player.Name, VersionMapper.GetDisplayString(_player.FileVersion));
        else
            statusLabel.Text = L("Status.Ready");
    }

    #endregion
}
