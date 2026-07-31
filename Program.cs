using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Load persisted settings before creating UI
            SettingsManager.Load();

            // Set system color mode for native controls (scrollbars, borders, etc.)
            Application.SetColorMode(SettingsManager.DarkMode
                ? SystemColorMode.Dark
                : SystemColorMode.System);

            // Initialize the theme system with saved dark mode preference
            ThemeManager.ApplyTheme(dark: SettingsManager.DarkMode);

            // Auto-save settings when language changes
            AppLocale.LanguageChanged += () => SettingsManager.Save();

            Application.Run(new MainForm());

            // Cleanup theme resources on shutdown
            ThemeManager.Shutdown();
        }
    }
}