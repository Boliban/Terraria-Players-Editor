namespace Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

/// <summary>
/// Loads and queries the embedded Terraria item database (items.json).
/// Provides bidirectional lookup between item IDs and names, plus search/filter.
/// </summary>
public static class ItemDatabase
{
    private static readonly Dictionary<int, string> IdToName = new();
    private static readonly Dictionary<string, int> NameToId = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> IdToCategory = new();

    public static int MaxId { get; private set; }

    static ItemDatabase()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Terraria_Players_Editor.Data.items.json");
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: items.json not found as embedded resource.");
                return;
            }
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonItemEntry>>(json);

            if (items == null) return;

            foreach (var item in items)
            {
                IdToName[item.Id] = item.Name;
                NameToId[item.Name] = item.Id;
                IdToCategory[item.Id] = item.Category ?? "None";
                if (item.Id > MaxId) MaxId = item.Id;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load item database: {ex.Message}");
        }
    }

    /// <summary>Get item name by ID. Returns "(empty)" or "Unknown(N)" if not found.
    /// Automatically uses Chinese name when AppLocale is set to ZH and translation exists.</summary>
    public static string GetName(int id)
    {
        if (id == 0) return AppLocale.Current == AppLocale.Lang.ZH ? "(空)" : "(empty)";
        if (!IdToName.TryGetValue(id, out var enName))
            return $"Unknown({id})";

        if (AppLocale.Current == AppLocale.Lang.ZH)
        {
            var zhName = GameLocaleLoader.GetItemName(enName);
            if (zhName != null) return zhName;
        }
        return enName;
    }

    /// <summary>Get item ID by name. Returns -1 if not found.</summary>
    public static int GetId(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        return NameToId.TryGetValue(name.Trim(), out var id) ? id : -1;
    }

    /// <summary>Get item category by ID.</summary>
    public static string GetCategory(int id)
    {
        return IdToCategory.TryGetValue(id, out var cat) ? cat : "Unknown";
    }

    /// <summary>Search items by name substring. Case-insensitive.</summary>
    public static List<ItemLookup> Search(string? query, int maxResults = 100)
    {
        var results = new List<ItemLookup>();
        if (string.IsNullOrWhiteSpace(query))
        {
            // Return first N items
            foreach (var kv in IdToName.Take(maxResults))
                results.Add(new ItemLookup(kv.Key, kv.Value, IdToCategory.GetValueOrDefault(kv.Key, "")));
            return results;
        }

        var q = query.Trim();
        foreach (var kv in IdToName)
        {
            if (kv.Value.Contains(q, StringComparison.OrdinalIgnoreCase))
                results.Add(new ItemLookup(kv.Key, kv.Value, IdToCategory.GetValueOrDefault(kv.Key, "")));
            if (results.Count >= maxResults) break;
        }
        return results;
    }

    /// <summary>Get all items as lookup records.</summary>
    public static List<ItemLookup> GetAllItems()
    {
        return IdToName.Select(kv => new ItemLookup(kv.Key, kv.Value, IdToCategory.GetValueOrDefault(kv.Key, "")))
                       .OrderBy(x => x.Id)
                       .ToList();
    }

    /// <summary>Try to find an item ID by partial name match.</summary>
    public static int FindIdByPartialName(string name)
    {
        var id = GetId(name);
        if (id >= 0) return id;

        // Try partial match
        var match = IdToName.FirstOrDefault(kv => kv.Value.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match.Key != 0 ? match.Key : -1;
    }

    public static int Count => IdToName.Count;

    /// <summary>Lightweight item lookup record.</summary>
    public readonly record struct ItemLookup(int Id, string Name, string Category)
    {
        public override string ToString() => $"{Name} (ID:{Id})";
    }

    private sealed class JsonItemEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Category { get; set; }
    }
}
