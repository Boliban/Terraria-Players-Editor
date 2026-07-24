using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text.Json;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Loads and caches item and buff icons from embedded base64-encoded PNG data.
/// Handles contain-scaling, animated sprite sheets, and multi-form food cropping.
/// </summary>
public static class IconService
{
    // Static icons (single frame or first frame of animated/food items)
    private static readonly Dictionary<int, Bitmap> ItemIcons = new();
    private static readonly Dictionary<int, Bitmap> BuffIcons = new();
    // Animated frames for multi-frame items
    private static readonly Dictionary<int, Bitmap[]> ItemFrames = new();
    private static Bitmap? _defaultIcon;
    private static bool _loaded;

    // Animated item IDs: vertical sprite sheets with equal-size frames
    private static readonly HashSet<int> AnimatedItemIds = new()
    {
        75, 520, 521, 547, 548, 549, 575,
        3453, 3454, 3455, 3580, 3581,
        4068, 4069, 4070, 5644
    };

    // Multi-form food IDs: vertical strips with 3 consumption states of different heights.
    // Only the topmost frame (standard item) should be shown.
    private static readonly HashSet<int> FoodItemIds = new()
    {
        353, 357,
        1787, 1911, 1912, 1919, 1920,
        2266, 2267, 2268, 2425, 2426, 2427,
        3195, 3532,
        // 4009..4037
        4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018,
        4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028,
        4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037,
        // 4282..4297
        4282, 4283, 4284, 4285, 4286, 4287, 4288, 4289, 4290,
        4291, 4292, 4293, 4294, 4295, 4296, 4297,
        4403, 4411,
        // 4614..4625
        4614, 4615, 4616, 4617, 4618, 4619, 4620, 4621, 4622,
        4623, 4624, 4625,
        5009, 5041, 5042, 5092, 5093,
        5275, 5277, 5278,
        5537, 5645
    };

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

    /// <summary>Get animation frames for an item, or null if not animated.</summary>
    public static Bitmap[]? GetItemFrames(int itemId)
    {
        EnsureLoaded();
        var has = ItemFrames.TryGetValue(itemId, out var frames);
        if (has)
            DebugLog.Log($"[IconService] GetItemFrames({itemId}) -> {frames!.Length} frames");
        return has ? frames : null;
    }

