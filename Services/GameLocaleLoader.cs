using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads game content translations from embedded locale JSON files.
/// Provides Chinese translations for items, prefixes, NPCs, buffs, and projectiles.
/// </summary>
public static class GameLocaleLoader
{
    private static Dictionary<string, string> ItemNames { get; } = new();
    private static Dictionary<string, string> PrefixNames { get; } = new();
    private static Dictionary<string, string> NPCNames { get; } = new();
    private static Dictionary<string, string> BuffNames { get; } = new();
    private static Dictionary<string, string> ProjectileNames { get; } = new();

    private static bool _loaded = false;

    static GameLocaleLoader()
    {
        try
        {
            LoadAll();
            _loaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GameLocaleLoader init failed: {ex.Message}");
        }
    }

    /// <summary>Whether locale data was loaded successfully.</summary>
    public static bool IsLoaded => _loaded;

    /// <summary>Get Chinese item name from internal name (PascalCase English name).</summary>
    public static string? GetItemName(string englishDisplayName)
    {
        var internalName = ToInternalName(englishDisplayName);
        return ItemNames.TryGetValue(internalName, out var zh) ? zh : null;
    }

    /// <summary>Get Chinese item name by direct internal name lookup (preferred when internal name is known).</summary>
    public static string? GetItemNameByInternal(string internalName)
    {
        return ItemNames.TryGetValue(internalName, out var zh) ? zh : null;
    }

    /// <summary>Get Chinese prefix name from English prefix name.</summary>
    public static string? GetPrefixName(string englishName)
    {
        return PrefixNames.TryGetValue(englishName, out var zh) ? zh : null;
    }

    /// <summary>Get Chinese NPC name from internal NPC name.</summary>
    public static string? GetNPCName(string internalName)
    {
        return NPCNames.TryGetValue(internalName, out var zh) ? zh : null;
    }

    /// <summary>Get Chinese buff name from internal buff name.</summary>
    public static string? GetBuffName(string internalName)
    {
        return BuffNames.TryGetValue(internalName, out var zh) ? zh : null;
    }

    /// <summary>Get Chinese projectile name from internal name.</summary>
    public static string? GetProjectileName(string internalName)
    {
        return ProjectileNames.TryGetValue(internalName, out var zh) ? zh : null;
    }

    /// <summary>Convert an English display name to Terraria internal name format (PascalCase, no spaces/punctuation).</summary>
    public static string ToInternalName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "";
        // Remove spaces and common punctuation
        var sb = new System.Text.StringBuilder();
        foreach (char c in displayName)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static void LoadAll()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "Terraria_Players_Editor.Data.Locale.";

        LoadDict(assembly, prefix + "items-zh.json", ItemNames);
        LoadDict(assembly, prefix + "prefixes-zh.json", PrefixNames);
        LoadDict(assembly, prefix + "npcs-zh.json", NPCNames);
        LoadDict(assembly, prefix + "buffs-zh.json", BuffNames);
        LoadDict(assembly, prefix + "projectiles-zh.json", ProjectileNames);
    }

    private static void LoadDict(Assembly assembly, string resourceName, Dictionary<string, string> target)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine($"Resource not found: {resourceName}");
            return;
        }
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict != null)
        {
            foreach (var kv in dict)
                target[kv.Key] = kv.Value;
        }
    }
}
