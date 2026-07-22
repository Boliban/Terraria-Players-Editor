namespace Terraria_Players_Editor.Models;

/// <summary>
/// Static database of Terraria item prefixes/modifiers and their display names.
/// Covers prefixes for all weapon types including 1.4.5 summoner prefixes (85-97).
/// </summary>
public static class PrefixData
{
    private static readonly Dictionary<byte, string> Prefixes = new()
    {
        { 0, "(none)" },
        // Universal
        { 1, "Large" }, { 2, "Massive" }, { 3, "Dangerous" }, { 4, "Savage" },
        { 5, "Sharp" }, { 6, "Pointy" }, { 7, "Tiny" }, { 8, "Terrible" },
        { 9, "Small" }, { 10, "Dull" }, { 11, "Unhappy" }, { 12, "Bulky" },
        { 13, "Shameful" }, { 14, "Heavy" }, { 15, "Light" },
        // Ranged
        { 16, "Sighted" }, { 17, "Rapid" }, { 18, "Hasty" }, { 19, "Intimidating" },
        { 20, "Deadly" }, { 21, "Staunch" }, { 22, "Awful" }, { 23, "Lethargic" },
        { 24, "Awkward" }, { 25, "Powerful" },
        // Magic
        { 26, "Mystic" }, { 27, "Adept" }, { 28, "Masterful" }, { 29, "Inept" },
        { 30, "Ignorant" }, { 31, "Deranged" }, { 32, "Intense" }, { 33, "Taboo" },
        { 34, "Celestial" }, { 35, "Furious" },
        // Melee
        { 36, "Keen" }, { 37, "Superior" }, { 38, "Forceful" }, { 39, "Broken" },
        { 40, "Damaged" }, { 41, "Shoddy" },
        { 42, "Quick" }, { 43, "Deadly2" }, { 44, "Agile" }, { 45, "Nimble" },
        { 46, "Murderous" }, { 47, "Slow" }, { 48, "Sluggish" }, { 49, "Lazy" },
        { 50, "Annoying" }, { 51, "Nasty" },
        // Melee/Universal (continued)
        { 52, "Manic" }, { 53, "Hurtful" }, { 54, "Strong" }, { 55, "Unpleasant" },
        { 56, "Weak" }, { 57, "Ruthless" }, { 58, "Frenzying" },
        // Universal best
        { 59, "Godly" }, { 60, "Demonic" }, { 61, "Zealous" },
        { 62, "Hard" }, { 63, "Guarding" }, { 64, "Armored" }, { 65, "Warding" },
        { 66, "Arcane" }, { 67, "Precise" }, { 68, "Lucky" }, { 69, "Jagged" },
        { 70, "Spiked" }, { 71, "Angry" }, { 72, "Menacing" },
        { 73, "Brisk" }, { 74, "Fleeting" }, { 75, "Hasty2" }, { 76, "Quick2" },
        { 77, "Wild" }, { 78, "Rash" }, { 79, "Intrepid" }, { 80, "Violent" },
        // Best universal
        { 81, "Legendary" }, { 82, "Unreal" }, { 83, "Mythical" },
        // Whip / summoner best
        { 84, "Legendary2" },
        // 1.4.5 summoner prefixes (85-97)
        { 85, "Fabled" }, { 86, "Loyal" }, { 87, "Worthy" }, { 88, "Focused" },
        { 89, "Patient" }, { 90, "Rabid" }, { 91, "Ill-Tempered" }, { 92, "Petty" },
        { 93, "Feeble" }, { 94, "Skittish" }, { 95, "Eager" }, { 96, "Ballistic" },
        { 97, "Scraggling" },
    };

    /// <summary>Gets the display name for a prefix ID.</summary>
    public static string GetName(byte prefix) =>
        Prefixes.TryGetValue(prefix, out var name) ? name : $"Unknown({prefix})";

    /// <summary>Gets the byte value for a prefix name. Returns 0 if not found.</summary>
    public static byte GetId(string name) =>
        Prefixes.FirstOrDefault(kv => kv.Value.Equals(name, StringComparison.OrdinalIgnoreCase)).Key;

    public static IReadOnlyDictionary<byte, string> All => Prefixes;
}
