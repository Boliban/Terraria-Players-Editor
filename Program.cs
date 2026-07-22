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

            // Load persisted settings (language) before creating UI
            SettingsManager.Load();

            // Auto-save settings when language changes
            AppLocale.LanguageChanged += () => SettingsManager.Save();

            Application.Run(new MainForm());
        }
    }
}