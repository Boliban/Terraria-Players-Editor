using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Persists application settings to %AppData%/TerrariaPlayersEditor/settings.json.
/// Currently manages language preference.
/// </summary>
public static class SettingsManager
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TerrariaPlayersEditor");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    /// <summary>Load saved settings and apply language. Call once at startup before creating UI.</summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    AppLocale.SetLanguage(settings.Language);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    /// <summary>Persist current settings to disk.</summary>
    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var settings = new SettingsData { Language = AppLocale.Current };
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private sealed class SettingsData
    {
        public AppLocale.Lang Language { get; set; }
    }
}
