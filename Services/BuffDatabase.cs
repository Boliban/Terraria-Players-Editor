using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads buff names, internal names, and types from the embedded buffs.json,
/// extracted from the game's buffIDs database.
/// Supports Chinese localization via GameLocaleLoader.
/// </summary>
public static class BuffDatabase
{
    private static readonly Dictionary<int, BuffEntry> Buffs = new();
    private static bool _loaded;

    /// <summary>Get the display name for a buff ID. Uses locale translation when available.</summary>
    public static string GetName(int buffId)
    {
        EnsureLoaded();
        if (!Buffs.TryGetValue(buffId, out var b))
            return $"Buff {buffId}";

        if (!string.IsNullOrEmpty(b.Internal))
        {
            // Try current locale translation first
            string? localeName = AppLocale.Current == AppLocale.Lang.ZH
                ? GameLocaleLoader.GetBuffName(b.Internal)
                : GameLocaleLoader.GetBuffNameEn(b.Internal);

            if (localeName != null) return localeName;
        }

        // Fallback to name field in buffs.json
        return string.IsNullOrEmpty(b.Name) ? "" : b.Name;
    }

    /// <summary>Get the raw type string ("Buff" or "Debuff") from the database.</summary>
    public static string GetType(int buffId)
    {
        EnsureLoaded();
        return Buffs.TryGetValue(buffId, out var b) ? b.Type : "Unknown";
    }

    /// <summary>Get the internal name (e.g. "ObsidianSkin") for a buff ID.</summary>
    public static string GetInternalName(int buffId)
    {
        EnsureLoaded();
        return Buffs.TryGetValue(buffId, out var b) ? b.Internal : "";
    }

    /// <summary>Get all buff IDs in sorted order.</summary>
    public static IReadOnlyList<int> GetAllIds()
    {
        EnsureLoaded();
        return Buffs.Keys.OrderBy(k => k).ToList();
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Terraria_Players_Editor.Data.buffs.json");
            if (stream == null) return;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, BuffEntry>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dict != null)
            {
                foreach (var kv in dict)
                {
                    if (int.TryParse(kv.Key, out var id))
                        Buffs[id] = kv.Value;
                }
            }
        }
        catch { }
    }

    private sealed class BuffEntry
    {
        public string Name { get; set; } = "";
        public string Internal { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
