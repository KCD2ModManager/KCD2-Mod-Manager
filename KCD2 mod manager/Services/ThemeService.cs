using System.Windows;
using System.Windows.Media;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Zentraler Theme-Service für Dark/Light Mode
    /// WICHTIG: Lädt Theme-Dictionaries dynamisch basierend auf Dark/Light Mode
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly IAppSettings _settings;

        public ThemeService(IAppSettings settings)
        {
            _settings = settings;
        }

        public bool IsDarkMode => _settings.IsDarkMode;

        /// <summary>
        /// Aktualisiert die Theme-Ressourcen für ein ResourceDictionary
        /// WICHTIG: Lädt Theme-Dictionaries aus Themes/Theme.Light.xaml oder Themes/Theme.Dark.xaml
        /// </summary>
        public void ApplyTheme(ResourceDictionary resources, bool isDarkMode)
        {
            // Entferne alte Theme-Dictionaries
            var dictionariesToRemove = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (ResourceDictionary dict in resources.MergedDictionaries)
            {
                if (dict.Source != null && 
                    (dict.Source.OriginalString.Contains("Theme.Light.xaml") || 
                     dict.Source.OriginalString.Contains("Theme.Dark.xaml")))
                {
                    dictionariesToRemove.Add(dict);
                }
            }
            foreach (var dict in dictionariesToRemove)
            {
                resources.MergedDictionaries.Remove(dict);
            }

            // Lade neues Theme-Dictionary
            var themeUri = new System.Uri(
                isDarkMode 
                    ? "pack://application:,,,/Themes/Theme.Dark.xaml"
                    : "pack://application:,,,/Themes/Theme.Light.xaml",
                System.UriKind.Absolute);
            
            var themeDict = new ResourceDictionary { Source = themeUri };
            resources.MergedDictionaries.Add(themeDict);
        }

        /// <summary>
        /// Event, das ausgelöst wird, wenn sich das Theme ändert
        /// </summary>
        public event System.EventHandler? ThemeChanged;
    }
}

