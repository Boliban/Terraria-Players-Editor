namespace Terraria_Players_Editor;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Menu
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem fileMenu = null!;
    private ToolStripMenuItem openMenuItem = null!;
    private ToolStripMenuItem saveMenuItem = null!;
    private ToolStripMenuItem saveAsMenuItem = null!;
    private ToolStripMenuItem exitMenuItem = null!;

    // Status
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripProgressBar statusProgress = null!;

    // Tab control
    private TabControl tabControl = null!;

    // === Tab 1: Player Info ===
    private TabPage tabPlayerInfo = null!;
    private Label lblPlayerName = null!;
    private TextBox txtPlayerName = null!;
    private Label lblDifficulty = null!;
    private ComboBox cmbDifficulty = null!;
    private Label lblPlayTime = null!;
    private TextBox txtPlayTime = null!;
    private Label lblFileVersion = null!;
    private TextBox txtFileVersion = null!;
    private Label lblLoadout = null!;
    private ComboBox cmbCurrentLoadout = null!;

    // === Tab 2: Stats ===
    private TabPage tabStats = null!;
    private GroupBox grpHealth = null!;
    private Label lblHealth = null!;
    private NumericUpDown nudHealth = null!;
    private Label lblMaxHealth = null!;
    private NumericUpDown nudMaxHealth = null!;
    private GroupBox grpMana = null!;
    private Label lblMana = null!;
    private NumericUpDown nudMana = null!;
    private Label lblMaxMana = null!;
    private NumericUpDown nudMaxMana = null!;
    private GroupBox grpCounters = null!;
    private Label lblDeathsPvE = null!;
    private NumericUpDown nudDeathsPvE = null!;
    private Label lblDeathsPvP = null!;
    private NumericUpDown nudDeathsPvP = null!;
    private Label lblTaxMoney = null!;
    private NumericUpDown nudTaxMoney = null!;
    private Label lblAnglerQuests = null!;
    private NumericUpDown nudAnglerQuests = null!;
    private Label lblGolferScore = null!;
    private NumericUpDown nudGolferScore = null!;

    // === Tab 3: Appearance ===
    private TabPage tabAppearance = null!;
    private Label lblHairStyle = null!;
    private NumericUpDown nudHairStyle = null!;
    private Label lblHairDye = null!;
    private NumericUpDown nudHairDye = null!;
    private Label lblSkinVariant = null!;
    private ComboBox cmbSkinVariant = null!;
    private GroupBox grpColors = null!;
    private Button[] colorButtons = null!;
    private Panel[] colorPanels = null!;
    private GroupBox grpVisibility = null!;
    private CheckBox[] chkHideVisual = null!;
    private CheckBox[] chkHideMisc = null!;

    // === Tab 4: Inventory ===
    private TabPage tabInventory = null!;
    private DataGridView dgvInventory = null!;
    private GroupBox grpInventoryEdit = null!;
    private ComboBox cmbItemSearch = null!;
    private Label lblStack = null!;
    private NumericUpDown nudStack = null!;
    private Label lblPrefix = null!;
    private ComboBox cmbPrefix = null!;
    private CheckBox chkFavorite = null!;
    private Button btnSetItem = null!;
    private Button btnClearItem = null!;
    private GroupBox grpCoins = null!;
    private DataGridView dgvCoins = null!;
    private GroupBox grpAmmo = null!;
    private DataGridView dgvAmmo = null!;

    // === Tab 5: Equipment ===
    private TabPage tabEquipment = null!;
    private GroupBox grpArmorSlots = null!;
    private GroupBox grpVanityArmorSlots = null!;
    private GroupBox grpAccessorySlots = null!;
    private GroupBox grpVanityAccessorySlots = null!;
    private GroupBox grpMiscEquipSlots = null!;
    private ComboBox[] cmbArmorSlots = null!;
    private ComboBox[] cmbVanityArmorSlots = null!;
    private ComboBox[] cmbAccessorySlots = null!;
    private ComboBox[] cmbVanityAccessorySlots = null!;
    private ComboBox[] cmbMiscEquipSlots = null!;
    private ComboBox[] cmbArmorPrefixes = null!;
    private ComboBox[] cmbVanityArmorPrefixes = null!;
    private ComboBox[] cmbAccessoryPrefixes = null!;
    private ComboBox[] cmbVanityAccessoryPrefixes = null!;

    // === Tab 6: Dyes ===
    private TabPage tabDyes = null!;
    private GroupBox grpArmorDyes = null!;
    private GroupBox grpAccessoryDyes = null!;
    private GroupBox grpMiscEquipDyes = null!;
    private ComboBox[] cmbArmorDyeSlots = null!;
    private ComboBox[] cmbAccessoryDyeSlots = null!;
    private ComboBox[] cmbMiscEquipDyeSlots = null!;

    // Label arrays for localization refresh
    private Label[] lblEquipArmor = null!;
    private Label[] lblEquipVanityArmor = null!;
    private Label[] lblEquipAccessory = null!;
    private Label[] lblEquipVanityAcc = null!;
    private Label[] lblEquipMisc = null!;
    private Label[] lblDyeArmor = null!;
    private Label[] lblDyeAccessory = null!;
    private Label[] lblDyeMisc = null!;

    // Color label references (7 colors: Hair, Skin, Eyes, Shirt, UnderShirt, Pants, Shoes)
    private Label[] lblColors = null!;

    // Loadout tab GroupBox references for text refresh
    private GroupBox[] grpLoadout2Boxes = null!;
    private GroupBox[] grpLoadout3Boxes = null!;
    private Label[][] lblLoadout2Labels = null!;
    private Label[][] lblLoadout3Labels = null!;

    // === Tab 7: Storage ===
    private TabPage tabStorage = null!;
    private TabControl tabStorageSub = null!;
    private TabPage subPiggyBank = null!;
    private TabPage subSafe = null!;
    private TabPage subDefenderForge = null!;
    private TabPage subVoidVault = null!;
    private DataGridView dgvPiggyBank = null!;
    private DataGridView dgvSafe = null!;
    private DataGridView dgvDefenderForge = null!;
    private DataGridView dgvVoidVault = null!;
    private GroupBox grpStorageEdit = null!;
    private ComboBox cmbStorageItemSearch = null!;
    private Label lblStorageStack = null!;
    private NumericUpDown nudStorageStack = null!;
    private Label lblStoragePrefix = null!;
    private ComboBox cmbStoragePrefix = null!;
    private Button btnStorageSet = null!;
    private Button btnStorageClear = null!;

    // === Tab 8: Buffs ===
    private TabPage tabBuffs = null!;
    private DataGridView dgvBuffs = null!;

    // === Tab 9: Upgrades ===
    private TabPage tabUpgrades = null!;
    private CheckBox chkExtraAccessory = null!;
    private CheckBox chkAegisCrystal = null!;
    private CheckBox chkAegisFruit = null!;
    private CheckBox chkArcaneCrystal = null!;
    private CheckBox chkGalaxyPearl = null!;
    private CheckBox chkGummyWorm = null!;
    private CheckBox chkAmbrosia = null!;
    private CheckBox chkArtisanBread = null!;
    private CheckBox chkBiomeTorches = null!;
    private CheckBox chkUsingBiomeTorches = null!;
    private Label lblSuperCart = null!;
    private NumericUpDown nudSuperCart = null!;
    private CheckBox chkSuperCartEnabled = null!;

    // === Tab 10: Loadouts ===
    private TabPage tabLoadouts = null!;
    private TabControl tabLoadoutSub = null!;
    private TabPage subLoadout2 = null!;
    private TabPage subLoadout3 = null!;
    private ComboBox[] cmbL2Armor = null!;
    private ComboBox[] cmbL2VanityArmor = null!;
    private ComboBox[] cmbL2Accessory = null!;
    private ComboBox[] cmbL2VanityAccessory = null!;
    private ComboBox[] cmbL2ArmorDyes = null!;
    private ComboBox[] cmbL2AccessoryDyes = null!;
    private ComboBox[] cmbL2MiscEquip = null!;
    private ComboBox[] cmbL2MiscEquipDyes = null!;
    private ComboBox[] cmbL3Armor = null!;
    private ComboBox[] cmbL3VanityArmor = null!;
    private ComboBox[] cmbL3Accessory = null!;
    private ComboBox[] cmbL3VanityAccessory = null!;
    private ComboBox[] cmbL3ArmorDyes = null!;
    private ComboBox[] cmbL3AccessoryDyes = null!;
    private ComboBox[] cmbL3MiscEquip = null!;
    private ComboBox[] cmbL3MiscEquipDyes = null!;

    // === Tab 11: Spawn Points ===
    private TabPage tabSpawnPoints = null!;
    private DataGridView dgvSpawnPoints = null!;
    private Button btnAddSpawn = null!;
    private Button btnRemoveSpawn = null!;

    // === Tab 12: Misc ===
    private TabPage tabMisc = null!;
    private CheckBox chkHotbarLocked = null!;
    private GroupBox grpHideInfo = null!;
    private CheckBox[] chkHideInfo = null!;
    private GroupBox grpCooldowns = null!;
    private Label lblPotionDelay = null!;
    private NumericUpDown nudPotionDelay = null!;
    private Label lblManaPotionDelay = null!;
    private NumericUpDown nudManaPotionDelay = null!;
    private Label lblRestorationCd = null!;
    private NumericUpDown nudRestorationCd = null!;

    // === New grid/icon controls ===
    private Controls.ItemBrowser _browserInventory = null!;
    private Controls.ItemModifier _modInventory = null!;
    private Controls.SlotGrid _gridInventory = null!;
    private Controls.SlotGrid _gridCoins = null!;
    private Controls.SlotGrid _gridAmmo = null!;
    private Controls.ItemBrowser _browserEquip = null!;
    private Controls.ItemModifier _modEquip = null!;
    private Controls.SlotGrid[] _equipSlots = null!;
    private Controls.SlotGrid[] _vanitySlots = null!;
    private Controls.SlotGrid[] _accSlots = null!;
    private Controls.SlotGrid[] _vaccSlots = null!;
    private Controls.SlotGrid[] _miscSlots = null!;
    private Controls.SlotGrid[] _armorDyeSlots = null!;
    private Controls.SlotGrid[] _accDyeSlots = null!;
    private Controls.SlotGrid[] _miscDyeSlots = null!;
    private FlowLayoutPanel _loadoutSelector = null!;
    private RadioButton _rbLoadout1 = null!;
    private RadioButton _rbLoadout2 = null!;
    private RadioButton _rbLoadout3 = null!;
    private int _activeLoadout = 0;
    private Controls.ItemBrowser _browserStorage = null!;
    private Controls.ItemModifier _modStorage = null!;
    private Controls.SlotGrid _gridPiggy = null!;
    private Controls.SlotGrid _gridSafe = null!;
    private Controls.SlotGrid _gridDefender = null!;
    private Controls.SlotGrid _gridVoid = null!;
    private int _activeStorageIdx = -1;
    private Controls.ItemBrowser _browserBuffs = null!;
    private Controls.SlotGrid _gridBuffs = null!;
    private Label _lblBuffTitle = null!;
    private Label _lblBuffType = null!;
    private NumericUpDown _nudBuffType = null!;
    private Label _lblBuffDuration = null!;
    private NumericUpDown _nudBuffDuration = null!;
    private Button _btnBuffSet = null!;
    private Button _btnBuffClear = null!;
    private SplitContainer _splitInventory = null!;
    private SplitContainer _splitEquip = null!;
    private SplitContainer _splitStorage = null!;
    private SplitContainer _splitBuffs = null!;
    private bool _populating;
    private Controls.SlotGrid? _activeEquipGrid;
    private string _activeSlotGrid = "main";

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }
}
