using System.Text.Json;

namespace Terraria_Players_Editor.Services.Memory;

/// <summary>
/// Persisted configuration for the memory editor: the Player pointer chain and
/// the field offsets (from DemoFile/Terraria-Player.CSX, Terraria 1.4.5.6).
/// Stored in %AppData%/TerrariaPlayersEditor/memory_settings.json so offsets can
/// be adjusted for other game versions without recompiling.
/// </summary>
public static class MemorySettings
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TerrariaPlayersEditor");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "memory_settings.json");

    /// <summary>Anchor: main thread stack base minus this value (threadstack0 - 984 = -0x3D8).</summary>
    public static uint ChainStackSubtract { get; set; } = 0x3D8;

    /// <summary>Pointer chain offsets (hex), CE semantics: read + add per step.</summary>
    public static List<uint> ChainOffsets { get; set; } = new() { 0x32C, 0x4, 0x550, 0x0, 0x0, 0xD8 };

    /// <summary>Whether the chain result is dereferenced one final time. False: the chain result IS the Player base.</summary>
    public static bool ChainFinalDeref { get; set; } = false;

    /// <summary>Auto-refresh the inventory while connected.</summary>
    public static bool AutoRefresh { get; set; } = true;

    /// <summary>When the pointer chain fails, scan the game memory to locate the Player object.</summary>
    public static bool AutoScanFallback { get; set; } = true;

    public static PlayerMemoryOffsets Offsets { get; } = new();

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return;
            var json = File.ReadAllText(SettingsFile);
            var s = JsonSerializer.Deserialize<MemorySettingsData>(json);
            if (s == null) return;
            ChainStackSubtract = s.ChainStackSubtract;
            if (s.ChainOffsets is { Count: > 0 })
                ChainOffsets = s.ChainOffsets;
            ChainFinalDeref = s.ChainFinalDeref;
            AutoRefresh = s.AutoRefresh;
            AutoScanFallback = s.AutoScanFallback;
            Offsets.LoadFrom(s.Offsets);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load memory settings: {ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var s = new MemorySettingsData
            {
                ChainStackSubtract = ChainStackSubtract,
                ChainOffsets = ChainOffsets,
                ChainFinalDeref = ChainFinalDeref,
                AutoRefresh = AutoRefresh,
                AutoScanFallback = AutoScanFallback,
                Offsets = Offsets.Export()
            };
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(s));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save memory settings: {ex.Message}");
        }
    }

    /// <summary>Parse a comma-separated hex list ("32C, 4, 550, 0") into offsets.</summary>
    public static bool TryParseChain(string text, out List<uint> offsets)
    {
        offsets = new List<uint>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                offsets.Add(Convert.ToUInt32(part, 16));
            }
            catch
            {
                return false;
            }
        }
        return offsets.Count > 0;
    }

    public static string ChainToString(IReadOnlyList<uint> offsets) =>
        string.Join(", ", offsets.Select(o => o.ToString("X")));

    private sealed class MemorySettingsData
    {
        public uint ChainStackSubtract { get; set; } = 0x3D8;
        public List<uint>? ChainOffsets { get; set; }
        public bool ChainFinalDeref { get; set; }
        public bool AutoRefresh { get; set; } = true;
        public bool AutoScanFallback { get; set; } = true;
        public PlayerMemoryOffsets.OffsetData? Offsets { get; set; }
    }
}

/// <summary>
/// Field offsets inside the Terraria Player / Item / Chest objects, extracted
/// from DemoFile/Terraria-Player.CSX (Terraria 1.4.5.6). Offsets are relative to
/// the object base (offset 0 = vtable/MethodTable pointer).
/// </summary>
public sealed class PlayerMemoryOffsets
{
    // === Player fields (CSX "Terraria.Player" structure) ===
    public uint Name = 0x8C;          // System.String reference
    public uint Armor = 0xB4;         // Item[3]
    public uint Dye = 0xB8;           // Item[10]
    public uint MiscEquips = 0xBC;    // Item[5]
    public uint MiscDyes = 0xC0;      // Item[5]
    public uint TrashItem = 0xC4;     // Item reference
    public uint BuffType = 0xC8;      // int[44]
    public uint BuffTime = 0xCC;      // int[44]
    public uint Inventory = 0xD8;     // Item[59]: 0-49 main, 50-53 coins, 54-57 ammo, 58 trash
    public uint Bank = 0xE4;          // Chest (piggy bank)
    public uint Bank2 = 0xE8;         // Chest (safe)
    public uint Bank3 = 0xEC;         // Chest (defender's forge)
    public uint Bank4 = 0xF0;         // Chest (void vault)
    public uint TaxMoney = 0x1A8;
    public uint DeathsPvE = 0x1B0;
    public uint SkinVariant = 0x2F8;
    public uint StatLifeMax = 0x424;
    public uint StatLife = 0x42C;
    public uint StatManaMax = 0x434;
    public uint StatMana = 0x430;
    public uint Hair = 0x504;
    public uint ExtraAccessory = 0x6D3;
    public uint Difficulty = 0x769;
    public uint HairDye = 0x89E;
    public uint HideMisc = 0xA04;

