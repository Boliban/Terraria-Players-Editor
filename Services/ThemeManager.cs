namespace Terraria_Players_Editor.Services;

/// <summary>
/// Centralized theme system providing colors, typography, and spacing for
/// the Win11-style flat design. Supports light and dark modes.
/// All previously hardcoded Color/Font values across the project are consolidated here.
/// </summary>
public static class ThemeManager
{
    private static bool _darkMode;

    // ── Cached brushes (disposed and recreated on theme change) ──
    private static readonly Dictionary<Color, SolidBrush> _brushCache = new();
    private static readonly Dictionary<Color, Pen> _penCache = new();

    // ── Surface Colors ──
    public static Color SurfaceBackground { get; private set; }
    public static Color SurfaceCard { get; private set; }
    public static Color SurfaceContainer { get; private set; }
    public static Color SurfaceRaised { get; private set; }

    // ── Text Colors ──
    public static Color TextPrimary { get; private set; }
    public static Color TextSecondary { get; private set; }
    public static Color TextOnAccent { get; private set; }
    public static Color TextDisabled { get; private set; }

    // ── Accent Colors (Win11 blue) ──
    public static Color AccentPrimary { get; private set; }
    public static Color AccentHover { get; private set; }
    public static Color AccentPressed { get; private set; }
    public static Color AccentSelection { get; private set; }

    // ── Slot Colors ──
    public static Color SlotNormal { get; private set; }
    public static Color SlotEmpty { get; private set; }
    public static Color SlotHotbar { get; private set; }
    public static Color SlotHover { get; private set; }
    public static Color SlotSelectedFill { get; private set; }
    public static Color SlotSelectedBorder { get; private set; }
    public static Color SlotBorder { get; private set; }

    // ── Control Colors ──
    public static Color ControlInputBg { get; private set; }
    public static Color ControlInputBorder { get; private set; }
    public static Color ControlButtonBg { get; private set; }
    public static Color ControlButtonHover { get; private set; }
    public static Color ControlButtonPressed { get; private set; }

    // ── Icon Modifier Dark Background ──
    public static Color IconModifierBg { get; private set; }

