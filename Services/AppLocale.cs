using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Application UI string localization. Loads translations from embedded JSON files
/// in Data/Locale/ui-en.json and Data/Locale/ui-zh.json for easy editing.
/// Switch language via <see cref="Current"/> and call RefreshAllUI() on the form.
/// </summary>
public static class AppLocale
{
    public enum Lang { EN, ZH }

    private static readonly Dictionary<string, string> EnStrings = new();
    private static readonly Dictionary<string, string> ZhStrings = new();
    private static bool _loaded;

    public static Lang Current { get; set; } = Lang.EN;

    /// <summary>Event raised when language changes, so forms can refresh their text.</summary>
    public static event Action? LanguageChanged;

    static AppLocale()
    {
        LoadResources();
    }

    public static void SetLanguage(Lang lang)
    {
        if (Current != lang)
        {
            Current = lang;
            LanguageChanged?.Invoke();
        }
    }

    /// <summary>Get the localized string for the given key. Falls back to the key itself if not found.</summary>
    public static string Get(string key)
    {
        var dict = Current == Lang.ZH ? ZhStrings : EnStrings;
        return dict.TryGetValue(key, out var value) ? value : key;
    }

    /// <summary>Reload all locale data from embedded resources.</summary>
    public static void Reload()
    {
        EnStrings.Clear();
        ZhStrings.Clear();
        LoadResources();
        if (_loaded)
            LanguageChanged?.Invoke();
    }

    private static void LoadResources()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            LoadDict(assembly, "Terraria_Players_Editor.Data.Locale.ui-en.json", EnStrings);
            LoadDict(assembly, "Terraria_Players_Editor.Data.Locale.ui-zh.json", ZhStrings);
            _loaded = EnStrings.Count > 0 || ZhStrings.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppLocale load failed: {ex.Message}");
        }
    }

    private static void LoadDict(Assembly assembly, string resourceName, Dictionary<string, string> target)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine($"AppLocale resource not found: {resourceName}");
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
