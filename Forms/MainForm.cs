using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor;

public partial class MainForm : Form
{
    private PlayerData? _player;
    private string? _filePath;

    private DataGridView? _activeStorageGrid; // tracks which storage grid is selected

    private static readonly string[] DifficultyNames = { "Softcore (Classic)", "Mediumcore", "Hardcore", "Journey" };
    private static readonly string[] HideVisualNames = { "Head", "Body", "Legs", "Vanity Head", "Vanity Body", "Vanity Legs", "Acc 1", "Acc 2", "Acc 3", "Acc 4" };
    private static readonly string[] HideMiscNames = { "Pet", "Light Pet", "Minecart", "Mount", "Hook" };
    private static readonly string[] HideInfoNames = { "Watch", "Weather", "Depth", "Compass", "Sextant", "Tally", "Stopwatch", "Metal Detector", "DPS", "Rare Creature", "Fishing Power", "Moon Phase", "Speed" };
    private static readonly string[] ColorNames = { "Hair", "Skin", "Eyes", "Shirt", "UnderShirt", "Pants", "Shoes" };
    private static readonly string[] MiscEquipNames = { "Pet", "Light Pet", "Minecart", "Mount", "Hook" };

    // Temporary color storage during editing
    private byte[][] _tempColors = Array.Empty<byte[]>();

    public MainForm()
    {
        InitializeComponent();
        BuildForm();
    }

    #region Form Construction

    private void BuildForm()
    {
        Text = "Terraria Players Editor";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        MinimumSize = new Size(1000, 600);

        BuildMenu();
        BuildStatusBar();
        BuildTabControl();
    }

    private void BuildMenu()
    {
        menuStrip = new MenuStrip();
        fileMenu = new ToolStripMenuItem("File");
        openMenuItem = new ToolStripMenuItem("Open...", null, OnOpen) { ShortcutKeys = Keys.Control | Keys.O };
        saveMenuItem = new ToolStripMenuItem("Save", null, OnSave) { ShortcutKeys = Keys.Control | Keys.S };
        saveAsMenuItem = new ToolStripMenuItem("Save As...", null, OnSaveAs) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S };
        exitMenuItem = new ToolStripMenuItem("Exit", null, (_, _) => Close());

