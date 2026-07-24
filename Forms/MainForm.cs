using Terraria_Players_Editor.Controls;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor;

public partial class MainForm : Form
{
    private PlayerData? _player;
    private string? _filePath;

    private DataGridView? _activeStorageGrid;
    private ToolStripMenuItem? _langEnItem, _langZhItem, _animIconItem;

    // Localized label arrays — used during tab construction
    private static string[] DifficultyNames() => [AppLocale.Get("Diff.Softcore"), AppLocale.Get("Diff.Mediumcore"), AppLocale.Get("Diff.Hardcore"), AppLocale.Get("Diff.Journey")];
    private static string[] LoadoutNames() => [AppLocale.Get("Loadout.Select1"), AppLocale.Get("Loadout.Select2"), AppLocale.Get("Loadout.Select3")];
    private static string[] GenderNames() => [AppLocale.Get("Gender.Female"), AppLocale.Get("Gender.Male")];
    private static string[] HideVisualNames() => [AppLocale.Get("Appearance.Head"), AppLocale.Get("Appearance.Body"), AppLocale.Get("Appearance.Legs"), AppLocale.Get("Appearance.VanityHead"), AppLocale.Get("Appearance.VanityBody"), AppLocale.Get("Appearance.VanityLegs"), AppLocale.Get("Appearance.Acc1"), AppLocale.Get("Appearance.Acc2"), AppLocale.Get("Appearance.Acc3"), AppLocale.Get("Appearance.Acc4")];
    private static string[] HideMiscNames() => [AppLocale.Get("Appearance.Pet"), AppLocale.Get("Appearance.LightPet"), AppLocale.Get("Appearance.Minecart"), AppLocale.Get("Appearance.Mount"), AppLocale.Get("Appearance.Hook")];
    private static string[] HideInfoNames() => [AppLocale.Get("Info.Watch"), AppLocale.Get("Info.Weather"), AppLocale.Get("Info.Depth"), AppLocale.Get("Info.Compass"), AppLocale.Get("Info.Sextant"), AppLocale.Get("Info.Tally"), AppLocale.Get("Info.Stopwatch"), AppLocale.Get("Info.MetalDetector"), AppLocale.Get("Info.DPS"), AppLocale.Get("Info.RareCreature"), AppLocale.Get("Info.FishingPower"), AppLocale.Get("Info.MoonPhase"), AppLocale.Get("Info.Speed")];
    private static string[] ColorNames() => [AppLocale.Get("Color.Hair"), AppLocale.Get("Color.Skin"), AppLocale.Get("Color.Eyes"), AppLocale.Get("Color.Shirt"), AppLocale.Get("Color.UnderShirt"), AppLocale.Get("Color.Pants"), AppLocale.Get("Color.Shoes")];
    private static string[] MiscEquipNames() => [AppLocale.Get("Equip.Pet"), AppLocale.Get("Equip.LightPet"), AppLocale.Get("Equip.Minecart"), AppLocale.Get("Equip.Mount"), AppLocale.Get("Equip.Hook")];
    private static string[] EquipArmorLabels() => [AppLocale.Get("Equip.Helmet"), AppLocale.Get("Equip.Chestplate"), AppLocale.Get("Equip.Leggings")];
    private static string[] EquipVanityArmorLabels() => [AppLocale.Get("Equip.VanityHelmet"), AppLocale.Get("Equip.VanityChest"), AppLocale.Get("Equip.VanityLegs")];
    private static string[] DyeArmorLabels() => [AppLocale.Get("Dyes.HelmetDye"), AppLocale.Get("Dyes.ChestDye"), AppLocale.Get("Dyes.LegsDye")];
    private static string[] DyeMiscLabels() => [AppLocale.Get("Dyes.PetDye"), AppLocale.Get("Dyes.LightPetDye"), AppLocale.Get("Dyes.MinecartDye"), AppLocale.Get("Dyes.MountDye"), AppLocale.Get("Dyes.HookDye")];

    // Locale key arrays for language-refreshable labels
    private static readonly string[] _keysEquipArmor = ["Equip.Helmet", "Equip.Chestplate", "Equip.Leggings"];
    private static readonly string[] _keysEquipVanityArmor = ["Equip.VanityHelmet", "Equip.VanityChest", "Equip.VanityLegs"];
    private static readonly string[] _keysEquipAccessory = ["Slot.Accessory", "Slot.Accessory", "Slot.Accessory", "Slot.Accessory", "Slot.Accessory", "Slot.Accessory", "Slot.Accessory"];
    private static readonly string[] _keysEquipVanityAcc = ["Slot.VanityAcc", "Slot.VanityAcc", "Slot.VanityAcc", "Slot.VanityAcc", "Slot.VanityAcc", "Slot.VanityAcc", "Slot.VanityAcc"];
    private static readonly string[] _keysEquipMisc = ["Equip.Pet", "Equip.LightPet", "Equip.Minecart", "Equip.Mount", "Equip.Hook"];
    private static readonly string[] _keysDyeArmor = ["Dyes.HelmetDye", "Dyes.ChestDye", "Dyes.LegsDye"];
    private static readonly string[] _keysDyeAccessory = ["Slot.AccDye", "Slot.AccDye", "Slot.AccDye", "Slot.AccDye", "Slot.AccDye", "Slot.AccDye", "Slot.AccDye"];
    private static readonly string[] _keysDyeMisc = ["Dyes.PetDye", "Dyes.LightPetDye", "Dyes.MinecartDye", "Dyes.MountDye", "Dyes.HookDye"];

    /// <summary>Refresh label array text from locale keys (with 1-based index for format args).</summary>
    private static void RefreshLabels(Label[] labels, string[] keys)
    {
        for (int i = 0; i < labels.Length && i < keys.Length; i++)
        {
            labels[i].Text = string.Format(AppLocale.Get(keys[i]), i + 1);
        }
    }

    /// <summary>Refresh a single loadout tab's group boxes and labels.</summary>
    private static void RefreshLoadoutGroupBoxes(GroupBox[]? boxes, Label[][]? allLabels, string loadoutKey)
    {
        if (boxes == null || allLabels == null) return;
        string loadoutName = AppLocale.Get(loadoutKey);

        string[] boxSuffixes = ["Loadouts.Armor3", "Loadouts.VanityArmor3", "Loadouts.Accessories7", "Loadouts.VanityAccessories7", "Loadouts.Equipment5"];
        string[][] labelKeys = [_keysEquipArmor, _keysEquipVanityArmor, _keysEquipAccessory, _keysEquipVanityAcc, _keysEquipMisc];

        for (int s = 0; s < boxes.Length && s < boxSuffixes.Length && s < allLabels.Length; s++)
        {
            boxes[s].Text = $"{loadoutName} — {AppLocale.Get(boxSuffixes[s])}";
            RefreshLabels(allLabels[s], labelKeys[s]);
        }
    }