    // ── Typography (cached Font objects — never recreated) ──
    public static class Typography
    {
        public static readonly Font Body = new("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font BodyBold = new("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font Caption = new("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font SlotStack = new("Segoe UI", 6.5f, FontStyle.Bold);
        public static readonly Font SlotStackSmall = new("Segoe UI", 5.5f, FontStyle.Bold);

        /// <summary>Dispose cached fonts. Call on application shutdown.</summary>
        public static void Dispose()
        {
            Body.Dispose();
            BodyBold.Dispose();
            Caption.Dispose();
            SlotStack.Dispose();
            SlotStackSmall.Dispose();
        }
    }

    // ── Spacing ──
    public static class Spacing
    {
        public const int MarginStandard = 6;
        public const int PaddingStandard = 10;
        public const int PaddingPage = 20;
        public const int CornerRadius = 8;
        public const int SlotSize = 48;
        public const int SlotCellSize = 50;
        public const int BorderWidth = 1;
    }

    // ── Events ──
    /// <summary>Fired when the theme changes (light/dark toggle).</summary>
    public static event Action? ThemeChanged;

    /// <summary>Whether dark mode is active.</summary>
    public static bool IsDarkMode => _darkMode;

    /// <summary>Apply the theme.</summary>
    public static void ApplyTheme(bool dark)
    {
        _darkMode = dark;
        DisposeBrushesAndPens();

        if (dark)
        {
            // ── Dark Theme (Win11 dark palette) ──
            SurfaceBackground = Color.FromArgb(32, 32, 32);
            SurfaceCard = Color.FromArgb(40, 40, 40);
            SurfaceContainer = Color.FromArgb(32, 32, 32);
            SurfaceRaised = Color.FromArgb(50, 50, 50);

            TextPrimary = Color.FromArgb(250, 250, 250);
            TextSecondary = Color.FromArgb(165, 165, 165);
            TextOnAccent = Color.White;
            TextDisabled = Color.FromArgb(100, 100, 100);

            AccentPrimary = Color.FromArgb(96, 155, 230);     // Win11 light accent on dark
            AccentHover = Color.FromArgb(120, 175, 245);
            AccentPressed = Color.FromArgb(70, 130, 210);
            AccentSelection = Color.FromArgb(96, 155, 230);

            SlotNormal = Color.FromArgb(55, 55, 60);
            SlotEmpty = Color.FromArgb(48, 48, 52);
            SlotHotbar = Color.FromArgb(62, 68, 78);
            SlotHover = Color.FromArgb(72, 78, 88);
            SlotSelectedFill = Color.FromArgb(80, 70, 30);
            SlotSelectedBorder = Color.FromArgb(255, 200, 60);
            SlotBorder = Color.FromArgb(80, 80, 85);

            ControlInputBg = Color.FromArgb(45, 45, 48);
            ControlInputBorder = Color.FromArgb(100, 100, 105);
            ControlButtonBg = Color.FromArgb(60, 60, 65);
            ControlButtonHover = Color.FromArgb(75, 75, 80);
            ControlButtonPressed = Color.FromArgb(50, 50, 55);

            IconModifierBg = Color.FromArgb(40, 35, 45);
        }
        else
        {
            // ── Light Theme (Win11 light palette) ──
            SurfaceBackground = Color.FromArgb(243, 243, 243);
            SurfaceCard = Color.FromArgb(249, 249, 249);
            SurfaceContainer = Color.FromArgb(243, 243, 243);
            SurfaceRaised = Color.FromArgb(255, 255, 255);

            TextPrimary = Color.FromArgb(26, 26, 26);
            TextSecondary = Color.FromArgb(97, 97, 97);
            TextOnAccent = Color.White;
            TextDisabled = Color.FromArgb(180, 180, 180);

            AccentPrimary = Color.FromArgb(0, 120, 212);       // Win11 accent blue
            AccentHover = Color.FromArgb(20, 140, 230);
            AccentPressed = Color.FromArgb(0, 100, 190);
            AccentSelection = Color.FromArgb(0, 120, 212);

            SlotNormal = Color.FromArgb(210, 208, 215);
            SlotEmpty = Color.FromArgb(210, 208, 215);
            SlotHotbar = Color.FromArgb(175, 190, 210);
            SlotHover = Color.FromArgb(185, 205, 225);
            SlotSelectedFill = Color.FromArgb(255, 220, 100);
            SlotSelectedBorder = Color.FromArgb(200, 150, 20);
            SlotBorder = Color.FromArgb(175, 175, 180);

            ControlInputBg = Color.White;
            ControlInputBorder = Color.FromArgb(209, 209, 209);
            ControlButtonBg = Color.FromArgb(240, 240, 240);
            ControlButtonHover = Color.FromArgb(225, 235, 248);
            ControlButtonPressed = Color.FromArgb(210, 222, 240);

            IconModifierBg = Color.FromArgb(40, 35, 45);
        }

        ThemeChanged?.Invoke();
    }

    // ── Brush/Pen cache ──
    public static SolidBrush GetBrush(Color color)
    {
        if (!_brushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(color);
            _brushCache[color] = brush;
        }
        return brush;
    }

    public static Pen GetPen(Color color, float width = 1f)
    {
        // Pens are not cached by default since width varies; create a simple lookup
        int key = HashCode.Combine(color.ToArgb(), width);
        if (!_penCache.TryGetValue(Color.FromArgb(key), out var pen))
        {
            pen = new Pen(color, width);
            _penCache[Color.FromArgb(key)] = pen;
        }
        return pen;
    }

    private static void DisposeBrushesAndPens()
    {
        foreach (var brush in _brushCache.Values)
            brush.Dispose();
        _brushCache.Clear();
        foreach (var pen in _penCache.Values)
            pen.Dispose();
        _penCache.Clear();
    }

    /// <summary>Dispose all theme resources. Call on application shutdown.</summary>
    public static void Shutdown()
    {
        DisposeBrushesAndPens();
        Typography.Dispose();
    }
}
