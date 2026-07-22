namespace Terraria_Players_Editor.Services;

/// <summary>
/// Maps internal PLR file version numbers to human-readable Terraria game version strings.
/// </summary>
public static class VersionMapper
{
    private static readonly (int MinVersion, string Label)[] Map =
    [
        (319, "1.4.5.6"),
        (270, "1.4.5"),
        (269, "1.4.4"),
        (230, "1.4.3"),
        (220, "1.4.1"),
        (210, "1.4.0"),
    ];

    /// <summary>Get the game version string for a given file version number.</summary>
    public static string GetGameVersion(int fileVersion)
    {
        foreach (var (minVer, label) in Map)
            if (fileVersion >= minVer) return label;
        return $"v{fileVersion}";
    }

    /// <summary>Get a combined display string like "1.4.5.6 (v319)".</summary>
    public static string GetDisplayString(int fileVersion)
    {
        return $"{GetGameVersion(fileVersion)}";
    }
}