    // Temporary color storage during editing
    private byte[][] _tempColors = Array.Empty<byte[]>();

    public MainForm()
    {
        InitializeComponent();
        BuildForm();
        AppLocale.LanguageChanged += RefreshAllUI;
        RefreshAllUI(); // Apply current language to all UI elements on startup
    }

    #region Form Construction

    private void BuildForm()
    {
        Text = "Terraria Players Editor";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        MinimumSize = new Size(800, 500);

        BuildMenu();
        BuildStatusBar();
        BuildTabControl();

        // Set SplitterDistance after form is shown (controls have proper sizes)
        Shown += (s, e) =>
        {
            _splitInventory.SplitterDistance = 300;
            _splitEquip.SplitterDistance = 280;
            _splitStorage.SplitterDistance = 280;
            _splitBuffs.SplitterDistance = 280;
        };
    }

    private void BuildMenu()
    {
        menuStrip = new MenuStrip();
        fileMenu = new ToolStripMenuItem(AppLocale.Get("Menu.File"));
        openMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Open"), null, OnOpen) { ShortcutKeys = Keys.Control | Keys.O };
        saveMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Save"), null, OnSave) { ShortcutKeys = Keys.Control | Keys.S };
        saveAsMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.SaveAs"), null, OnSaveAs) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S };
        exitMenuItem = new ToolStripMenuItem(AppLocale.Get("Menu.Exit"), null, (_, _) => Close());

        var langMenu = new ToolStripMenuItem(AppLocale.Get("Menu.Language"));
        _langEnItem = new ToolStripMenuItem(AppLocale.Get("Menu.LangEN"), null, (_, _) => AppLocale.SetLanguage(AppLocale.Lang.EN));
        _langZhItem = new ToolStripMenuItem(AppLocale.Get("Menu.LangZH"), null, (_, _) => AppLocale.SetLanguage(AppLocale.Lang.ZH));
        langMenu.DropDownItems.AddRange([_langEnItem, _langZhItem]);

        var settingsMenu = new ToolStripMenuItem(AppLocale.Get("Menu.Settings"));
        settingsMenu.DropDownItems.Add(langMenu);

        _animIconItem = new ToolStripMenuItem("动态图标渲染")
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

        var debugItem = new ToolStripMenuItem("Debug Log");
        debugItem.Click += (_, _) =>
        {
            DebugLog.Enabled = !DebugLog.Enabled;
            debugItem.Checked = DebugLog.Enabled;
            statusLabel.Text = DebugLog.Enabled ? "Debug: ON (see debug_plr.log)" : "Debug: OFF";
        };
        settingsMenu.DropDownItems.Add(debugItem);

        fileMenu.DropDownItems.AddRange([openMenuItem, saveMenuItem, new ToolStripSeparator(), saveAsMenuItem, new ToolStripSeparator(), exitMenuItem]);
        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(settingsMenu);
        Controls.Add(menuStrip);
    }

    private void BuildStatusBar()
    {
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel(AppLocale.Get("Status.Ready"));
        statusProgress = new ToolStripProgressBar { Visible = false, Width = 120 };
        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(statusProgress);
        Controls.Add(statusStrip);
    }

