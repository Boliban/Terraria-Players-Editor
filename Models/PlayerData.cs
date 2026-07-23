namespace Terraria_Players_Editor.Models;

/// <summary>
/// Complete Terraria player data model. Holds all data parsed from a .plr file.
/// </summary>
public sealed class PlayerData
{
    // === Raw data for safe round-trip saving ===
    public byte[]? RawData { get; set; }

    // === Header ===
    public int FileVersion { get; set; }
    public int Revision { get; set; }

    // === Hotbar ===
    public int SelectedItem { get; set; }

    // === Identity ===
    public string Name { get; set; } = "";
    public byte Difficulty { get; set; }  // 0=Softcore, 1=Mediumcore, 2=Hardcore, 3=Journey

    // === Time ===
    public long PlayTime { get; set; }  // ticks

    // === Core Stats ===
    public PlayerStats Stats { get; set; } = new();

    // === Appearance ===
    public PlayerAppearance Appearance { get; set; } = new();

    // === Permanent Upgrades ===
    public PermanentUpgrades Upgrades { get; set; } = new();

    // === Equipment (id + prefix only, no stack/favorited) ===
    public List<ItemData> Armor { get; set; } = new();           // 3 slots
    public List<ItemData> VanityArmor { get; set; } = new();     // 3 slots
    public List<ItemData> Accessories { get; set; } = new();     // up to 7 slots
    public List<ItemData> VanityAccessories { get; set; } = new(); // up to 7 slots
    public List<ItemData> ArmorDyes { get; set; } = new();       // 3 armor + 7 acc dyes = 10
    public List<ItemData> MiscEquips { get; set; } = new();      // 5: pet, light pet, minecart, mount, hook
    public List<ItemData> MiscEquipDyes { get; set; } = new();   // 5

    // === Inventory (with stack + favorited) ===
    public List<ItemData> MainInventory { get; set; } = new();   // 50 slots (1.4.4+)
    public List<ItemData> Coins { get; set; } = new();           // 4 slots
    public List<ItemData> Ammo { get; set; } = new();            // 4 slots

    // === Storage (with stack, no favorited) ===
    public List<ItemData> PiggyBank { get; set; } = new();       // 40 slots
    public List<ItemData> Safe { get; set; } = new();            // 40 slots
    public List<ItemData> DefenderForge { get; set; } = new();   // 40 slots
    public List<ItemData> VoidVault { get; set; } = new();       // 40 slots

    // === Trash ===
    public ItemData? TrashItem { get; set; } = new();

    // === Buffs ===
    public int[] BuffTypes { get; set; } = new int[44];
    public int[] BuffTimes { get; set; } = new int[44];

    // === Spawn Points ===
    public List<SpawnPointData> SpawnPoints { get; set; } = new();

    // === Meta / Flags ===
    public bool HotbarLocked { get; set; }
    public bool[] HideInfo { get; set; } = new bool[13];  // info accessory toggles

    // === Misc Counters ===
    public int AnglerQuestsFinished { get; set; }
    public int GolferScoreAccumulated { get; set; }
    public int TaxMoney { get; set; }
    public int NumberOfDeathsPvE { get; set; }
    public int NumberOfDeathsPvP { get; set; }

    // === Cooldowns (ticks) ===
    public int PotionDelay { get; set; }
    public int ManaPotionDelay { get; set; }
    public int RestorationPotionCd { get; set; }

    // === Builder / Controller ===
    public bool[] BuilderToggles { get; set; } = new bool[12];
    public int[] DPadRadialBindings { get; set; } = new int[4];
    public int[] BuilderAccStatus { get; set; } = new int[12];

    // === Loadouts ===
    public int CurrentLoadout { get; set; }
    public PlayerLoadout? Loadout2 { get; set; }
    public PlayerLoadout? Loadout3 { get; set; }

    // === Emotes ===
    public List<int> UnlockedEmotes { get; set; } = new();

    // === Journey Research ===
    public Dictionary<string, int> ResearchedItems { get; set; } = new();

    // === Helpers ===
    /// <summary>Formatted play time string (h:mm:ss).</summary>
    public string PlayTimeFormatted
    {
        get
        {
            var ts = TimeSpan.FromTicks(PlayTime);
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public string DifficultyName => Difficulty switch
    {
        0 => "Softcore (Classic)",
        1 => "Mediumcore",
        2 => "Hardcore",
        3 => "Journey",
        _ => "Unknown"
    };
}

/// <summary>
/// A saved equipment loadout (loadouts 2 and 3; loadout 1 is the main equipment).
/// </summary>
public sealed class PlayerLoadout
{
    public List<ItemData> Armor { get; set; } = new();         // 3
    public List<ItemData> VanityArmor { get; set; } = new();   // 3
    public List<ItemData> Accessories { get; set; } = new();   // 7
    public List<ItemData> VanityAccessories { get; set; } = new(); // 7
    public List<ItemData> ArmorDyes { get; set; } = new();     // 10
    public List<ItemData> MiscEquips { get; set; } = new();    // 5
    public List<ItemData> MiscEquipDyes { get; set; } = new(); // 5
}