    // === Item fields (CSX "Terraria.Item" structure) ===
    public uint ItemType = 0x50;
    public uint ItemStack = 0x64;
    public uint ItemPrefix = 0x12E;
    public uint ItemFavorited = 0x10C;

    // === Chest fields (CSX "Terraria.Chest" structure) ===
    public uint ChestItemArray = 0x04;   // Item[40] reference

    // === .NET array object layout (x86) ===
    public const uint ArrayLength = 0x04;
    public const uint ArrayData = 0x08;

    // === .NET string object layout (x86) ===
    public const uint StringLength = 0x04;
    public const uint StringData = 0x08;

    public sealed class OffsetData
    {
        public uint? Name { get; set; }
        public uint? Armor { get; set; }
        public uint? Dye { get; set; }
        public uint? MiscEquips { get; set; }
        public uint? MiscDyes { get; set; }
        public uint? TrashItem { get; set; }
        public uint? BuffType { get; set; }
        public uint? BuffTime { get; set; }
        public uint? Inventory { get; set; }
        public uint? Bank { get; set; }
        public uint? Bank2 { get; set; }
        public uint? Bank3 { get; set; }
        public uint? Bank4 { get; set; }
        public uint? StatLife { get; set; }
        public uint? StatMana { get; set; }
        public uint? Difficulty { get; set; }
        public uint? ItemType { get; set; }
        public uint? ItemStack { get; set; }
        public uint? ItemPrefix { get; set; }
        public uint? ItemFavorited { get; set; }
        public uint? ChestItemArray { get; set; }
    }

    public void LoadFrom(OffsetData? d)
    {
        if (d == null) return;
        if (d.Name.HasValue) Name = d.Name.Value;
        if (d.Armor.HasValue) Armor = d.Armor.Value;
        if (d.Dye.HasValue) Dye = d.Dye.Value;
        if (d.MiscEquips.HasValue) MiscEquips = d.MiscEquips.Value;
        if (d.MiscDyes.HasValue) MiscDyes = d.MiscDyes.Value;
        if (d.TrashItem.HasValue) TrashItem = d.TrashItem.Value;
        if (d.BuffType.HasValue) BuffType = d.BuffType.Value;
        if (d.BuffTime.HasValue) BuffTime = d.BuffTime.Value;
        if (d.Inventory.HasValue) Inventory = d.Inventory.Value;
        if (d.Bank.HasValue) Bank = d.Bank.Value;
        if (d.Bank2.HasValue) Bank2 = d.Bank2.Value;
        if (d.Bank3.HasValue) Bank3 = d.Bank3.Value;
        if (d.Bank4.HasValue) Bank4 = d.Bank4.Value;
        if (d.StatLife.HasValue) StatLife = d.StatLife.Value;
        if (d.StatMana.HasValue) StatMana = d.StatMana.Value;
        if (d.Difficulty.HasValue) Difficulty = d.Difficulty.Value;
        if (d.ItemType.HasValue) ItemType = d.ItemType.Value;
        if (d.ItemStack.HasValue) ItemStack = d.ItemStack.Value;
        if (d.ItemPrefix.HasValue) ItemPrefix = d.ItemPrefix.Value;
        if (d.ItemFavorited.HasValue) ItemFavorited = d.ItemFavorited.Value;
        if (d.ChestItemArray.HasValue) ChestItemArray = d.ChestItemArray.Value;
    }

    public OffsetData Export() => new()
    {
        Name = Name, Armor = Armor, Dye = Dye, MiscEquips = MiscEquips, MiscDyes = MiscDyes,
        TrashItem = TrashItem, BuffType = BuffType, BuffTime = BuffTime, Inventory = Inventory,
        Bank = Bank, Bank2 = Bank2, Bank3 = Bank3, Bank4 = Bank4,
        StatLife = StatLife, StatMana = StatMana, Difficulty = Difficulty,
        ItemType = ItemType, ItemStack = ItemStack, ItemPrefix = ItemPrefix,
        ItemFavorited = ItemFavorited, ChestItemArray = ChestItemArray
    };
}