    private void BuildTabControl()
    {
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        tabControl.TabPages.AddRange([
            BuildPlayerInfoTab(),
            BuildStatsTab(),
            BuildAppearanceTab(),
            BuildInventoryTab(),
            BuildEquipmentTab(), // Unified: Equipment + Dyes + Loadouts
            BuildStorageTab(),
            BuildBuffsTab(),
            BuildUpgradesTab(),
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

        lblPlayerName = new Label { Text = AppLocale.Get("Info.Name"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayerName = new TextBox { Dock = DockStyle.Left, Width = 300 };
        lblDifficulty = new Label { Text = AppLocale.Get("Info.Difficulty"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbDifficulty = new ComboBox { Dock = DockStyle.Left, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDifficulty.Items.AddRange(DifficultyNames());
        lblPlayTime = new Label { Text = AppLocale.Get("Info.PlayTime"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtPlayTime = new TextBox { Dock = DockStyle.Left, Width = 150, ReadOnly = true };
        lblFileVersion = new Label { Text = AppLocale.Get("Info.FileVersion"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        txtFileVersion = new TextBox { Dock = DockStyle.Left, Width = 100, ReadOnly = true };
        lblLoadout = new Label { Text = AppLocale.Get("Info.Loadout"), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        cmbCurrentLoadout = new ComboBox { Dock = DockStyle.Left, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCurrentLoadout.Items.AddRange(LoadoutNames());

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

        grpHealth = new GroupBox { Text = AppLocale.Get("Stats.Health"), Width = 350, Height = 100 };
        lblHealth = new Label { Text = AppLocale.Get("Stats.Current"), Location = new Point(15, 30), Width = 70 };
        nudHealth = new NumericUpDown { Location = new Point(90, 28), Width = 100, Minimum = 0, Maximum = 600 };
        lblMaxHealth = new Label { Text = AppLocale.Get("Stats.Max"), Location = new Point(210, 30), Width = 40 };
        nudMaxHealth = new NumericUpDown { Location = new Point(250, 28), Width = 80, Minimum = 100, Maximum = 600, Increment = 20 };
        grpHealth.Controls.AddRange([lblHealth, nudHealth, lblMaxHealth, nudMaxHealth]);

        grpMana = new GroupBox { Text = AppLocale.Get("Stats.Mana"), Width = 350, Height = 100 };
        lblMana = new Label { Text = AppLocale.Get("Stats.Current"), Location = new Point(15, 30), Width = 70 };
        nudMana = new NumericUpDown { Location = new Point(90, 28), Width = 100, Minimum = 0, Maximum = 400 };
        lblMaxMana = new Label { Text = AppLocale.Get("Stats.Max"), Location = new Point(210, 30), Width = 40 };
        nudMaxMana = new NumericUpDown { Location = new Point(250, 28), Width = 80, Minimum = 0, Maximum = 400, Increment = 20 };
        grpMana.Controls.AddRange([lblMana, nudMana, lblMaxMana, nudMaxMana]);

        grpCounters = new GroupBox { Text = AppLocale.Get("Stats.Counters"), Width = 350, Height = 210 };
        lblDeathsPvE = new Label { Text = AppLocale.Get("Stats.DeathsPvE"), Location = new Point(15, 30), Width = 100 };
        nudDeathsPvE = new NumericUpDown { Location = new Point(120, 28), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblDeathsPvP = new Label { Text = AppLocale.Get("Stats.DeathsPvP"), Location = new Point(15, 60), Width = 100 };
        nudDeathsPvP = new NumericUpDown { Location = new Point(120, 58), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblTaxMoney = new Label { Text = AppLocale.Get("Stats.TaxMoney"), Location = new Point(15, 90), Width = 100 };
        nudTaxMoney = new NumericUpDown { Location = new Point(120, 88), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblAnglerQuests = new Label { Text = AppLocale.Get("Stats.AnglerQuests"), Location = new Point(15, 120), Width = 100 };
        nudAnglerQuests = new NumericUpDown { Location = new Point(120, 118), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        lblGolferScore = new Label { Text = AppLocale.Get("Stats.GolferScore"), Location = new Point(15, 150), Width = 100 };
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
        lblHairStyle = new Label { Text = AppLocale.Get("Appearance.HairStyle"), Width = 70, TextAlign = ContentAlignment.MiddleRight };
        nudHairStyle = new NumericUpDown { Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblHairDye = new Label { Text = AppLocale.Get("Appearance.HairDye"), Width = 70, TextAlign = ContentAlignment.MiddleRight };
        nudHairDye = new NumericUpDown { Width = 70, Minimum = 0, Maximum = int.MaxValue };
        lblSkinVariant = new Label { Text = AppLocale.Get("Appearance.Skin"), Width = 50, TextAlign = ContentAlignment.MiddleRight };
        cmbSkinVariant = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSkinVariant.Items.AddRange(GenderNames());
        topRow.Controls.AddRange([lblHairStyle, nudHairStyle, lblHairDye, nudHairDye, lblSkinVariant, cmbSkinVariant]);

        // Color pickers
        grpColors = new GroupBox { Text = AppLocale.Get("Appearance.Colors"), Width = 800, Height = 160 };
        colorButtons = new Button[7];
        colorPanels = new Panel[7];
        lblColors = new Label[7];
        _tempColors = new byte[7][];
        for (int i = 0; i < 7; i++)
        {
            _tempColors[i] = new byte[3];
            int x = 15 + (i % 4) * 190;
            int y = 25 + (i / 4) * 60;
            lblColors[i] = new Label { Text = ColorNames()[i] + ":", Location = new Point(x, y), Width = 40, TextAlign = ContentAlignment.MiddleRight };
            colorPanels[i] = new Panel { Location = new Point(x + 45, y), Width = 40, Height = 24, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            colorButtons[i] = new Button { Text = AppLocale.Get("Appearance.Pick"), Location = new Point(x + 90, y - 1), Width = 55, Height = 26 };
            int idx = i;
            colorButtons[i].Click += (_, _) => PickColor(idx);
            grpColors.Controls.AddRange([lblColors[i], colorPanels[i], colorButtons[i]]);
        }

        // Visibility toggles
        grpVisibility = new GroupBox { Text = AppLocale.Get("Appearance.Visibility"), Width = 800, Height = 100 };
        chkHideVisual = new CheckBox[10];
        chkHideMisc = new CheckBox[5];
        for (int i = 0; i < 10; i++)
        {
            chkHideVisual[i] = new CheckBox { Text = i < HideVisualNames().Length ? HideVisualNames()[i] : $"Visual{i}", Location = new Point(15 + (i % 5) * 155, 25 + (i / 5) * 28), Width = 150 };
            grpVisibility.Controls.Add(chkHideVisual[i]);
        }

        mainPanel.Controls.AddRange([topRow, grpColors, grpVisibility]);
        tabAppearance.Controls.Add(mainPanel);
        return tabAppearance;
    }

    private TabPage BuildInventoryTab()
    {
        tabInventory = new TabPage("Inventory");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };

        // Store split container for deferred SplitterDistance setting
        _splitInventory = split;

        // === LEFT: Item Browser ===
        _browserInventory = new ItemBrowser();
        split.Panel1.Controls.Add(_browserInventory);

        // === RIGHT: Modifier + Grids ===
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(5) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _modInventory = new ItemModifier { Dock = DockStyle.Top, ShowStack = true, ShowPrefix = true, ShowFavorite = true };
        right.Controls.Add(_modInventory, 0, 0);

        var gridPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(0) };
        gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        _gridInventory = new SlotGrid(10, 5, enableHotbarColor: true);
        gridPanel.Controls.Add(_gridInventory, 0, 0);

        var coinAmmoPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _gridCoins = new SlotGrid(4, 1);
        var grpCoinsNew = new GroupBox { Text = AppLocale.Get("Inventory.Coins"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpCoinsNew.Controls.Add(_gridCoins);
        _gridAmmo = new SlotGrid(4, 1);
        var grpAmmoNew = new GroupBox { Text = AppLocale.Get("Inventory.Ammo"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpAmmoNew.Controls.Add(_gridAmmo);
        coinAmmoPanel.Controls.AddRange([grpCoinsNew, grpAmmoNew]);
        gridPanel.Controls.Add(coinAmmoPanel, 0, 2);

        right.Controls.Add(gridPanel, 0, 1);
        split.Panel2.Controls.Add(right);

        // Events
        _gridInventory.SlotSelected += (s, idx) => OnInvGridSlotSelected(idx);
        _gridCoins.SlotSelected += (s, idx) => { _activeSlotGrid = "coins"; _modInventory.LoadFromSlot(idx, _player?.Coins.Count > idx ? _player.Coins[idx] : new ItemData()); };
        _gridAmmo.SlotSelected += (s, idx) => { _activeSlotGrid = "ammo"; _modInventory.LoadFromSlot(idx, _player?.Ammo.Count > idx ? _player.Ammo[idx] : new ItemData()); };
        _browserInventory.ItemSelected += (s, itemId) => OnBrowserItemSelected(itemId);
        _modInventory.SetClicked += (s, idx) => OnInvModSet(idx);
        _modInventory.ClearClicked += (s, idx) => OnInvModClear(idx);

        tabInventory.Controls.Add(split);
        return tabInventory;
    }

    private TabPage BuildEquipmentTab()
    {
        tabEquipment = new TabPage("Equipment");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };

        _splitEquip = split;

        // === LEFT: Item Browser (dynamically switches between all/dye-only) ===
        _browserEquip = new ItemBrowser();
        split.Panel1.Controls.Add(_browserEquip);

        // === RIGHT: Loadout selector + Modifier + Equipment grid ===
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(5) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // Loadout selector
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));  // Modifier
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Grids

        // Loadout selector
        _loadoutSelector = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _rbLoadout1 = new RadioButton { Text = AppLocale.Get("Loadout.Select1"), Width = 90, Checked = true };
        _rbLoadout2 = new RadioButton { Text = AppLocale.Get("Loadout.Select2"), Width = 90 };
        _rbLoadout3 = new RadioButton { Text = AppLocale.Get("Loadout.Select3"), Width = 90 };
        _rbLoadout1.CheckedChanged += (s, e) => { if (_rbLoadout1.Checked) OnLoadoutSwitch(0); };
        _rbLoadout2.CheckedChanged += (s, e) => { if (_rbLoadout2.Checked) OnLoadoutSwitch(1); };
        _rbLoadout3.CheckedChanged += (s, e) => { if (_rbLoadout3.Checked) OnLoadoutSwitch(2); };
        _loadoutSelector.Controls.AddRange([_rbLoadout1, _rbLoadout2, _rbLoadout3]);
        right.Controls.Add(_loadoutSelector, 0, 0);

        // Modifier
        _modEquip = new ItemModifier { Dock = DockStyle.Top, ShowStack = false, ShowPrefix = true, ShowFavorite = false };
        right.Controls.Add(_modEquip, 0, 1);

        // Equipment grid area (scrollable) — 3-column layout: Dyes | Vanity | Equipment
        var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var equipPanel = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Padding = new Padding(5) };
        equipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        equipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        equipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // --- Row 0: Armor (3)  ---
        _armorDyeSlots = new SlotGrid[1];
        _armorDyeSlots[0] = new SlotGrid(3, 1);
        _armorDyeSlots[0].Tag = "DyeArmor";
        var grpArmorDyes = new GroupBox { Text = AppLocale.Get("Dyes.Armor"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpArmorDyes.Controls.Add(_armorDyeSlots[0]);
        equipPanel.Controls.Add(grpArmorDyes, 0, 0);

        _vanitySlots = new SlotGrid[1];
        _vanitySlots[0] = new SlotGrid(3, 1);
        _vanitySlots[0].Tag = "EquipVanity";
        var grpVanityArmor = new GroupBox { Text = AppLocale.Get("Equip.VanityArmor"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpVanityArmor.Controls.Add(_vanitySlots[0]);
        equipPanel.Controls.Add(grpVanityArmor, 1, 0);

        _equipSlots = new SlotGrid[1];
        _equipSlots[0] = new SlotGrid(3, 1);
        _equipSlots[0].Tag = "EquipArmor";
        var grpEquipArmor = new GroupBox { Text = AppLocale.Get("Equip.Armor"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpEquipArmor.Controls.Add(_equipSlots[0]);
        equipPanel.Controls.Add(grpEquipArmor, 2, 0);

        // --- Row 1: Accessories (7) ---
        _accDyeSlots = new SlotGrid[1];
        _accDyeSlots[0] = new SlotGrid(7, 1);
        _accDyeSlots[0].Tag = "DyeAcc";
        var grpAccDyes = new GroupBox { Text = AppLocale.Get("Dyes.Accessories"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpAccDyes.Controls.Add(_accDyeSlots[0]);
        equipPanel.Controls.Add(grpAccDyes, 0, 1);

        _vaccSlots = new SlotGrid[1];
        _vaccSlots[0] = new SlotGrid(7, 1);
        _vaccSlots[0].Tag = "EquipVAcc";
        var grpVAcc = new GroupBox { Text = AppLocale.Get("Equip.VanityAccessories"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpVAcc.Controls.Add(_vaccSlots[0]);
        equipPanel.Controls.Add(grpVAcc, 1, 1);

        _accSlots = new SlotGrid[1];
        _accSlots[0] = new SlotGrid(7, 1);
        _accSlots[0].Tag = "EquipAcc";
        var grpAcc = new GroupBox { Text = AppLocale.Get("Equip.Accessories"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpAcc.Controls.Add(_accSlots[0]);
        equipPanel.Controls.Add(grpAcc, 2, 1);

        // --- Row 2: Equipment/Misc (5) ---
        _miscDyeSlots = new SlotGrid[1];
        _miscDyeSlots[0] = new SlotGrid(5, 1);
        _miscDyeSlots[0].Tag = "DyeMisc";
        var grpMiscDyes = new GroupBox { Text = AppLocale.Get("Dyes.Equipment"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpMiscDyes.Controls.Add(_miscDyeSlots[0]);
        equipPanel.Controls.Add(grpMiscDyes, 0, 2);

        _miscSlots = new SlotGrid[1];
        _miscSlots[0] = new SlotGrid(5, 1);
        _miscSlots[0].Tag = "EquipMisc";
        var grpMisc = new GroupBox { Text = AppLocale.Get("Equip.Misc"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        grpMisc.Controls.Add(_miscSlots[0]);
        equipPanel.Controls.Add(grpMisc, 2, 2);

        scrollPanel.Controls.Add(equipPanel);
        right.Controls.Add(scrollPanel, 0, 2);
        split.Panel2.Controls.Add(right);

        // Events
        _modEquip.SetClicked += (s, idx) => OnEquipModSet(idx);
        _modEquip.ClearClicked += (s, idx) => OnEquipModClear(idx);
        _browserEquip.ItemSelected += (s, itemId) => { if (_activeEquipGrid != null) OnEquipBrowserSelect(itemId); };

        // Wire all equip slot grids to the same handler
        foreach (var grid in _equipSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _vanitySlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _accSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _vaccSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _miscSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _armorDyeSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _accDyeSlots) grid.SlotSelected += OnEquipSlotSelected;
        foreach (var grid in _miscDyeSlots) grid.SlotSelected += OnEquipSlotSelected;

        tabEquipment.Controls.Add(split);
        return tabEquipment;
    }

    /// <summary>Helper to create a GroupBox with a 1-row SlotGrid inside.</summary>
    private TabPage BuildStorageTab()
    {
        tabStorage = new TabPage("Storage");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1
        };

        _splitStorage = split;

        _browserStorage = new ItemBrowser();
        split.Panel1.Controls.Add(_browserStorage);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(5) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _modStorage = new ItemModifier { Dock = DockStyle.Top, ShowStack = true, ShowPrefix = true, ShowFavorite = false };
        right.Controls.Add(_modStorage, 0, 0);

        tabStorageSub = new TabControl { Dock = DockStyle.Fill };
        subPiggyBank = new TabPage(AppLocale.Get("Storage.PiggyBank"));
        subSafe = new TabPage(AppLocale.Get("Storage.Safe"));
        subDefenderForge = new TabPage(AppLocale.Get("Storage.DefenderForge"));
        subVoidVault = new TabPage(AppLocale.Get("Storage.VoidVault"));

        _gridPiggy = new SlotGrid(10, 4); subPiggyBank.Controls.Add(_gridPiggy);
        _gridSafe = new SlotGrid(10, 4); subSafe.Controls.Add(_gridSafe);
        _gridDefender = new SlotGrid(10, 4); subDefenderForge.Controls.Add(_gridDefender);
        _gridVoid = new SlotGrid(10, 4); subVoidVault.Controls.Add(_gridVoid);

        _gridPiggy.SlotSelected += (s, idx) => _activeStorageIdx = idx;
        _gridSafe.SlotSelected += (s, idx) => _activeStorageIdx = idx;
        _gridDefender.SlotSelected += (s, idx) => _activeStorageIdx = idx;
        _gridVoid.SlotSelected += (s, idx) => _activeStorageIdx = idx;

        tabStorageSub.TabPages.AddRange([subPiggyBank, subSafe, subDefenderForge, subVoidVault]);
        tabStorageSub.SelectedIndexChanged += OnStorageTabChanged;
        right.Controls.Add(tabStorageSub, 0, 1);

        _browserStorage.ItemSelected += (s, id) => OnStorageBrowserSelect(id);
        _modStorage.SetClicked += (s, idx) => OnStorageModSet();
        _modStorage.ClearClicked += (s, idx) => OnStorageModClear();

        split.Panel2.Controls.Add(right);
        tabStorage.Controls.Add(split);
        return tabStorage;
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
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblBuffTitle = new Label { Text = AppLocale.Get("Buffs.Title"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        right.Controls.Add(_lblBuffTitle, 0, 0);

        // Buff modifier
        var buffMod = new Panel { Dock = DockStyle.Top, Height = 100, BorderStyle = BorderStyle.FixedSingle };
        _lblBuffType = new Label { Text = AppLocale.Get("Buffs.Type"), Location = new Point(5, 5), Width = 70 };
        _nudBuffType = new NumericUpDown { Location = new Point(80, 3), Width = 80, Minimum = 0, Maximum = 387 };
        _lblBuffDuration = new Label { Text = AppLocale.Get("Buffs.Duration"), Location = new Point(170, 5), Width = 70 };
        _nudBuffDuration = new NumericUpDown { Location = new Point(245, 3), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        _lblBuffTimeUnit = new Label { Text = "ticks", Location = new Point(348, 5), Width = 40, ForeColor = Color.Gray };
        _btnBuffSet = new Button { Text = AppLocale.Get("Storage.Set"), Location = new Point(5, 30), Width = 75 };
        _btnBuffClear = new Button { Text = AppLocale.Get("Storage.Clear"), Location = new Point(85, 30), Width = 75 };
        void BuffAutoSet()
        {
            if (_gridBuffs.SelectedIndex < 0) return;
            _cachedBuffType = (int)_nudBuffType.Value;
            _cachedBuffDur = (int)_nudBuffDuration.Value;
            OnBuffModSet();
        }
        _nudBuffType.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; BuffAutoSet(); } };
        _nudBuffDuration.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; BuffAutoSet(); } };
        _nudBuffType.Leave += (s, e) => BuffAutoSet();
        _nudBuffDuration.Leave += (s, e) => BuffAutoSet();
        _btnBuffSet.Click += (s, e) => BuffAutoSet();
        _btnBuffClear.Click += (s, e) => OnBuffModClear();
        buffMod.Controls.AddRange([_lblBuffType, _nudBuffType, _lblBuffDuration, _nudBuffDuration, _lblBuffTimeUnit, _btnBuffSet, _btnBuffClear]);
        right.Controls.Add(buffMod, 0, 1);

        _gridBuffs = new SlotGrid(11, 4) { IsBuffGrid = true };
        _gridBuffs.SlotSelected += (s, idx) => OnBuffSlotSelected(idx);
        _browserBuffs.ItemSelected += (s, id) => OnBuffBrowserSelect(id);
        right.Controls.Add(_gridBuffs, 0, 2);

        split.Panel2.Controls.Add(right);
        tabBuffs.Controls.Add(split);
        return tabBuffs;
    }

    private TabPage BuildUpgradesTab()
    {
        tabUpgrades = new TabPage("Upgrades");
        var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), AutoScroll = true };

        chkExtraAccessory = new CheckBox { Text = AppLocale.Get("Upgrades.ExtraAccessory"), Width = 400, Margin = new Padding(5) };
        chkAegisCrystal = new CheckBox { Text = AppLocale.Get("Upgrades.AegisCrystal"), Width = 400, Margin = new Padding(5) };
        chkAegisFruit = new CheckBox { Text = AppLocale.Get("Upgrades.AegisFruit"), Width = 400, Margin = new Padding(5) };
        chkArcaneCrystal = new CheckBox { Text = AppLocale.Get("Upgrades.ArcaneCrystal"), Width = 400, Margin = new Padding(5) };
        chkGalaxyPearl = new CheckBox { Text = AppLocale.Get("Upgrades.GalaxyPearl"), Width = 400, Margin = new Padding(5) };
        chkGummyWorm = new CheckBox { Text = AppLocale.Get("Upgrades.GummyWorm"), Width = 400, Margin = new Padding(5) };
        chkAmbrosia = new CheckBox { Text = AppLocale.Get("Upgrades.Ambrosia"), Width = 400, Margin = new Padding(5) };
        chkArtisanBread = new CheckBox { Text = AppLocale.Get("Upgrades.ArtisanBread"), Width = 400, Margin = new Padding(5) };
        chkBiomeTorches = new CheckBox { Text = AppLocale.Get("Upgrades.BiomeTorches"), Width = 400, Margin = new Padding(5) };
        chkUsingBiomeTorches = new CheckBox { Text = AppLocale.Get("Upgrades.UsingBiomeTorches"), Width = 400, Margin = new Padding(5) };

        var cartPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 40, Width = 400, Margin = new Padding(5) };
        lblSuperCart = new Label { Text = AppLocale.Get("Upgrades.SuperCart"), Width = 110, TextAlign = ContentAlignment.MiddleRight };
        nudSuperCart = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 2 };
        chkSuperCartEnabled = new CheckBox { Text = AppLocale.Get("Upgrades.SuperCartEnabled"), Width = 80 };
        cartPanel.Controls.AddRange([lblSuperCart, nudSuperCart, chkSuperCartEnabled]);

        mainPanel.Controls.AddRange([
            chkExtraAccessory, chkAegisCrystal, chkAegisFruit, chkArcaneCrystal,
            chkGalaxyPearl, chkGummyWorm, chkAmbrosia, chkArtisanBread,
            chkBiomeTorches, chkUsingBiomeTorches, cartPanel
        ]);

        tabUpgrades.Controls.Add(mainPanel);
        return tabUpgrades;
    }

    private TabPage BuildSpawnPointsTab()
    {
        tabSpawnPoints = new TabPage("Spawn Points");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 85));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

        dgvSpawnPoints = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
        dgvSpawnPoints.Columns.Add("WorldId", AppLocale.Get("Spawn.WorldId"));
        dgvSpawnPoints.Columns.Add("WorldName", AppLocale.Get("Spawn.WorldName"));
        dgvSpawnPoints.Columns.Add("X", AppLocale.Get("Spawn.X"));
        dgvSpawnPoints.Columns.Add("Y", AppLocale.Get("Spawn.Y"));

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5) };
        btnAddSpawn = new Button { Text = AppLocale.Get("Spawn.Add"), Width = 130 };
        btnRemoveSpawn = new Button { Text = AppLocale.Get("Spawn.Remove"), Width = 130 };
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

        chkHotbarLocked = new CheckBox { Text = AppLocale.Get("Misc.HotbarLocked"), Width = 200, Margin = new Padding(5, 5, 5, 15) };

        grpHideInfo = new GroupBox { Text = AppLocale.Get("Misc.HideInfo"), Width = 700, Height = 160 };
        chkHideInfo = new CheckBox[13];
        for (int i = 0; i < 13; i++)
        {
            chkHideInfo[i] = new CheckBox
            {
                Text = i < HideInfoNames().Length ? HideInfoNames()[i] : $"Info{i}",
                Location = new Point(15 + (i % 4) * 170, 25 + (i / 4) * 30),
                Width = 160
            };
            grpHideInfo.Controls.Add(chkHideInfo[i]);
        }

        grpCooldowns = new GroupBox { Text = AppLocale.Get("Misc.Cooldowns"), Width = 460, Height = 200 };
        lblPotionDelay = new Label { Text = AppLocale.Get("Misc.PotionDelay"), Location = new Point(15, 30), Width = 110 };
        nudPotionDelay = new NumericUpDown { Location = new Point(120, 28), Width = 120, Minimum = 0, Maximum = int.MaxValue };
        lblManaPotionDelay = new Label { Text = AppLocale.Get("Misc.ManaPotionDelay"), Location = new Point(15, 60), Width = 110 };
        nudManaPotionDelay = new NumericUpDown { Location = new Point(120, 58), Width = 120, Minimum = 0, Maximum = int.MaxValue };
        lblRestorationCd = new Label { Text = AppLocale.Get("Misc.RestorationCd"), Location = new Point(15, 90), Width = 110 };
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

    #region Event Handlers

    private void OnInvGridSlotSelected(int idx)
    {
        if (_player == null) return;
        _activeSlotGrid = "main";
        var item = idx < _player.MainInventory.Count ? _player.MainInventory[idx] : new ItemData();
        _modInventory.LoadFromSlot(idx, item);
    }

    private void OnBrowserItemSelected(int itemId)
    {
        var item = new ItemData { ItemId = itemId, StackSize = 1 };
        var (grid, list) = GetActiveInvTarget();
        if (grid != null && list != null && grid.SelectedIndex >= 0)
        {
            var idx = grid.SelectedIndex;
            if (idx < list.Count)
            {
                list[idx] = item;
                grid.SetSlot(idx, item);
                _modInventory.LoadFromSlot(idx, item);
            }
        }
    }

    private void OnInvModSet(int slotIdx)
    {
        if (_player == null || slotIdx < 0) return;
        var item = _modInventory.BuildItemData();
        var (grid, list) = GetActiveInvTarget();
        if (grid != null && list != null && slotIdx < list.Count)
        {
            list[slotIdx] = item;
            grid.SetSlot(slotIdx, item);
        }
    }

    private void OnInvModClear(int slotIdx)
    {
        if (_player == null || slotIdx < 0) return;
        var (grid, list) = GetActiveInvTarget();
        if (grid != null && list != null && slotIdx < list.Count)
        {
            list[slotIdx] = new ItemData();
            grid.SetSlot(slotIdx, new ItemData());
        }
    }

    /// <summary>Get the currently active inventory grid and its corresponding player data list.</summary>
    private (SlotGrid? grid, List<ItemData>? list) GetActiveInvTarget()
    {
        return _activeSlotGrid switch
        {
            "main" => (_gridInventory, _player?.MainInventory),
            "coins" => (_gridCoins, _player?.Coins),
            "ammo" => (_gridAmmo, _player?.Ammo),
            _ => (null, null)
        };
    }

    private void OnLoadoutSwitch(int loadoutIdx)
    {
        if (_player == null) return;
        // Save current loadout edits before switching (skip during initial population)
        if (!_populating) CollectEquipToLoadout();
        _activeLoadout = loadoutIdx;
        switch (loadoutIdx)
        {
            case 0:
                PopulateEquipFromData(_player.Armor, _player.VanityArmor, _player.Accessories,
                    _player.VanityAccessories, _player.MiscEquips, _player.ArmorDyes, _player.MiscEquipDyes);
                break;
            case 1:
                if (_player.Loadout2 != null)
                    PopulateEquipFromData(_player.Loadout2.Armor, _player.Loadout2.VanityArmor,
                        _player.Loadout2.Accessories, _player.Loadout2.VanityAccessories,
                        _player.Loadout2.MiscEquips, _player.Loadout2.ArmorDyes, _player.Loadout2.MiscEquipDyes);
                break;
            case 2:
                if (_player.Loadout3 != null)
                    PopulateEquipFromData(_player.Loadout3.Armor, _player.Loadout3.VanityArmor,
                        _player.Loadout3.Accessories, _player.Loadout3.VanityAccessories,
                        _player.Loadout3.MiscEquips, _player.Loadout3.ArmorDyes, _player.Loadout3.MiscEquipDyes);
                break;
        }
    }

    private void OnEquipSlotSelected(object? sender, int idx)
    {
        if (sender is SlotGrid grid)
        {
            _activeEquipGrid = grid;
            _modEquip.LoadFromSlot(idx, grid.GetItem(idx) ?? new ItemData());
        }
    }

    private void OnEquipBrowserSelect(int itemId)
    {
        if (_activeEquipGrid == null || _activeEquipGrid.SelectedIndex < 0) return;
        var idx = _activeEquipGrid.SelectedIndex;
        var item = new ItemData { ItemId = itemId, StackSize = 1 };
        _activeEquipGrid.SetSlot(idx, item);
        _modEquip.LoadFromSlot(idx, item);
    }

    private void OnEquipModSet(int slotIdx)
    {
        if (_activeEquipGrid == null || slotIdx < 0) return;
        var item = _modEquip.BuildItemData();
        _activeEquipGrid.SetSlot(slotIdx, item);
    }

    private void OnEquipModClear(int slotIdx)
    {
        if (_activeEquipGrid == null || slotIdx < 0) return;
        _activeEquipGrid.SetSlot(slotIdx, new ItemData());
    }

    private void OnStorageBrowserSelect(int itemId)
    {
        var activeGrid = GetActiveStorageGrid();
        if (activeGrid == null || _activeStorageIdx < 0) return;
        var item = new ItemData { ItemId = itemId, StackSize = 1 };
        activeGrid.SetSlot(_activeStorageIdx, item);
        _modStorage.LoadFromSlot(_activeStorageIdx, item);
    }

    private void OnStorageModSet()
    {
        var activeGrid = GetActiveStorageGrid();
        if (activeGrid == null || _activeStorageIdx < 0) return;
        var item = _modStorage.BuildItemData();
        activeGrid.SetSlot(_activeStorageIdx, item);
    }

    private void OnStorageModClear()
    {
        var activeGrid = GetActiveStorageGrid();
        if (activeGrid == null || _activeStorageIdx < 0) return;
        activeGrid.SetSlot(_activeStorageIdx, new ItemData());
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

    private void OnBuffSlotSelected(int idx)
    {
        var item = _gridBuffs.GetItem(idx);
        if (item != null)
        {
            _nudBuffType.Value = item.ItemId;
            _nudBuffDuration.Value = item.StackSize;
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
        _nudBuffType.Value = itemId;
    }

    private void OnBuffModSet()
    {
        if (_gridBuffs.SelectedIndex < 0) return;
        var idx = _gridBuffs.SelectedIndex;
        var type = _cachedBuffType;
        var dur = _cachedBuffDur;
        DebugLog.Log($"[Buff] ModSet: slot={idx}, type={type}, dur={dur}");
        _player!.BuffTypes[idx] = type;
        _player.BuffTimes[idx] = dur;
        _gridBuffs.SetSlot(idx, new ItemData { ItemId = type, StackSize = dur });
    }

    private void OnBuffModClear()
    {
        if (_gridBuffs.SelectedIndex < 0) return;
        var idx = _gridBuffs.SelectedIndex;
        _player!.BuffTypes[idx] = 0;
        _player.BuffTimes[idx] = 0;
        _gridBuffs.SetSlot(idx, new ItemData());
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

        // Tab 1: File version as game version string
        txtFileVersion.Text = VersionMapper.GetDisplayString(_player.FileVersion);

        // Tab 4: Inventory — SlotGrids
        _gridInventory.SetItems(_player.MainInventory);
        _gridCoins.SetItems(_player.Coins);
        _gridAmmo.SetItems(_player.Ammo);

        // Tab 5: Unified Equipment — load Loadout 1
        _activeLoadout = 0;
        _rbLoadout1.Checked = true;
        PopulateEquipFromData(_player.Armor, _player.VanityArmor, _player.Accessories,
            _player.VanityAccessories, _player.MiscEquips, _player.ArmorDyes, _player.MiscEquipDyes);

        // Tab 6: Storage
        _gridPiggy.SetItems(_player.PiggyBank);
        _gridSafe.SetItems(_player.Safe);
        _gridDefender.SetItems(_player.DefenderForge);
        _gridVoid.SetItems(_player.VoidVault);

        // Tab 7: Buffs
        var buffItems = new List<ItemData>(44);
        for (int i = 0; i < 44; i++)
        {
            buffItems.Add(new ItemData { ItemId = _player.BuffTypes[i], StackSize = _player.BuffTimes[i] });
        }
        _gridBuffs.SetItems(buffItems);

        // Tab 8: Upgrades
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

        // Tab 9: Spawn Points
        dgvSpawnPoints.Rows.Clear();
        foreach (var sp in _player.SpawnPoints)
            dgvSpawnPoints.Rows.Add(sp.WorldId, sp.WorldName, sp.X, sp.Y);

        // Tab 10: Misc
        chkHotbarLocked.Checked = _player.HotbarLocked;
        for (int i = 0; i < 13; i++) chkHideInfo[i].Checked = i < _player.HideInfo.Length && _player.HideInfo[i];
        nudPotionDelay.Value = ClampNud(nudPotionDelay, _player.PotionDelay);
        nudManaPotionDelay.Value = ClampNud(nudManaPotionDelay, _player.ManaPotionDelay);
        nudRestorationCd.Value = ClampNud(nudRestorationCd, _player.RestorationPotionCd);

        // Populate item combos in modifiers
        _modInventory.PopulateItems();
        _modInventory.PopulatePrefixes();
        _modEquip.PopulateItems();
        _modEquip.PopulatePrefixes();
        _modStorage.PopulateItems();
        _modStorage.PopulatePrefixes();
        _browserInventory.LoadItems();
        _browserEquip.LoadItems();
        _browserStorage.LoadItems();
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
        List<ItemData> armor, vanity, acc, vacc, misc, armorDyes, miscDyes;
        if (_activeLoadout == 0)
        {
            armor = _player.Armor; vanity = _player.VanityArmor; acc = _player.Accessories;
            vacc = _player.VanityAccessories; misc = _player.MiscEquips;
            armorDyes = _player.ArmorDyes; miscDyes = _player.MiscEquipDyes;
        }
        else if (_activeLoadout == 1 && _player.Loadout2 != null)
        {
            armor = _player.Loadout2.Armor; vanity = _player.Loadout2.VanityArmor; acc = _player.Loadout2.Accessories;
            vacc = _player.Loadout2.VanityAccessories; misc = _player.Loadout2.MiscEquips;
            armorDyes = _player.Loadout2.ArmorDyes; miscDyes = _player.Loadout2.MiscEquipDyes;
        }
        else if (_activeLoadout == 2 && _player.Loadout3 != null)
        {
            armor = _player.Loadout3.Armor; vanity = _player.Loadout3.VanityArmor; acc = _player.Loadout3.Accessories;
            vacc = _player.Loadout3.VanityAccessories; misc = _player.Loadout3.MiscEquips;
            armorDyes = _player.Loadout3.ArmorDyes; miscDyes = _player.Loadout3.MiscEquipDyes;
        }
        else return;

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
        _equipSlots[0].SetItems(armor);
        _vanitySlots[0].SetItems(vanity);
        _accSlots[0].SetItems(acc);
        _vaccSlots[0].SetItems(vacc);
        _miscSlots[0].SetItems(misc);
        // ArmorDyes: first 3 = armor dyes, slots 3-9 = accessory dyes
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

        // Tab 4: Inventory — read from SlotGrids (all three: main, coins, ammo)
        int invNonEmpty = _gridInventory.Slots.Count(s => s.Item != null && s.Item.ItemId > 0);
        DebugLog.Log($"CollectAllTabs: Grid inventory has {invNonEmpty} non-empty slots");
        for (int i = 0; i < _gridInventory.Slots.Length && i < _player.MainInventory.Count; i++)
            _player.MainInventory[i] = _gridInventory.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridCoins.Slots.Length && i < _player.Coins.Count; i++)
            _player.Coins[i] = _gridCoins.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridAmmo.Slots.Length && i < _player.Ammo.Count; i++)
            _player.Ammo[i] = _gridAmmo.Slots[i].Item ?? new ItemData();
        DebugLog.Log($"CollectAllTabs: After collect — MainInventory={_player.MainInventory.Count(x=>x.ItemId>0)} non-empty");

        // Tab 5: Unified Equipment — save to active loadout
        CollectEquipToLoadout();

        // Tab 6: Storage — collect ALL four containers, not just the visible one
        for (int i = 0; i < _gridPiggy.Slots.Length && i < _player.PiggyBank.Count; i++)
            _player.PiggyBank[i] = _gridPiggy.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridSafe.Slots.Length && i < _player.Safe.Count; i++)
            _player.Safe[i] = _gridSafe.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridDefender.Slots.Length && i < _player.DefenderForge.Count; i++)
            _player.DefenderForge[i] = _gridDefender.Slots[i].Item ?? new ItemData();
        for (int i = 0; i < _gridVoid.Slots.Length && i < _player.VoidVault.Count; i++)
            _player.VoidVault[i] = _gridVoid.Slots[i].Item ?? new ItemData();

        // Tab 7: Buffs
        for (int i = 0; i < 44 && i < _gridBuffs.Slots.Length; i++)
        {
            var bItem = _gridBuffs.Slots[i].Item;
            _player.BuffTypes[i] = bItem?.ItemId ?? 0;
            _player.BuffTimes[i] = bItem?.StackSize ?? 0;
        }

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

    #region Event Handlers (Legacy)

    private void OnStorageTabChanged(object? sender, EventArgs e)
    {
        _activeStorageIdx = -1;
        PopulateStorageModifier();
    }

    private void PopulateStorageModifier()
    {
        var grid = GetActiveStorageGrid();
        if (grid != null && _activeStorageIdx >= 0 && _activeStorageIdx < grid.Slots.Length)
        {
            var item = grid.Slots[_activeStorageIdx].Item;
            if (item != null) _modStorage.LoadFromSlot(_activeStorageIdx, item);
            else _modStorage.LoadFromSlot(_activeStorageIdx, new ItemData());
        }
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
        dgv.Columns.Add("Name", AppLocale.Get("Grid.Name"));
        dgv.Columns.Add("ID", AppLocale.Get("Grid.ID"));
        dgv.Columns.Add("Stack", AppLocale.Get("Grid.Stack"));
        dgv.Columns.Add("Prefix", AppLocale.Get("Grid.Prefix"));
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

    private Label[] BuildEquipmentGroup(GroupBox grp, ComboBox[] itemCombos, ComboBox[] prefixCombos, int count, string[]? labels = null)
    {
        var lbls = new Label[count];
        int y = 25;
        for (int i = 0; i < count; i++)
        {
            string labelText = labels != null && i < labels.Length ? labels[i] : string.Format(AppLocale.Get("Slot.Generic"), i + 1);
            lbls[i] = new Label { Text = labelText, Location = new Point(10, y + 3), Width = 100, TextAlign = ContentAlignment.MiddleRight };
            itemCombos[i] = new ComboBox { Location = new Point(115, y), Width = 320, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            prefixCombos[i] = new ComboBox { Location = new Point(440, y), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            grp.Controls.AddRange([lbls[i], itemCombos[i], prefixCombos[i]]);
            y += 32;
        }
        grp.Height = y + 10;
        return lbls;
    }

    private Label[] BuildSimpleEquipmentGroup(GroupBox grp, ComboBox[] combos, int count, string[]? labels = null)
    {
        var lbls = new Label[count];
        int y = 25;
        for (int i = 0; i < count; i++)
        {
            string labelText = labels != null && i < labels.Length ? labels[i] : string.Format(AppLocale.Get("Slot.Generic"), i + 1);
            lbls[i] = new Label { Text = labelText, Location = new Point(10, y + 3), Width = 100, TextAlign = ContentAlignment.MiddleRight };
            combos[i] = new ComboBox { Location = new Point(115, y), Width = 400, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            grp.Controls.AddRange([lbls[i], combos[i]]);
            y += 32;
        }
        grp.Height = y + 10;
        return lbls;
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
        // Settings menu (now contains Language)
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
        tabStats.Text = L("Tab.Stats");
        tabAppearance.Text = L("Tab.Appearance");
        tabInventory.Text = L("Tab.Inventory");
        tabEquipment.Text = L("Tab.Equipment"); // Unified: Equip + Dyes + Loadouts
        tabStorage.Text = L("Tab.Storage");
        tabBuffs.Text = L("Tab.Buffs");
        tabUpgrades.Text = L("Tab.Upgrades");
        tabSpawnPoints.Text = L("Tab.SpawnPoints");
        tabMisc.Text = L("Tab.Misc");

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

        // Tab 2: Stats
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

        // Tab 4: Inventory — refresh modifier
        _modInventory.RefreshLocale();

        // Tab 5: Equipment — refresh modifier + loadout selector
        _modEquip.RefreshLocale();
        _rbLoadout1.Text = L("Loadout.Select1");
        _rbLoadout2.Text = L("Loadout.Select2");
        _rbLoadout3.Text = L("Loadout.Select3");

        // Tab 6: Storage — refresh modifier
        _modStorage.RefreshLocale();
        subPiggyBank.Text = L("Storage.PiggyBank");
        subSafe.Text = L("Storage.Safe");
        subDefenderForge.Text = L("Storage.DefenderForge");
        subVoidVault.Text = L("Storage.VoidVault");

        // Tab 7: Buffs
        _lblBuffTitle.Text = L("Buffs.Title");
        _lblBuffType.Text = L("Buffs.Type");
        _lblBuffDuration.Text = L("Buffs.Duration");
        _btnBuffSet.Text = L("Storage.Set");
        _btnBuffClear.Text = L("Storage.Clear");

        // Tab 9: Upgrades
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

        // Tab 11: Spawn Points
        btnAddSpawn.Text = L("Spawn.Add");
        btnRemoveSpawn.Text = L("Spawn.Remove");
        dgvSpawnPoints.Columns["WorldId"].HeaderText = L("Spawn.WorldId");
        dgvSpawnPoints.Columns["WorldName"].HeaderText = L("Spawn.WorldName");
        dgvSpawnPoints.Columns["X"].HeaderText = L("Spawn.X");
        dgvSpawnPoints.Columns["Y"].HeaderText = L("Spawn.Y");

        // Tab 12: Misc
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

        // Grid column headers
        void UpdateGridHeaders(DataGridView dgv)
        {
            if (dgv.Columns["Name"] != null) dgv.Columns["Name"].HeaderText = L("Grid.Name");
            if (dgv.Columns["ID"] != null) dgv.Columns["ID"].HeaderText = L("Grid.ID");
            if (dgv.Columns["Stack"] != null) dgv.Columns["Stack"].HeaderText = L("Grid.Stack");
            if (dgv.Columns["Prefix"] != null) dgv.Columns["Prefix"].HeaderText = L("Grid.Prefix");
        }
        // Repopulate data if player is loaded (refresh display text with new language)
        if (_player != null)
        {
            SuspendLayout();
            try
            {
                // Refresh slot display text (re-reads item names from database without overwriting data)
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

                // Refresh browser display text without rebuilding rows
                _browserInventory.RefreshDisplayText();
                _browserEquip.RefreshDisplayText();
                _browserStorage.RefreshDisplayText();
                _browserBuffs.RefreshDisplayText();

                // Refresh modifier combo boxes (prefix items need locale-aware names)
                _modInventory.PopulatePrefixes();
                _modEquip.PopulatePrefixes();
                _modStorage.PopulatePrefixes();
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