    /// <summary>Whether an item has animation frames.</summary>
    public static bool IsAnimated(int itemId)
    {
        return ItemFrames.ContainsKey(itemId);
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

            LoadDict(assembly, "Terraria_Players_Editor.Data.icons_items.json",
                ItemIcons, ItemFrames, isBuff: false);
            LoadDict(assembly, "Terraria_Players_Editor.Data.icons_buffs.json",
                BuffIcons, new Dictionary<int, Bitmap[]>(), isBuff: true);

            _loaded = true;
            DebugLog.Log(
                $"IconService loaded: {ItemIcons.Count} items ({ItemFrames.Count} animated), {BuffIcons.Count} buffs");
            DebugLog.Log(
                $"[IconService] EnableAnimatedIcons={SettingsManager.EnableAnimatedIcons}");
        }
        catch (Exception ex)
        {
            DebugLog.Log($"IconService load failed: {ex.Message}");
        }
    }

    private static void LoadDict(Assembly assembly, string resourceName,
        Dictionary<int, Bitmap> staticTarget, Dictionary<int, Bitmap[]> framesTarget,
        bool isBuff)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            DebugLog.Log($"Icon resource not found: {resourceName}");
            return;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict == null) return;

        foreach (var kv in dict)
        {
            if (!int.TryParse(kv.Key, out var id) || string.IsNullOrEmpty(kv.Value))
                continue;

            try
            {
                var bytes = Convert.FromBase64String(kv.Value);
                using var ms = new MemoryStream(bytes);
                using var bmp = new Bitmap(ms);

                // Convert to 32bpp ARGB if needed — GetPixel fails on indexed formats
                Bitmap bmp32;
                if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    bmp32 = new Bitmap(bmp);
                }
                else
                {
                    bmp32 = new Bitmap(bmp.Width, bmp.Height,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using var g = Graphics.FromImage(bmp32);
                    g.DrawImage(bmp, 0, 0);
                }

                if (isBuff)
                {
                    // Buffs: simple contain-scale to 32x32
                    var processed = ScaleContain(bmp32, 32, 32);
                    staticTarget[id] = processed;
                }
                else if (AnimatedItemIds.Contains(id))
                {
                    // Animated: extract frames, store first frame as static
                    var frames = ExtractFrames(bmp32);
                    DebugLog.Log(
                        $"[IconService] Animated ID {id}: {bmp32.Width}x{bmp32.Height}, PF={bmp32.PixelFormat}, frames={frames.Length}");
                    if (frames.Length > 1)
                    {
                        framesTarget[id] = frames;
                        staticTarget[id] = ScaleContain(frames[0], 32, 32);
                        DebugLog.Log(
                            $"[IconService]   -> Stored {frames.Length} frames, first frame {frames[0].Width}x{frames[0].Height}");
                    }
                    else
                    {
                        staticTarget[id] = ScaleContain(bmp32, 32, 32);
                        DebugLog.Log(
                            $"[IconService]   -> Only {frames.Length} frame(s), storing as static");
                    }
                }
                else if (FoodItemIds.Contains(id))
                {
                    // Food: crop to first frame (top portion), then contain-scale
                    var firstFrame = CropFirstFrame(bmp32);
                    staticTarget[id] = ScaleContain(firstFrame, 32, 32);
                    firstFrame.Dispose();
                }
                else
                {
                    // Normal item: contain-scale to 32x32
                    staticTarget[id] = ScaleContain(bmp32, 32, 32);
                }

                if (bmp32 != bmp) bmp32.Dispose();
            }
            catch
            {
                // Skip malformed entries
            }
        }
    }

    /// <summary>
    /// Scale image to fit within targetW×targetH, maintaining aspect ratio.
    /// Centered with transparent padding. Uses NearestNeighbor for crisp pixel art.
    /// </summary>
    private static Bitmap ScaleContain(Bitmap src, int targetW, int targetH)
    {
        if (src.Width == targetW && src.Height == targetH)
            return new Bitmap(src); // Exact match, just clone

        double scale = Math.Min((double)targetW / src.Width, (double)targetH / src.Height);
        int newW = Math.Max(1, (int)Math.Round(src.Width * scale));
        int newH = Math.Max(1, (int)Math.Round(src.Height * scale));
        int x = (targetW - newW) / 2;
        int y = (targetH - newH) / 2;

        var result = new Bitmap(targetW, targetH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(result);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(src, x, y, newW, newH);
        return result;
    }

    /// <summary>
    /// Extract equal-size frames from a vertical sprite sheet.
    /// Uses transparency scanning to find real frame boundaries,
    /// then extracts only content regions (ignoring transparent gaps).
    /// </summary>
    private static Bitmap[] ExtractFrames(Bitmap src)
    {
        int frameW = src.Width;
        if (frameW <= 0) return Array.Empty<Bitmap>();

        // Detect frame boundaries by scanning for transparent rows
        var boundaries = FindFrameBoundaries(src);

        // boundaries alternates: [content_start, gap_start, content_start, gap_start, ...]
        // Pair them up to extract content regions only
        var contentRegions = new List<(int top, int bottom)>();
        for (int i = 0; i < boundaries.Count - 1; i += 2)
        {
            int top = boundaries[i];
            int bottom = boundaries[i + 1];
            if (bottom - top > 0)
                contentRegions.Add((top, bottom));
        }

        if (contentRegions.Count >= 2)
        {
            var frames = new Bitmap[contentRegions.Count];
            for (int i = 0; i < contentRegions.Count; i++)
            {
                var (top, bottom) = contentRegions[i];
                int h = bottom - top;
                var frame = new Bitmap(frameW, h,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(frame);
                g.DrawImage(src,
                    new Rectangle(0, 0, frameW, h),
                    new Rectangle(0, top, frameW, h),
                    GraphicsUnit.Pixel);
                frames[i] = frame;
            }
            return frames;
        }

        // Fallback: divide by frame width evenly
        int frameCount = src.Height / Math.Max(1, frameW);
        if (frameCount >= 2)
        {
            var frames = new Bitmap[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int y = i * frameW;
                int h = Math.Min(frameW, src.Height - y);
                var frame = new Bitmap(frameW, h,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(frame);
                g.DrawImage(src,
                    new Rectangle(0, 0, frameW, h),
                    new Rectangle(0, y, frameW, h),
                    GraphicsUnit.Pixel);
                frames[i] = frame;
            }
            return frames;
        }

        return Array.Empty<Bitmap>();
    }

    /// <summary>
    /// Crop to the first visual frame (topmost non-transparent region) for food items.
    /// Food items have 3 consumption states of different heights stacked vertically.
    /// </summary>
    private static Bitmap CropFirstFrame(Bitmap src)
    {
        var boundaries = FindFrameBoundaries(src);
        if (boundaries.Count >= 2)
        {
            int top = boundaries[0];
            int bottom = boundaries[1];
            int frameH = bottom - top;
            if (frameH > 0)
            {
                var frame = new Bitmap(src.Width, frameH,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(frame);
                g.DrawImage(src,
                    new Rectangle(0, 0, src.Width, frameH),
                    new Rectangle(0, top, src.Width, frameH),
                    GraphicsUnit.Pixel);
                return frame;
            }
        }
        // Fallback: assume first third of image is the first frame
        int h = src.Height / 3;
        if (h <= 0) return new Bitmap(src);
        var fallback = new Bitmap(src.Width, h,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g2 = Graphics.FromImage(fallback);
        g2.DrawImage(src,
            new Rectangle(0, 0, src.Width, h),
            new Rectangle(0, 0, src.Width, h),
            GraphicsUnit.Pixel);
        return fallback;
    }

    /// <summary>
    /// Scan the image for frame boundaries by detecting fully-transparent horizontal rows.
    /// Returns Y-positions where content begins/ends.
    /// </summary>
    private static List<int> FindFrameBoundaries(Bitmap src)
    {
        var boundaries = new List<int>();
        bool inContent = false;

        for (int y = 0; y < src.Height; y++)
        {
            bool rowHasContent = false;
            for (int x = 0; x < src.Width; x++)
            {
                if (src.GetPixel(x, y).A > 0)
                {
                    rowHasContent = true;
                    break;
                }
            }

            if (rowHasContent && !inContent)
            {
                boundaries.Add(y);
                inContent = true;
            }
            else if (!rowHasContent && inContent)
            {
                boundaries.Add(y);
                inContent = false;
            }
        }

        if (inContent)
            boundaries.Add(src.Height);

        return boundaries;
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
