using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads buff names and types from the embedded buffs.json,
/// extracted from the game's buffIDs database.
/// </summary>
public static class BuffDatabase
{
    private static readonly Dictionary<int, BuffEntry> Buffs = new();
    private static bool _loaded;

    public static string GetName(int buffId) =>
        Buffs.TryGetValue(buffId, out var b) ? b.Name : $"Buff {buffId}";

    public static string GetType(int buffId) =>
        Buffs.TryGetValue(buffId, out var b) ? b.Type : "Unknown";

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
            var dict = JsonSerializer.Deserialize<Dictionary<string, BuffEntry>>(json);
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
        public string Type { get; set; } = "";
    }
}
