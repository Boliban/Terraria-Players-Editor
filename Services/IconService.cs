using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads and caches item and buff icons from embedded base64-encoded PNG data.
/// Icons originate from WinTerrEdit's itemIDs.txt and buffIDs.txt,
/// processed into icons_items.json and icons_buffs.json by extract_icons.py.
/// </summary>
public static class IconService
{
    private static readonly Dictionary<int, Bitmap> ItemIcons = new();
    private static readonly Dictionary<int, Bitmap> BuffIcons = new();
    private static Bitmap? _defaultIcon;
    private static bool _loaded;

    static IconService()
    {
        LoadIcons();
    }

    /// <summary>Get the icon for an item ID, or the default placeholder if not found.</summary>
    public static Bitmap? GetItemIcon(int itemId)
    {
        EnsureLoaded();
        return ItemIcons.TryGetValue(itemId, out var bmp) ? bmp : _defaultIcon;
    }

    /// <summary>Get the icon for a buff ID, or the default placeholder if not found.</summary>
    public static Bitmap? GetBuffIcon(int buffId)
    {
        EnsureLoaded();
        return BuffIcons.TryGetValue(buffId, out var bmp) ? bmp : _defaultIcon;
    }

    /// <summary>Default 32×32 transparent placeholder icon.</summary>
    public static Bitmap DefaultIcon => _defaultIcon ?? CreateDefaultIcon();

    private static void EnsureLoaded()
    {
        if (!_loaded) LoadIcons();
    }

    private static void LoadIcons()
    {
        try
        {
            _defaultIcon = CreateDefaultIcon();
            var assembly = Assembly.GetExecutingAssembly();

            LoadDict(assembly, "Terraria_Players_Editor.Data.icons_items.json", ItemIcons);
            LoadDict(assembly, "Terraria_Players_Editor.Data.icons_buffs.json", BuffIcons);

            _loaded = true;
            System.Diagnostics.Debug.WriteLine(
                $"IconService loaded: {ItemIcons.Count} items, {BuffIcons.Count} buffs");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IconService load failed: {ex.Message}");
        }
    }

    private static void LoadDict(Assembly assembly, string resourceName, Dictionary<int, Bitmap> target)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine($"Icon resource not found: {resourceName}");
            return;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict == null) return;

        foreach (var kv in dict)
        {
            if (int.TryParse(kv.Key, out var id) && !string.IsNullOrEmpty(kv.Value))
            {
                try
                {
                    var bytes = Convert.FromBase64String(kv.Value);
                    using var ms = new MemoryStream(bytes);
                    var bmp = new Bitmap(ms);
                    // Validate and rescale non-32x32 icons
                    if (bmp.Width != 32 || bmp.Height != 32)
                    {
                        var resized = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using var g = Graphics.FromImage(resized);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(bmp, 0, 0, 32, 32);
                        bmp.Dispose();
                        bmp = resized;
                    }
                    target[id] = bmp;
                }
                catch
                {
                    // Skip malformed entries
                }
            }
        }
    }

    private static Bitmap CreateDefaultIcon()
    {
        var bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.Gray, 1);
        g.DrawRectangle(pen, 1, 1, 29, 29);
        return bmp;
    }
}
