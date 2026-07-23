using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Simple debug logger for diagnosing PLR file read/write issues.
/// Writes hex dumps and parse info to debug_plr.log in the app directory.
/// </summary>
public static class DebugLog
{
    private static bool _enabled;
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_plr.log");

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public static void Log(string message)
    {
        if (!_enabled) return;
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { /* ignore */ }
    }

    public static void LogHex(string label, byte[] data, int maxLen = 200)
    {
        if (!_enabled) return;
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] {label} ({data.Length} bytes):");
        int len = Math.Min(data.Length, maxLen);
        for (int i = 0; i < len; i += 16)
        {
            sb.Append($"  {i:X4}: ");
            for (int j = 0; j < 16 && i + j < len; j++)
                sb.Append($"{data[i + j]:X2} ");
            sb.Append("  ");
            for (int j = 0; j < 16 && i + j < len; j++)
            {
                byte b = data[i + j];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            sb.AppendLine();
        }
        if (data.Length > maxLen)
            sb.AppendLine($"  ... ({data.Length - maxLen} more bytes)");
        try { File.AppendAllText(LogPath, sb.ToString()); } catch { }
    }

    public static void Clear()
    {
        try { File.WriteAllText(LogPath, ""); } catch { }
    }
}
