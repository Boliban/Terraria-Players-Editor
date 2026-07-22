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
    private static readonly Dictionary<string, int> NameZhToId = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> IdToInternal = new();
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
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonItemEntry>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items == null) return;

            foreach (var item in items)
            {
                IdToName[item.Id] = item.Name;
                NameToId[item.Name] = item.Id;
                IdToInternal[item.Id] = item.Internal ?? "";
                IdToCategory[item.Id] = item.Category ?? "None";
                if (item.Id > MaxId) MaxId = item.Id;
            }

            // Build Chinese name → ID reverse lookup
            BuildZhReverseLookup();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load item database: {ex.Message}");
        }
    }

    /// <summary>Build Chinese name → ID mapping for search support.</summary>
    private static void BuildZhReverseLookup()
    {
        foreach (var kv in IdToName)
        {
            // Get Chinese name via GameLocaleLoader using the internal name
            var internalName = IdToInternal.GetValueOrDefault(kv.Key, "");
            string? zhName = null;
            if (!string.IsNullOrEmpty(internalName))
                zhName = GameLocaleLoader.GetItemNameByInternal(internalName);
            if (zhName == null)
                zhName = GameLocaleLoader.GetItemName(kv.Value);
            if (!string.IsNullOrEmpty(zhName))
                NameZhToId[zhName] = kv.Key;
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
            // Use internal name (from items.json) for precise lookup
            if (IdToInternal.TryGetValue(id, out var internalName) && !string.IsNullOrEmpty(internalName))
            {
                var zhName = GameLocaleLoader.GetItemNameByInternal(internalName);
                if (zhName != null) return zhName;
            }
            // Fallback: convert display name to internal format
            var zhName2 = GameLocaleLoader.GetItemName(enName);
            if (zhName2 != null) return zhName2;
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

    /// <summary>Search items by name substring (supports both EN and ZH). Case-insensitive.</summary>
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

        // Search English names
        foreach (var kv in IdToName)
        {
            if (kv.Value.Contains(q, StringComparison.OrdinalIgnoreCase))
                results.Add(new ItemLookup(kv.Key, kv.Value, IdToCategory.GetValueOrDefault(kv.Key, "")));
            if (results.Count >= maxResults) break;
        }

        // Also search Chinese names if in ZH mode
        if (AppLocale.Current == AppLocale.Lang.ZH && results.Count < maxResults)
        {
            var existingIds = new HashSet<int>(results.Select(r => r.Id));
            foreach (var kv in NameZhToId)
            {
                if (existingIds.Contains(kv.Value)) continue;
                if (kv.Key.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ItemLookup(kv.Value, IdToName.GetValueOrDefault(kv.Value, "Unknown"), IdToCategory.GetValueOrDefault(kv.Value, "")));
                    if (results.Count >= maxResults) break;
                }
            }
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

    /// <summary>Try to find an item ID by exact or partial name match (supports both EN and ZH).</summary>
    public static int FindIdByPartialName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var trimmed = name.Trim();

        // 1. Exact match in English
        if (NameToId.TryGetValue(trimmed, out var id))
            return id;

        // 2. Exact match in Chinese
        if (AppLocale.Current == AppLocale.Lang.ZH && NameZhToId.TryGetValue(trimmed, out var zhId))
            return zhId;

        // 3. Partial match in English
        var matchEn = IdToName.FirstOrDefault(kv => kv.Value.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (matchEn.Key != 0) return matchEn.Key;

        // 4. Partial match in Chinese
        if (AppLocale.Current == AppLocale.Lang.ZH)
        {
            var matchZh = NameZhToId.FirstOrDefault(kv => kv.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (matchZh.Key != null) return matchZh.Value;

            // 5. Contains search in Chinese
            foreach (var kv in NameZhToId)
            {
                if (kv.Key.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
        }

        // 6. Contains search in English
        foreach (var kv in IdToName)
        {
            if (kv.Value.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }

        return -1;
    }

    public static int Count => IdToName.Count;

    /// <summary>Lightweight item lookup record.</summary>
    public readonly record struct ItemLookup(int Id, string Name, string Category)
    {
        public override string ToString() => $"{ItemDatabase.GetName(Id)} (ID:{Id})";
    }

    private sealed class JsonItemEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Internal { get; set; }
        public string? Category { get; set; }
    }
}
