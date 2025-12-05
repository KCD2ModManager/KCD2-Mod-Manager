using System.Windows;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Theme-Service zur zentralen Verwaltung von Dark/Light Mode
    /// WICHTIG: Stellt sicher, dass alle Dialoge und Fenster konsistent thematisiert werden
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Aktualisiert die Theme-Ressourcen für ein ResourceDictionary
        /// </summary>
        void ApplyTheme(ResourceDictionary resources, bool isDarkMode);

        /// <summary>
        /// Gibt an, ob Dark Mode aktiv ist
        /// </summary>
        bool IsDarkMode { get; }
    }
}