        fileMenu.DropDownItems.AddRange([openMenuItem, saveMenuItem, new ToolStripSeparator(), saveAsMenuItem, new ToolStripSeparator(), exitMenuItem]);
        menuStrip.Items.Add(fileMenu);
        Controls.Add(menuStrip);
    }

    private void BuildStatusBar()
    {
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Ready — Open a .plr file to begin.");
        statusProgress = new ToolStripProgressBar { Visible = false, Width = 120 };
        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(statusProgress);
        Controls.Add(statusStrip);
    }

    private void BuildTabControl()
    {
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Top = menuStrip.Bottom,
            Height = ClientSize.Height - menuStrip.Height - statusStrip.Height
        };

        tabControl.TabPages.AddRange([
            BuildPlayerInfoTab(),
            BuildStatsTab(),
            BuildAppearanceTab(),
            BuildInventoryTab(),
            BuildEquipmentTab(),
            BuildDyesTab(),
            BuildStorageTab(),
            BuildBuffsTab(),
            BuildUpgradesTab(),
            BuildLoadoutsTab(),
            BuildSpawnPointsTab(),
            BuildMiscTab()
        ]);

        Controls.Add(tabControl);
        tabControl.BringToFront();
    }

    #endregion

    #region Tab Pages Construction

    private TabPage BuildPlayerInfoTab()
    {
        tabPlayerInfo = new TabPage("Player Info");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(20) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        lblPlayerName = new Label { Text = "Name:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayerName = new TextBox { Dock = DockStyle.Left, Width = 300 };
        lblDifficulty = new Label { Text = "Difficulty:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbDifficulty = new ComboBox { Dock = DockStyle.Left, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDifficulty.Items.AddRange(DifficultyNames);
        lblPlayTime = new Label { Text = "Play Time:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayTime = new TextBox { Dock = DockStyle.Left, Width = 150, ReadOnly = true };
        lblFileVersion = new Label { Text = "File Version:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtFileVersion = new TextBox { Dock = DockStyle.Left, Width = 100, ReadOnly = true };
        lblLoadout = new Label { Text = "Active Loadout:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbCurrentLoadout = new ComboBox { Dock = DockStyle.Left, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCurrentLoadout.Items.AddRange(["Loadout 1", "Loadout 2", "Loadout 3"]);

        AddRow(layout, 0, lblPlayerName, txtPlayerName);
        AddRow(layout, 1, lblDifficulty, cmbDifficulty);
        AddRow(layout, 2, lblPlayTime, txtPlayTime);
        AddRow(layout, 3, lblFileVersion, txtFileVersion);
        AddRow(layout, 4, lblLoadout, cmbCurrentLoadout);

        tabPlayerInfo.Controls.Add(layout);
        return tabPlayerInfo;
    }

    private TabPage BuildStatsTab()
    {
        tabStats = new TabPage("Stats");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20) };

        grpHealth = new GroupBox { Text = "Health", Width = 350, Height = 100 };
        lblHealth = new Label { Text = "Current:", Location = new Point(15, 30), Width = 70 };
        nudHealth = new NumericUpDown { Location = new Point(90, 28), Width = 100, Minimum = 0, Maximum = 600 };
        lblMaxHealth = new Label { Text = "Max:", Location = new Point(210, 30), Width = 40 };
        nudMaxHealth = new NumericUpDown { Location = new Point(250, 28), Width = 80, Minimum = 100, Maximum = 600, Increment = 20 };
        grpHealth.Controls.AddRange([lblHealth, nudHealth, lblMaxHealth, nudMaxHealth]);

        grpMana = new GroupBox { Text = "Mana", Width = 350, Height = 100 };
        lblMana = new Label { Text = "Current:", Location = new Point(15, 30), Width = 70 };
        nudMana = new NumericUpDown { Location = new Point(90, 28), Width = 100, Minimum = 0, Maximum = 400 };
        lblMaxMana = new Label { Text = "Max:", Location = new Point(210, 30), Width = 40 };
        nudMaxMana = new NumericUpDown { Location = new Point(250, 28), Width = 80, Minimum = 0, Maximum = 400, Increment = 20 };
        grpMana.Controls.AddRange([lblMana, nudMana, lblMaxMana, nudMaxMana]);

        grpCounters = new GroupBox { Text = "Counters", Width = 350, Height = 210 };
        lblDeathsPvE = new Label { Text = "Deaths (PvE):", Location = new Point(15, 30), Width = 100 };
        nudDeathsPvE = new NumericUpDown { Location = new Point(120, 28), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblDeathsPvP = new Label { Text = "Deaths (PvP):", Location = new Point(15, 60), Width = 100 };
        nudDeathsPvP = new NumericUpDown { Location = new Point(120, 58), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblTaxMoney = new Label { Text = "Tax Money:", Location = new Point(15, 90), Width = 100 };
        nudTaxMoney = new NumericUpDown { Location = new Point(120, 88), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblAnglerQuests = new Label { Text = "Angler Quests:", Location = new Point(15, 120), Width = 100 };
        nudAnglerQuests = new NumericUpDown { Location = new Point(120, 118), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblGolferScore = new Label { Text = "Golfer Score:", Location = new Point(15, 150), Width = 100 };
        nudGolferScore = new NumericUpDown { Location = new Point(120, 148), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        grpCounters.Controls.AddRange([lblDeathsPvE, nudDeathsPvE, lblDeathsPvP, nudDeathsPvP, lblTaxMoney, nudTaxMoney, lblAnglerQuests, nudAnglerQuests, lblGolferScore, nudGolferScore]);

        var leftCol = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        leftCol.Controls.AddRange([grpHealth, grpMana]);
        var rightCol = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        rightCol.Controls.Add(grpCounters);

        layout.Controls.Add(leftCol, 0, 0);
        layout.Controls.Add(rightCol, 1, 0);
        tabStats.Controls.Add(layout);
        return tabStats;
    }

    private TabPage BuildAppearanceTab()
    {
        tabAppearance = new TabPage("Appearance");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        // Hair & Skin row
        var topRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 40, Width = 1100 };
        lblHairStyle = new Label { Text = "Hair Style:", Width = 70, TextAlign = ContentAlignment.MiddleRight };
        nudHairStyle = new NumericUpDown { Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblHairDye = new Label { Text = "Hair Dye:", Width = 70, TextAlign = ContentAlignment.MiddleRight };
        nudHairDye = new NumericUpDown { Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblSkinVariant = new Label { Text = "Skin:", Width = 50, TextAlign = ContentAlignment.MiddleRight };
        cmbSkinVariant = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSkinVariant.Items.AddRange(["Female", "Male"]);
        topRow.Controls.AddRange([lblHairStyle, nudHairStyle, lblHairDye, nudHairDye, lblSkinVariant, cmbSkinVariant]);

        // Color pickers
        grpColors = new GroupBox { Text = "Colors", Width = 800, Height = 160 };
        colorButtons = new Button[7];
        colorPanels = new Panel[7];
        _tempColors = new byte[7][];
        for (int i = 0; i < 7; i++)
        {
            _tempColors[i] = new byte[3];
            int x = 15 + (i % 4) * 190;
            int y = 25 + (i / 4) * 60;
            var lbl = new Label { Text = ColorNames[i] + ":", Location = new Point(x, y), Width = 40, TextAlign = ContentAlignment.MiddleRight };
            colorPanels[i] = new Panel { Location = new Point(x + 45, y), Width = 40, Height = 24, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            colorButtons[i] = new Button { Text = "Pick...", Location = new Point(x + 90, y - 1), Width = 55, Height = 26 };
            int idx = i;
            colorButtons[i].Click += (_, _) => PickColor(idx);
            grpColors.Controls.AddRange([lbl, colorPanels[i], colorButtons[i]]);
        }

        // Visibility toggles
        grpVisibility = new GroupBox { Text = "Visibility Toggles", Width = 800, Height = 100 };
        chkHideVisual = new CheckBox[10];
        chkHideMisc = new CheckBox[5];
        for (int i = 0; i < 10; i++)
        {
            chkHideVisual[i] = new CheckBox { Text = i < HideVisualNames.Length ? HideVisualNames[i] : $"Visual{i}", Location = new Point(15 + (i % 5) * 155, 25 + (i / 5) * 28), Width = 150 };
            grpVisibility.Controls.Add(chkHideVisual[i]);
        }

        mainPanel.Controls.AddRange([topRow, grpColors, grpVisibility]);
        tabAppearance.Controls.Add(mainPanel);
        return tabAppearance;
    }

    private TabPage BuildInventoryTab()
    {
        tabInventory = new TabPage("Inventory");
        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        // Upper: DataGridView
        dgvInventory = CreateItemGrid();
        mainPanel.Controls.Add(dgvInventory, 0, 0);

        // Lower: Edit panel + Coins/Ammo
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));

        grpInventoryEdit = new GroupBox { Text = "Edit Selected Slot", Dock = DockStyle.Fill };
        cmbItemSearch = new ComboBox { Location = new Point(15, 25), Width = 280, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        lblStack = new Label { Text = "Stack:", Location = new Point(310, 28), Width = 45 };
        nudStack = new NumericUpDown { Location = new Point(355, 26), Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblPrefix = new Label { Text = "Prefix:", Location = new Point(15, 55), Width = 45 };
        cmbPrefix = new ComboBox { Location = new Point(65, 53), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        chkFavorite = new CheckBox { Text = "Favorite", Location = new Point(240, 54), Width = 80 };
        btnSetItem = new Button { Text = "Set Item", Location = new Point(15, 85), Width = 100 };
        btnClearItem = new Button { Text = "Clear Slot", Location = new Point(125, 85), Width = 100 };
        btnSetItem.Click += OnInventorySetItem;
        btnClearItem.Click += OnInventoryClearItem;
        dgvInventory.SelectionChanged += OnInventorySelectionChanged;
        grpInventoryEdit.Controls.AddRange([cmbItemSearch, lblStack, nudStack, lblPrefix, cmbPrefix, chkFavorite, btnSetItem, btnClearItem]);

        grpCoins = new GroupBox { Text = "Coins", Dock = DockStyle.Fill };
        dgvCoins = CreateItemGrid();
        dgvCoins.Dock = DockStyle.Fill;
        grpCoins.Controls.Add(dgvCoins);

        grpAmmo = new GroupBox { Text = "Ammo", Dock = DockStyle.Fill };
        dgvAmmo = CreateItemGrid();
        dgvAmmo.Dock = DockStyle.Fill;
        grpAmmo.Controls.Add(dgvAmmo);

        bottom.Controls.Add(grpInventoryEdit, 0, 0);
        bottom.Controls.Add(grpCoins, 1, 0);
        bottom.Controls.Add(grpAmmo, 2, 0);
        mainPanel.Controls.Add(bottom, 0, 1);
        tabInventory.Controls.Add(mainPanel);
        return tabInventory;
    }

    private TabPage BuildEquipmentTab()
    {
        tabEquipment = new TabPage("Equipment");
        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };

        grpArmorSlots = new GroupBox { Text = "Armor (3)", Dock = DockStyle.Fill };
        cmbArmorSlots = new ComboBox[3];
        cmbArmorPrefixes = new ComboBox[3];
        BuildEquipmentGroup(grpArmorSlots, cmbArmorSlots, cmbArmorPrefixes, 3, ["Helmet", "Chestplate", "Leggings"]);

        grpVanityArmorSlots = new GroupBox { Text = "Vanity Armor (3)", Dock = DockStyle.Fill };
        cmbVanityArmorSlots = new ComboBox[3];
        cmbVanityArmorPrefixes = new ComboBox[3];
        BuildEquipmentGroup(grpVanityArmorSlots, cmbVanityArmorSlots, cmbVanityArmorPrefixes, 3, ["Vanity Helmet", "Vanity Chest", "Vanity Legs"]);

        grpAccessorySlots = new GroupBox { Text = "Accessories (7)", Dock = DockStyle.Fill };
        cmbAccessorySlots = new ComboBox[7];
        cmbAccessoryPrefixes = new ComboBox[7];
        BuildEquipmentGroup(grpAccessorySlots, cmbAccessorySlots, cmbAccessoryPrefixes, 7);

        grpVanityAccessorySlots = new GroupBox { Text = "Vanity Accessories (7)", Dock = DockStyle.Fill };
        cmbVanityAccessorySlots = new ComboBox[7];
        cmbVanityAccessoryPrefixes = new ComboBox[7];
        BuildEquipmentGroup(grpVanityAccessorySlots, cmbVanityAccessorySlots, cmbVanityAccessoryPrefixes, 7);

        grpMiscEquipSlots = new GroupBox { Text = "Equipment (Pet, Light Pet, Minecart, Mount, Hook)", Dock = DockStyle.Fill };
        cmbMiscEquipSlots = new ComboBox[5];
        BuildSimpleEquipmentGroup(grpMiscEquipSlots, cmbMiscEquipSlots, 5, MiscEquipNames);

        mainPanel.Controls.Add(grpArmorSlots, 0, 0);
        mainPanel.Controls.Add(grpVanityArmorSlots, 1, 0);
        mainPanel.Controls.Add(grpAccessorySlots, 0, 1);
        mainPanel.Controls.Add(grpVanityAccessorySlots, 1, 1);
        mainPanel.Controls.Add(grpMiscEquipSlots, 0, 2);
        mainPanel.SetColumnSpan(grpMiscEquipSlots, 2);

        tabEquipment.Controls.Add(mainPanel);
        return tabEquipment;
    }

    private TabPage BuildDyesTab()
    {
        tabDyes = new TabPage("Dyes");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10), AutoScroll = true };

        grpArmorDyes = new GroupBox { Text = "Armor Dyes (3)", Width = 600, Height = 160 };
        cmbArmorDyeSlots = new ComboBox[3];
        BuildSimpleEquipmentGroup(grpArmorDyes, cmbArmorDyeSlots, 3, ["Helmet Dye", "Chestplate Dye", "Leggings Dye"]);

        grpAccessoryDyes = new GroupBox { Text = "Accessory Dyes (7)", Width = 600, Height = 220 };
        cmbAccessoryDyeSlots = new ComboBox[7];
        BuildSimpleEquipmentGroup(grpAccessoryDyes, cmbAccessoryDyeSlots, 7);

        grpMiscEquipDyes = new GroupBox { Text = "Equipment Dyes (5)", Width = 600, Height = 180 };
        cmbMiscEquipDyeSlots = new ComboBox[5];
        BuildSimpleEquipmentGroup(grpMiscEquipDyes, cmbMiscEquipDyeSlots, 5, MiscEquipNames.Select(n => n + " Dye").ToArray());

        mainPanel.Controls.AddRange([grpArmorDyes, grpAccessoryDyes, grpMiscEquipDyes]);
        tabDyes.Controls.Add(mainPanel);
        return tabDyes;
    }

    private TabPage BuildStorageTab()
    {
        tabStorage = new TabPage("Storage");
        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        tabStorageSub = new TabControl { Dock = DockStyle.Fill };
        subPiggyBank = new TabPage("Piggy Bank");
        subSafe = new TabPage("Safe");
        subDefenderForge = new TabPage("Defender's Forge");
        subVoidVault = new TabPage("Void Vault");

        dgvPiggyBank = CreateItemGrid(); subPiggyBank.Controls.Add(dgvPiggyBank); dgvPiggyBank.Dock = DockStyle.Fill;
        dgvSafe = CreateItemGrid(); subSafe.Controls.Add(dgvSafe); dgvSafe.Dock = DockStyle.Fill;
        dgvDefenderForge = CreateItemGrid(); subDefenderForge.Controls.Add(dgvDefenderForge); dgvDefenderForge.Dock = DockStyle.Fill;
        dgvVoidVault = CreateItemGrid(); subVoidVault.Controls.Add(dgvVoidVault); dgvVoidVault.Dock = DockStyle.Fill;

        tabStorageSub.TabPages.AddRange([subPiggyBank, subSafe, subDefenderForge, subVoidVault]);
        tabStorageSub.SelectedIndexChanged += OnStorageTabChanged;

        grpStorageEdit = new GroupBox { Text = "Edit Storage Slot", Dock = DockStyle.Fill, Height = 130 };
        cmbStorageItemSearch = new ComboBox { Location = new Point(15, 25), Width = 280, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        lblStorageStack = new Label { Text = "Stack:", Location = new Point(310, 28), Width = 45 };
        nudStorageStack = new NumericUpDown { Location = new Point(355, 26), Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblStoragePrefix = new Label { Text = "Prefix:", Location = new Point(15, 55), Width = 45 };
        cmbStoragePrefix = new ComboBox { Location = new Point(65, 53), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        btnStorageSet = new Button { Text = "Set", Location = new Point(15, 85), Width = 80 };
        btnStorageClear = new Button { Text = "Clear", Location = new Point(105, 85), Width = 80 };
        btnStorageSet.Click += OnStorageSetItem;
        btnStorageClear.Click += OnStorageClearItem;
        grpStorageEdit.Controls.AddRange([cmbStorageItemSearch, lblStorageStack, nudStorageStack, lblStoragePrefix, cmbStoragePrefix, btnStorageSet, btnStorageClear]);

        mainPanel.Controls.Add(tabStorageSub, 0, 0);
        mainPanel.Controls.Add(grpStorageEdit, 0, 1);
        tabStorage.Controls.Add(mainPanel);
        return tabStorage;
    }

    private TabPage BuildBuffsTab()
    {
        tabBuffs = new TabPage("Buffs");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Active Buffs (44 slots) — Edit Type ID and Duration in ticks below:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        dgvBuffs = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
        dgvBuffs.Columns.Add("Type", "Buff Type ID");
        dgvBuffs.Columns.Add("Duration", "Duration (ticks)");
        layout.Controls.Add(dgvBuffs, 0, 1);
        tabBuffs.Controls.Add(layout);
        return tabBuffs;
    }

    private TabPage BuildUpgradesTab()
    {
        tabUpgrades = new TabPage("Upgrades");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        chkExtraAccessory = new CheckBox { Text = "Extra Accessory Slot (Demon Heart)", Width = 350, Margin = new Padding(5) };
        chkAegisCrystal = new CheckBox { Text = "Used Aegis Crystal (+20 HP)", Width = 350, Margin = new Padding(5) };
        chkAegisFruit = new CheckBox { Text = "Used Aegis Fruit (+20 HP)", Width = 350, Margin = new Padding(5) };
        chkArcaneCrystal = new CheckBox { Text = "Used Arcane Crystal (+20 MP)", Width = 350, Margin = new Padding(5) };
        chkGalaxyPearl = new CheckBox { Text = "Used Galaxy Pearl (+20 luck)", Width = 350, Margin = new Padding(5) };
        chkGummyWorm = new CheckBox { Text = "Used Gummy Worm (+fishing power)", Width = 350, Margin = new Padding(5) };
        chkAmbrosia = new CheckBox { Text = "Used Ambrosia (+mining speed)", Width = 350, Margin = new Padding(5) };
        chkArtisanBread = new CheckBox { Text = "Ate Artisan Bread (+build range)", Width = 350, Margin = new Padding(5) };
        chkBiomeTorches = new CheckBox { Text = "Unlocked Biome Torches", Width = 350, Margin = new Padding(5) };
        chkUsingBiomeTorches = new CheckBox { Text = "Using Biome Torches", Width = 350, Margin = new Padding(5) };

        var cartPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 40, Width = 400, Margin = new Padding(5) };
        lblSuperCart = new Label { Text = "Super Cart Level:", Width = 100, TextAlign = ContentAlignment.MiddleRight };
        nudSuperCart = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 2 };
        chkSuperCartEnabled = new CheckBox { Text = "Enabled", Width = 80 };
        cartPanel.Controls.AddRange([lblSuperCart, nudSuperCart, chkSuperCartEnabled]);

        mainPanel.Controls.AddRange([
            chkExtraAccessory, chkAegisCrystal, chkAegisFruit, chkArcaneCrystal,
            chkGalaxyPearl, chkGummyWorm, chkAmbrosia, chkArtisanBread,
            chkBiomeTorches, chkUsingBiomeTorches, cartPanel
        ]);

        tabUpgrades.Controls.Add(mainPanel);
        return tabUpgrades;
    }

    private TabPage BuildLoadoutsTab()
    {
        tabLoadouts = new TabPage("Loadouts");
        tabLoadoutSub = new TabControl { Dock = DockStyle.Fill };

        subLoadout2 = new TabPage("Loadout 2");
        subLoadout3 = new TabPage("Loadout 3");

        cmbL2Armor = BuildLoadoutTab(subLoadout2, "Loadout 2");
        cmbL3Armor = BuildLoadoutTab(subLoadout3, "Loadout 3");

        tabLoadoutSub.TabPages.AddRange([subLoadout2, subLoadout3]);
        tabLoadouts.Controls.Add(tabLoadoutSub);
        return tabLoadouts;
    }

    private ComboBox[] BuildLoadoutTab(TabPage page, string name)
    {
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10), AutoScroll = true };

        var armor = new GroupBox { Text = $"{name} — Armor (3)", Width = 700, Height = 140 };
        var cArmor = new ComboBox[3];
        BuildSimpleEquipmentGroup(armor, cArmor, 3, ["Helmet", "Chestplate", "Leggings"]);

        var vanity = new GroupBox { Text = $"{name} — Vanity Armor (3)", Width = 700, Height = 140 };
        var cVanity = new ComboBox[3];
        BuildSimpleEquipmentGroup(vanity, cVanity, 3, ["Vanity Helmet", "Vanity Chest", "Vanity Legs"]);

        var acc = new GroupBox { Text = $"{name} — Accessories (7)", Width = 700, Height = 200 };
        var cAcc = new ComboBox[7];
        BuildSimpleEquipmentGroup(acc, cAcc, 7);

        var vanityAcc = new GroupBox { Text = $"{name} — Vanity Accessories (7)", Width = 700, Height = 200 };
        var cVanityAcc = new ComboBox[7];
        BuildSimpleEquipmentGroup(vanityAcc, cVanityAcc, 7);

        var misc = new GroupBox { Text = $"{name} — Equipment (5)", Width = 700, Height = 160 };
        var cMisc = new ComboBox[5];
        BuildSimpleEquipmentGroup(misc, cMisc, 5, MiscEquipNames);

        mainPanel.Controls.AddRange([armor, vanity, acc, vanityAcc, misc]);
        page.Controls.Add(mainPanel);
        return cArmor;
    }

    private TabPage BuildSpawnPointsTab()
    {
        tabSpawnPoints = new TabPage("Spawn Points");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 85));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

        dgvSpawnPoints = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
        dgvSpawnPoints.Columns.Add("WorldId", "World ID");
        dgvSpawnPoints.Columns.Add("WorldName", "World Name");
        dgvSpawnPoints.Columns.Add("X", "X");
        dgvSpawnPoints.Columns.Add("Y", "Y");

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5) };
        btnAddSpawn = new Button { Text = "Add Spawn Point", Width = 130 };
        btnRemoveSpawn = new Button { Text = "Remove Selected", Width = 130 };
        btnAddSpawn.Click += OnAddSpawnPoint;
        btnRemoveSpawn.Click += OnRemoveSpawnPoint;
        btnPanel.Controls.AddRange([btnAddSpawn, btnRemoveSpawn]);

        layout.Controls.Add(dgvSpawnPoints, 0, 0);
        layout.Controls.Add(btnPanel, 0, 1);
        tabSpawnPoints.Controls.Add(layout);
        return tabSpawnPoints;
    }

    private TabPage BuildMiscTab()
    {
        tabMisc = new TabPage("Misc");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        chkHotbarLocked = new CheckBox { Text = "Hotbar Locked", Width = 200, Margin = new Padding(5, 5, 5, 15) };

        grpHideInfo = new GroupBox { Text = "Info Accessory Display", Width = 700, Height = 160 };
        chkHideInfo = new CheckBox[13];
        for (int i = 0; i < 13; i++)
        {
            chkHideInfo[i] = new CheckBox
            {
                Text = i < HideInfoNames.Length ? HideInfoNames[i] : $"Info{i}",
                Location = new Point(15 + (i % 4) * 170, 25 + (i / 4) * 30),
                Width = 160
            };
            grpHideInfo.Controls.Add(chkHideInfo[i]);
        }

        grpCooldowns = new GroupBox { Text = "Cooldowns (ticks)", Width = 400, Height = 130 };
        lblPotionDelay = new Label { Text = "Potion Delay:", Location = new Point(15, 30), Width = 100 };
        nudPotionDelay = new NumericUpDown { Location = new Point(120, 28), Width = 120, Minimum = 0, Maximum = int.MaxValue };
        lblManaPotionDelay = new Label { Text = "Mana Potion Delay:", Location = new Point(15, 60), Width = 100 };
        nudManaPotionDelay = new NumericUpDown { Location = new Point(120, 58), Width = 120, Minimum = 0, Maximum = int.MaxValue };
        lblRestorationCd = new Label { Text = "Restoration CD:", Location = new Point(15, 90), Width = 100 };
        nudRestorationCd = new NumericUpDown { Location = new Point(120, 88), Width = 120, Minimum = 0, Maximum = int.MaxValue };
        grpCooldowns.Controls.AddRange([lblPotionDelay, nudPotionDelay, lblManaPotionDelay, nudManaPotionDelay, lblRestorationCd, nudRestorationCd]);

        mainPanel.Controls.AddRange([chkHotbarLocked, grpHideInfo, grpCooldowns]);
        tabMisc.Controls.Add(mainPanel);
        return tabMisc;
    }

    #endregion

    #region File Operations

    private async void OnOpen(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open Terraria Player File",
            Filter = "Terraria Player Files (*.plr)|*.plr|All Files (*.*)|*.*",
            InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "Players")
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        SetLoading(true);
        try
        {
            var fileBytes = await Task.Run(() => File.ReadAllBytes(dlg.FileName));
            var player = await Task.Run(() =>
            {
                byte[] decrypted;
                try
                {
                    decrypted = PlrCrypto.Decrypt(fileBytes);
                }
                catch
                {
                    // Maybe it's unencrypted? Try reading directly
                    decrypted = fileBytes;
                }
                return PlrFileReader.Read(decrypted);
            });

            _player = player;
            _filePath = dlg.FileName;
            PopulateAllTabs();
            statusLabel.Text = $"Loaded: {Path.GetFileName(dlg.FileName)} — {player.Name} (v{player.FileVersion})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Failed to load file.";
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_player == null) { MessageBox.Show("No player data loaded.", "Info"); return; }
        if (string.IsNullOrEmpty(_filePath)) { OnSaveAs(sender, e); return; }

        DoSave(_filePath);
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        if (_player == null) { MessageBox.Show("No player data loaded.", "Info"); return; }

        using var dlg = new SaveFileDialog
        {
            Title = "Save Terraria Player File",
            Filter = "Terraria Player Files (*.plr)|*.plr",
            FileName = _player.Name + ".plr",
            InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "Players")
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;
        DoSave(dlg.FileName);
    }

    private async void DoSave(string path)
    {
        SetLoading(true);
        try
        {
            CollectAllTabs();
            var bytes = await Task.Run(() => PlrFileWriter.Write(_player!));
            await Task.Run(() => File.WriteAllBytes(path, bytes));
            _filePath = path;
            statusLabel.Text = $"Saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Failed to save file.";
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
        if (loading) statusLabel.Text = "Working...";
    }

    #endregion

    #region Populate / Collect

    private void PopulateAllTabs()
    {
        if (_player == null) return;

        // Tab 1: Player Info
        txtPlayerName.Text = _player.Name;
        cmbDifficulty.SelectedIndex = Math.Clamp((int)_player.Difficulty, 0, 3);
        txtPlayTime.Text = _player.PlayTimeFormatted;
        txtFileVersion.Text = _player.FileVersion.ToString();
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

        // Tab 4: Inventory
        PopulateItemGrid(dgvInventory, _player.MainInventory, 50);
        PopulateItemGrid(dgvCoins, _player.Coins, 4);
        PopulateItemGrid(dgvAmmo, _player.Ammo, 4);

        // Tab 5: Equipment
        PopulateEquipmentCombos(cmbArmorSlots, _player.Armor, 3);
        PopulateEquipmentCombos(cmbVanityArmorSlots, _player.VanityArmor, 3);
        PopulateEquipmentCombos(cmbAccessorySlots, _player.Accessories, _player.Accessories.Count);
        PopulateEquipmentCombos(cmbVanityAccessorySlots, _player.VanityAccessories, _player.VanityAccessories.Count);
        PopulatePrefixCombos(cmbArmorPrefixes, _player.Armor, 3);
        PopulatePrefixCombos(cmbVanityArmorPrefixes, _player.VanityArmor, 3);
        PopulatePrefixCombos(cmbAccessoryPrefixes, _player.Accessories, _player.Accessories.Count);
        PopulatePrefixCombos(cmbVanityAccessoryPrefixes, _player.VanityAccessories, _player.VanityAccessories.Count);
        PopulateEquipmentCombos(cmbMiscEquipSlots, _player.MiscEquips, 5);

        // Tab 6: Dyes
        PopulateEquipmentCombos(cmbArmorDyeSlots, _player.ArmorDyes, 3);
        PopulateEquipmentCombos(cmbAccessoryDyeSlots, _player.ArmorDyes, 7, 3);
        PopulateEquipmentCombos(cmbMiscEquipDyeSlots, _player.MiscEquipDyes, 5);

        // Tab 7: Storage
        PopulateItemGrid(dgvPiggyBank, _player.PiggyBank, 40);
        PopulateItemGrid(dgvSafe, _player.Safe, 40);
        PopulateItemGrid(dgvDefenderForge, _player.DefenderForge, 40);
        PopulateItemGrid(dgvVoidVault, _player.VoidVault, 40);

        // Tab 8: Buffs
        dgvBuffs.Rows.Clear();
        for (int i = 0; i < 44; i++)
            dgvBuffs.Rows.Add(_player.BuffTypes[i], _player.BuffTimes[i]);

        // Tab 9: Upgrades
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

        // Tab 10: Loadouts
        if (_player.Loadout2 != null) PopulateLoadoutTab(cmbL2Armor, _player.Loadout2);
        if (_player.Loadout3 != null) PopulateLoadoutTab(cmbL3Armor, _player.Loadout3);

        // Tab 11: Spawn Points
        dgvSpawnPoints.Rows.Clear();
        foreach (var sp in _player.SpawnPoints)
            dgvSpawnPoints.Rows.Add(sp.WorldId, sp.WorldName, sp.X, sp.Y);

        // Tab 12: Misc
        chkHotbarLocked.Checked = _player.HotbarLocked;
        for (int i = 0; i < 13; i++) chkHideInfo[i].Checked = i < _player.HideInfo.Length && _player.HideInfo[i];
        nudPotionDelay.Value = ClampNud(nudPotionDelay, _player.PotionDelay);
        nudManaPotionDelay.Value = ClampNud(nudManaPotionDelay, _player.ManaPotionDelay);
        nudRestorationCd.Value = ClampNud(nudRestorationCd, _player.RestorationPotionCd);

        // Populate item combos
        PopulateAllItemCombos();

        statusLabel.Text = $"Loaded: {Path.GetFileName(_filePath ?? "file")} — {_player.Name} (v{_player.FileVersion})";
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

        // Tab 4
        CollectFromGrid(dgvInventory, _player.MainInventory);
        CollectFromGrid(dgvCoins, _player.Coins);
        CollectFromGrid(dgvAmmo, _player.Ammo);

        // Tab 5
        CollectEquipmentFromCombos(cmbArmorSlots, cmbArmorPrefixes, _player.Armor);
        CollectEquipmentFromCombos(cmbVanityArmorSlots, cmbVanityArmorPrefixes, _player.VanityArmor);
        CollectEquipmentFromCombos(cmbAccessorySlots, cmbAccessoryPrefixes, _player.Accessories);
        CollectEquipmentFromCombos(cmbVanityAccessorySlots, cmbVanityAccessoryPrefixes, _player.VanityAccessories);
        CollectEquipmentFromCombos(cmbMiscEquipSlots, null, _player.MiscEquips);

        // Tab 6
        CollectDyesFromCombos(cmbArmorDyeSlots, _player.ArmorDyes, 0, 3);
        CollectDyesFromCombos(cmbAccessoryDyeSlots, _player.ArmorDyes, 3, 7);
        CollectEquipmentFromCombos(cmbMiscEquipDyeSlots, null, _player.MiscEquipDyes);

        // Tab 7
        CollectFromGrid(dgvPiggyBank, _player.PiggyBank);
        CollectFromGrid(dgvSafe, _player.Safe);
        CollectFromGrid(dgvDefenderForge, _player.DefenderForge);
        CollectFromGrid(dgvVoidVault, _player.VoidVault);

        // Tab 8
        for (int i = 0; i < 44 && i < dgvBuffs.Rows.Count; i++) { _player.BuffTypes[i] = GetCellInt(dgvBuffs, i, 0); _player.BuffTimes[i] = GetCellInt(dgvBuffs, i, 1); }

        // Tab 9
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

        // Tab 11
        _player.SpawnPoints.Clear();
        foreach (DataGridViewRow row in dgvSpawnPoints.Rows)
        {
            if (row.IsNewRow) continue;
            _player.SpawnPoints.Add(new SpawnPointData { WorldId = GetCellInt(row, 0), WorldName = GetCellStr(row, 1), X = GetCellInt(row, 2), Y = GetCellInt(row, 3) });
        }

        // Tab 12
        _player.HotbarLocked = chkHotbarLocked.Checked;
        for (int i = 0; i < 13; i++) _player.HideInfo[i] = chkHideInfo[i].Checked;
        _player.PotionDelay = (int)nudPotionDelay.Value;
        _player.ManaPotionDelay = (int)nudManaPotionDelay.Value;
        _player.RestorationPotionCd = (int)nudRestorationCd.Value;
    }

    #endregion

    #region Event Handlers

    private void OnInventorySetItem(object? sender, EventArgs e)
    {
        if (_player == null) return;
        var row = dgvInventory.CurrentRow;
        if (row == null || row.Index >= _player.MainInventory.Count) return;

        var sel = cmbItemSearch.SelectedItem as ItemDatabase.ItemLookup?;
        int id = sel?.Id ?? 0;
        if (sel == null) id = ItemDatabase.FindIdByPartialName(cmbItemSearch.Text);

        var item = _player.MainInventory[row.Index];
        item.ItemId = id;
        item.StackSize = (int)nudStack.Value;
        item.Prefix = (byte)cmbPrefix.SelectedIndex;
        item.Favorited = chkFavorite.Checked;
        PopulateItemGrid(dgvInventory, _player.MainInventory, 50);
    }

    private void OnInventoryClearItem(object? sender, EventArgs e)
    {
        if (_player == null) return;
        var row = dgvInventory.CurrentRow;
        if (row == null || row.Index >= _player.MainInventory.Count) return;
        _player.MainInventory[row.Index] = new ItemData();
        PopulateItemGrid(dgvInventory, _player.MainInventory, 50);
    }

    private void OnInventorySelectionChanged(object? sender, EventArgs e)
    {
        if (_player == null) return;
        var row = dgvInventory.CurrentRow;
        if (row == null || row.Index >= _player.MainInventory.Count) return;
        var item = _player.MainInventory[row.Index];
        cmbItemSearch.Text = item.ItemName;
        nudStack.Value = item.StackSize;
        cmbPrefix.SelectedIndex = item.Prefix < cmbPrefix.Items.Count ? item.Prefix : 0;
        chkFavorite.Checked = item.Favorited;
    }

    private void OnStorageTabChanged(object? sender, EventArgs e) => _activeStorageGrid = tabStorageSub.SelectedIndex switch { 0 => dgvPiggyBank, 1 => dgvSafe, 2 => dgvDefenderForge, _ => dgvVoidVault };

    private void OnStorageSetItem(object? sender, EventArgs e)
    {
        var grid = _activeStorageGrid;
        if (_player == null || grid == null) return;
        var row = grid.CurrentRow;
        if (row == null) return;

        var items = GetStorageList(grid);
        if (row.Index >= items.Count) return;

        var sel = cmbStorageItemSearch.SelectedItem as ItemDatabase.ItemLookup?;
        int id = sel?.Id ?? 0;
        if (sel == null) id = ItemDatabase.FindIdByPartialName(cmbStorageItemSearch.Text);

        var item = items[row.Index];
        item.ItemId = id;
        item.StackSize = (int)nudStorageStack.Value;
        item.Prefix = (byte)cmbStoragePrefix.SelectedIndex;
        PopulateItemGrid(grid, items, items.Count);
    }

    private void OnStorageClearItem(object? sender, EventArgs e)
    {
        var grid = _activeStorageGrid;
        if (_player == null || grid == null) return;
        var row = grid.CurrentRow;
        if (row == null) return;
        var items = GetStorageList(grid);
        if (row.Index >= items.Count) return;
        items[row.Index] = new ItemData();
        PopulateItemGrid(grid, items, items.Count);
    }

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

    private static DataGridView CreateItemGrid()
    {
        var dgv = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        dgv.Columns.Add("Name", "Name");
        dgv.Columns.Add("ID", "ID");
        dgv.Columns.Add("Stack", "Stack");
        dgv.Columns.Add("Prefix", "Prefix");
        return dgv;
    }

    private static void PopulateItemGrid(DataGridView dgv, List<ItemData> items, int count)
    {
        dgv.Rows.Clear();
        for (int i = 0; i < count; i++)
        {
            var item = i < items.Count ? items[i] : new ItemData();
            dgv.Rows.Add(item.ItemName, item.ItemId, item.StackSize, item.PrefixName);
            if (item.IsEmpty) dgv.Rows[i].DefaultCellStyle.BackColor = Color.LightGray;
        }
    }

    private static void CollectFromGrid(DataGridView dgv, List<ItemData> items)
    {
        items.Clear();
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (row.IsNewRow) continue;
            items.Add(new ItemData { ItemId = GetCellInt(row, 1), StackSize = GetCellInt(row, 2), Prefix = (byte)GetCellInt(row, 3), Favorited = false });
        }
    }

    private static void PopulateEquipmentCombos(ComboBox[] combos, List<ItemData> items, int count, int offset = 0)
    {
        for (int i = 0; i < count && i + offset < combos.Length; i++)
        {
            combos[i + offset].Text = i < items.Count ? items[i].ItemName : "(empty)";
        }
    }

    private static void PopulatePrefixCombos(ComboBox[] combos, List<ItemData> items, int count)
    {
        for (int i = 0; i < count && i < combos.Length; i++)
        {
            combos[i].Text = i < items.Count ? items[i].PrefixName : "(none)";
        }
    }

    private static void CollectEquipmentFromCombos(ComboBox[] itemCombos, ComboBox[]? prefixCombos, List<ItemData> items)
    {
        items.Clear();
        for (int i = 0; i < itemCombos.Length; i++)
        {
            int id = ItemDatabase.FindIdByPartialName(itemCombos[i].Text);
            byte prefix = prefixCombos != null && i < prefixCombos.Length ? PrefixData.GetId(prefixCombos[i].Text) : (byte)0;
            items.Add(new ItemData { ItemId = Math.Max(0, id), Prefix = prefix, StackSize = 1 });
        }
    }

    private static void CollectDyesFromCombos(ComboBox[] combos, List<ItemData> dyes, int offset, int count)
    {
        while (dyes.Count < offset + count) dyes.Add(new ItemData());
        for (int i = 0; i < count && i < combos.Length; i++)
        {
            int id = ItemDatabase.FindIdByPartialName(combos[i].Text);
            dyes[offset + i] = new ItemData { ItemId = Math.Max(0, id), StackSize = 1 };
        }
    }

    private List<ItemData> GetStorageList(DataGridView grid)
    {
        if (_player == null) return [];
        if (grid == dgvPiggyBank) return _player.PiggyBank;
        if (grid == dgvSafe) return _player.Safe;
        if (grid == dgvDefenderForge) return _player.DefenderForge;
        if (grid == dgvVoidVault) return _player.VoidVault;
        return [];
    }

    private void PopulateLoadoutTab(ComboBox[] armorCombos, PlayerLoadout loadout)
    {
        // Simplified: just populate armor for now
        for (int i = 0; i < 3 && i < armorCombos.Length; i++)
            armorCombos[i].Text = i < loadout.Armor.Count ? loadout.Armor[i].ItemName : "(empty)";
    }

    private void PopulateAllItemCombos()
    {
        var allItems = ItemDatabase.GetAllItems();
        var itemStrs = allItems.Select(it => it.ToString()).ToArray();

        void FillCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.AddRange(itemStrs);
        }

        FillCombo(cmbItemSearch);
        FillCombo(cmbStorageItemSearch);

        // Prefix combo
        cmbPrefix.Items.Clear();
        cmbStoragePrefix.Items.Clear();
        foreach (var kv in PrefixData.All)
        {
            cmbPrefix.Items.Add(kv.Value);
            cmbStoragePrefix.Items.Add(kv.Value);
        }

        // Equipment combos
        foreach (var cb in cmbArmorSlots) FillCombo(cb);
        foreach (var cb in cmbVanityArmorSlots) FillCombo(cb);
        foreach (var cb in cmbAccessorySlots) FillCombo(cb);
        foreach (var cb in cmbVanityAccessorySlots) FillCombo(cb);
        foreach (var cb in cmbMiscEquipSlots) FillCombo(cb);
        foreach (var cb in cmbArmorPrefixes) { cb.Items.Clear(); foreach (var kv in PrefixData.All) cb.Items.Add(kv.Value); }
        foreach (var cb in cmbVanityArmorPrefixes) { cb.Items.Clear(); foreach (var kv in PrefixData.All) cb.Items.Add(kv.Value); }
        foreach (var cb in cmbAccessoryPrefixes) { cb.Items.Clear(); foreach (var kv in PrefixData.All) cb.Items.Add(kv.Value); }
        foreach (var cb in cmbVanityAccessoryPrefixes) { cb.Items.Clear(); foreach (var kv in PrefixData.All) cb.Items.Add(kv.Value); }
        foreach (var cb in cmbArmorDyeSlots) FillCombo(cb);
        foreach (var cb in cmbAccessoryDyeSlots) FillCombo(cb);
        foreach (var cb in cmbMiscEquipDyeSlots) FillCombo(cb);
    }

    private static void BuildEquipmentGroup(GroupBox grp, ComboBox[] itemCombos, ComboBox[] prefixCombos, int count, string[]? labels = null)
    {
        int y = 25;
        for (int i = 0; i < count; i++)
        {
            var lbl = new Label { Text = labels != null && i < labels.Length ? labels[i] + ":" : $"Slot {i + 1}:", Location = new Point(10, y + 3), Width = 100, TextAlign = ContentAlignment.MiddleRight };
            itemCombos[i] = new ComboBox { Location = new Point(115, y), Width = 320, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            prefixCombos[i] = new ComboBox { Location = new Point(440, y), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            grp.Controls.AddRange([lbl, itemCombos[i], prefixCombos[i]]);
            y += 32;
        }
        grp.Height = y + 10;
    }

    private static void BuildSimpleEquipmentGroup(GroupBox grp, ComboBox[] combos, int count, string[]? labels = null)
    {
        int y = 25;
        for (int i = 0; i < count; i++)
        {
            var lbl = new Label { Text = labels != null && i < labels.Length ? labels[i] + ":" : $"Slot {i + 1}:", Location = new Point(10, y + 3), Width = 100, TextAlign = ContentAlignment.MiddleRight };
            combos[i] = new ComboBox { Location = new Point(115, y), Width = 400, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            grp.Controls.AddRange([lbl, combos[i]]);
            y += 32;
        }
        grp.Height = y + 10;
    }

    private static void AddRow(TableLayoutPanel layout, int row, Control label, Control ctrl)
    {
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(ctrl, 1, row);
    }

    private static int GetCellInt(DataGridViewRow row, int col) =>
        row.Cells[col].Value is int i ? i : int.TryParse(row.Cells[col].Value?.ToString(), out var v) ? v : 0;

    private static int GetCellInt(DataGridView dgv, int row, int col) =>
        dgv.Rows[row].Cells[col].Value is int i ? i : int.TryParse(dgv.Rows[row].Cells[col].Value?.ToString(), out var v) ? v : 0;

    private static string GetCellStr(DataGridViewRow row, int col) =>
        row.Cells[col].Value?.ToString() ?? "";

    /// <summary>Clamp a value to a NumericUpDown's range.</summary>
    private static decimal ClampNud(NumericUpDown nud, decimal value) =>
        Math.Clamp(value, nud.Minimum, nud.Maximum);

    private static decimal ClampNud(NumericUpDown nud, int value) =>
        ClampNud(nud, (decimal)value);

    #endregion
}
