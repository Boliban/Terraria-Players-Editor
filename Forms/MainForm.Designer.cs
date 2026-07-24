#nullable enable
using Terraria_Players_Editor.Controls;
using Terraria_Players_Editor.Models;

namespace Terraria_Players_Editor;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

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

    // === Tab 4: Items (merged: Inventory + Equipment + Storage) ===
    private TabPage tabItems = null!;
    private Controls.ItemBrowser _browserItems = null!;
    private Controls.ItemModifier _modItems = null!;
    private SplitContainer _splitItems = null!;
    private Panel _scrollPanelItems = null!;
    private GroupBox _grpInventorySection = null!;
    private GroupBox _grpEquipmentSection = null!;
    private GroupBox _grpStorageSection = null!;

    // Inventory grids
    private Controls.SlotGrid _gridInventory = null!;
    private Controls.SlotGrid _gridCoins = null!;
    private Controls.SlotGrid _gridAmmo = null!;

    // Equipment grids
    private Controls.SlotGrid[] _equipSlots = null!;
    private Controls.SlotGrid[] _vanitySlots = null!;
    private Controls.SlotGrid[] _accSlots = null!;
    private Controls.SlotGrid[] _vaccSlots = null!;
    private Controls.SlotGrid[] _miscSlots = null!;
    private Controls.SlotGrid[] _armorDyeSlots = null!;
    private Controls.SlotGrid[] _accDyeSlots = null!;
    private Controls.SlotGrid[] _miscDyeSlots = null!;

    // Color label references (7 colors: Hair, Skin, Eyes, Shirt, UnderShirt, Pants, Shoes)
    private Label[] lblColors = null!;

    // Storage grids + sub-tabs
    private TabControl tabStorageSub = null!;
    private TabPage subPiggyBank = null!;
    private TabPage subSafe = null!;
    private TabPage subDefenderForge = null!;
    private TabPage subVoidVault = null!;
    private Controls.SlotGrid _gridPiggy = null!;
    private Controls.SlotGrid _gridSafe = null!;
    private Controls.SlotGrid _gridDefender = null!;
    private Controls.SlotGrid _gridVoid = null!;

    // Loadout selector (inside Equipment sub-tab)
    private FlowLayoutPanel _loadoutSelector = null!;
    private RadioButton _rbLoadout1 = null!;
    private RadioButton _rbLoadout2 = null!;
    private RadioButton _rbLoadout3 = null!;
    private int _activeLoadout = 0;

    // === Tab 5: Buffs ===
    private TabPage tabBuffs = null!;
    private Controls.ItemBrowser _browserBuffs = null!;
    private Controls.SlotGrid _gridBuffs = null!;
    private Label _lblBuffTitle = null!;
    private Label _lblBuffType = null!;
    private NumericUpDown _nudBuffType = null!;
    private Label _lblBuffDuration = null!;
    private NumericUpDown _nudBuffDuration = null!;
    private Label _lblBuffTimeUnit = null!;
    private Button _btnBuffSet = null!;
    private Button _btnBuffClear = null!;
    private SplitContainer _splitBuffs = null!;

    // === Tab 6: Upgrades ===
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

    // === Tab 7: Spawn Points ===
    private TabPage tabSpawnPoints = null!;
    private DataGridView dgvSpawnPoints = null!;
    private Button btnAddSpawn = null!;
    private Button btnRemoveSpawn = null!;

    // === Tab 8: Misc ===
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

    // Shared modifier tracking
    private Controls.SlotGrid? _activeModGrid;
    private List<ItemData>? _activeModList;
    private List<Controls.SlotGrid> _allItemGrids = null!;
    private string _activeModContext = "";
    private int _activeStorageIdx = -1;
    private int _cachedBuffType;
    private int _cachedBuffDur;
    private bool _populating;

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
