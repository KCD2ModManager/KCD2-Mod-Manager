using System.Globalization;
using System.Resources;
using System.Reflection;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für Lokalisierung - verwaltet die aktuelle Sprache
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly IAppSettings _settings;
        private CultureInfo _currentCulture;

        public event EventHandler? LanguageChanged;

        public LocalizationService(IAppSettings settings)
        {
            _settings = settings;
            string languageCode = _settings.Language ?? "en";
            SetLanguage(languageCode);
        }

        public CultureInfo CurrentCulture => _currentCulture;

        public void SetLanguage(string languageCode)
        {
            try
            {
                _currentCulture = new CultureInfo(languageCode);
                CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
                Thread.CurrentThread.CurrentCulture = _currentCulture;
                Thread.CurrentThread.CurrentUICulture = _currentCulture;

                _settings.Language = languageCode;
                _settings.Save();
                
                // Event auslösen, damit UI aktualisiert werden kann
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Fallback auf Englisch bei ungültiger Sprache
                _currentCulture = new CultureInfo("en");
                CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
                Thread.CurrentThread.CurrentCulture = _currentCulture;
                Thread.CurrentThread.CurrentUICulture = _currentCulture;
            }
        }

        public Dictionary<string, string> GetAvailableLanguages()
        {
            return new Dictionary<string, string>
            {
                { "en", "English" },
                { "de", "Deutsch" },
                { "fr", "Français" }
            };
        }
    }
}

