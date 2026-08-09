using System.Drawing;
using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads the game's creative-menu item groups (item-groups.json, extracted from
/// CreativeHelper.ItemGroup) and maps them onto 16 merged supergroups.
/// Provides id → group/supergroup lookup, localized display names, and the
/// category text color used by the item browser's colored-text feature.
/// </summary>
public static class ItemGroupDatabase
{
    /// <summary>A merged supergroup: display key + the game group keys it contains.</summary>
    public readonly record struct SuperGroup(string Key, string[] GroupKeys);

    /// <summary>
    /// Merged supergroup definitions (54 game groups → 16 supergroups).
    /// ConsumableThatDoesNotDamage has no natural category, so it lands in Misc.
    /// </summary>
    public static readonly SuperGroup[] SuperGroups =
    {
        new("Weapon",     ["MeleeWeapon", "RangedWeapon", "MagicWeapon", "SummonWeapon", "ConsumableThatDamages"]),
        new("Armor",      ["Headgear", "Torso", "Pants"]),
        new("Accessories",["Accessories", "Hook", "VanityPet"]),
        new("Tools",      ["Pickaxe", "Axe", "Hammer", "Wands"]),
        new("Potions",    ["LifePotions", "ManaPotions", "BuffPotion", "Flask", "Food"]),
        new("Materials",  ["DyeMaterial", "AlchemySeeds", "AlchemyPlants", "Wood"]),
        new("BlockWall",  ["Blocks", "Walls"]),
        new("Placeable",  ["PlaceableObjects", "CraftingObjects", "Torches", "Paint"]),
        new("Wiring",     ["Wiring", "Minecart"]),
        new("Ammo",       ["Ammo"]),
        new("KeyChest",   ["Keys", "Crates", "GoodieBags", "BossBags", "BossItem", "EventItem"]),
        new("Fishing",    ["FishingRods", "FishingBait", "FishingQuestFish"]),
        new("Mount",      ["Mount"]),
        new("Dye",        ["Dye", "HairDye"]),
        new("Coin",       ["Coin"]),
        new("Misc",       ["Rope", "Solutions", "Glowsticks", "Bombs", "Golf",
                            "RemainingUseItems", "EverythingElse", "ConsumableThatDoesNotDamage"]),
    };

    private static readonly Dictionary<int, string> IdToGroup = new();       // item id → game group key
    private static readonly Dictionary<string, string> GroupToSuper = new(); // group key → supergroup key
    private static readonly List<string> SuperOrder = new();                 // supergroup keys (display order)
    private static readonly List<string> GroupOrder = new();                 // group keys, flattened by supergroup

    /// <summary>Whether the embedded item-groups.json was loaded successfully.</summary>
    public static bool IsAvailable { get; private set; }

    static ItemGroupDatabase()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Terraria_Players_Editor.Data.item-groups.json");
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: item-groups.json not found as embedded resource.");
                return;
            }
            using var reader = new StreamReader(stream);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            // Top-level property name contains a dot — use TryGetProperty on the root.
            if (!doc.RootElement.TryGetProperty("CreativeHelper.ItemGroup", out var groups))
            {
                System.Diagnostics.Debug.WriteLine("Warning: item-groups.json missing 'CreativeHelper.ItemGroup'.");
                return;
            }

            foreach (var prop in groups.EnumerateObject())
            {
                foreach (var el in prop.Value.EnumerateArray())
                {
                    if (el.TryGetInt32(out var id) && id > 0)
                        IdToGroup[id] = prop.Name;
                }
            }

            // Build the supergroup mappings in declaration order (display order).
            foreach (var sg in SuperGroups)
            {
                SuperOrder.Add(sg.Key);
                foreach (var g in sg.GroupKeys)
                {
                    GroupToSuper[g] = sg.Key;
                    GroupOrder.Add(g);
                }
            }

            // Dev-time completeness check: every loaded group must map to a supergroup.
            var loadedGroups = groups.EnumerateObject().Select(p => p.Name).ToHashSet();
            foreach (var missing in loadedGroups.Where(g => !GroupToSuper.ContainsKey(g)))
                System.Diagnostics.Debug.WriteLine($"ItemGroupDatabase: group '{missing}' not mapped to a supergroup!");

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load item groups: {ex.Message}");
        }
    }

    /// <summary>Get the game group key for an item ID. Returns "Unknown" when not found.</summary>
    public static string GetGroup(int id)
    {
        return IdToGroup.TryGetValue(id, out var g) ? g : "Unknown";
    }

    /// <summary>Get the merged supergroup key for a game group key. Null when unmapped.</summary>
    public static string? GetSuperGroup(string groupKey)
    {
        return GroupToSuper.TryGetValue(groupKey, out var sg) ? sg : null;
    }

    /// <summary>All 54 game group keys in supergroup display order.</summary>
    public static IReadOnlyList<string> GetAllGroupKeys() => GroupOrder;

    /// <summary>All 16 supergroup keys in display order.</summary>
    public static IReadOnlyList<string> GetAllSuperGroupKeys() => SuperOrder;

    /// <summary>Localized display name for a game group key.</summary>
    public static string GetGroupDisplayName(string key) => AppLocale.Get("Browser.Group." + key) ?? key;

    /// <summary>Localized display name for a supergroup key.</summary>
    public static string GetSuperGroupDisplayName(string key) => AppLocale.Get("Browser.Super." + key) ?? key;

    /// <summary>
    /// Category text color for an item ID (respects the colored-text setting).
    /// Returns null when coloring is off, the item is Misc/unmapped, or the
    /// database is unavailable — callers fall back to the default text color.
    /// </summary>
    public static Color? GetItemColor(int id)
    {
        if (!SettingsManager.EnableColoredText) return null;
        var superKey = GetSuperGroup(GetGroup(id));
        return superKey == null ? null : GetSuperGroupColor(superKey);
    }

    /// <summary>Color for a supergroup key. Misc/unmapped returns null (default text color).</summary>
    public static Color? GetSuperGroupColor(string? superKey)
    {
        return superKey switch
        {
            "Weapon" => ThemeManager.ItemColorRed,
            "Ammo" => ThemeManager.ItemColorRed,
            "Armor" => ThemeManager.ItemColorBlue,
            "Potions" => ThemeManager.ItemColorGreen,
            "Materials" => ThemeManager.ItemColorYellow,
            "KeyChest" => ThemeManager.ItemColorYellow,
            "Coin" => ThemeManager.ItemColorYellow,
            "BlockWall" => ThemeManager.ItemColorBlueGray,
            "Placeable" => ThemeManager.ItemColorCyan,
            "Wiring" => ThemeManager.ItemColorOrange,
            "Tools" => ThemeManager.ItemColorOrange,
            "Accessories" => ThemeManager.ItemColorTeal,
            "Mount" => ThemeManager.ItemColorTeal,
            "Dye" => ThemeManager.ItemColorTeal,
            "Fishing" => ThemeManager.ItemColorLightBlue,
            _ => null, // Misc and unknown → default text color
        };
    }
}
