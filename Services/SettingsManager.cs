using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Persists application settings to %AppData%/TerrariaPlayersEditor/settings.json.
/// Manages language preference and animated icon toggle.
/// </summary>
public static class SettingsManager
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TerrariaPlayersEditor");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    /// <summary>Whether animated item icons should play in SlotPanel and ItemModifier.</summary>
    public static bool EnableAnimatedIcons { get; set; } = true;

    /// <summary>Whether dark mode is active.</summary>
    public static bool DarkMode { get; set; } = false;

    /// <summary>Whether equipment column labels are arranged vertically on the grids' left (true) or horizontally above the grids (false).</summary>
    public static bool VerticalEquipLabels { get; set; } = true;

    /// <summary>Item/buff browser display mode (details rows or large-icon card grid).</summary>
    public static BrowserViewMode BrowserViewMode { get; set; } = BrowserViewMode.Details;

    /// <summary>Large-icon card size in pixels (32, 48, or 64).</summary>
    public static int BrowserIconSize { get; set; } = 48;

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
                    EnableAnimatedIcons = settings.EnableAnimatedIcons;
                    DarkMode = settings.DarkMode;
                    VerticalEquipLabels = settings.VerticalEquipLabels;
                    BrowserViewMode = settings.BrowserViewMode;
                    BrowserIconSize = settings.BrowserIconSize;
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
            var settings = new SettingsData
            {
                Language = AppLocale.Current,
                EnableAnimatedIcons = EnableAnimatedIcons,
                DarkMode = DarkMode,
                VerticalEquipLabels = VerticalEquipLabels,
                BrowserViewMode = BrowserViewMode,
                BrowserIconSize = BrowserIconSize
            };
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
        public bool EnableAnimatedIcons { get; set; } = true;
        public bool DarkMode { get; set; } = false;
        public bool VerticalEquipLabels { get; set; } = true;
        public BrowserViewMode BrowserViewMode { get; set; } = BrowserViewMode.Details;
        public int BrowserIconSize { get; set; } = 48;
    }
}

/// <summary>Display mode for the item/buff browser.</summary>
public enum BrowserViewMode
{
    Details,
    LargeIcons
}
