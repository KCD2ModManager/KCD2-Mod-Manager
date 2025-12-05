using System.Globalization;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Lokalisierungs-Service
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Aktuelle Kultur
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Sprache ändern
        /// </summary>
        void SetLanguage(string languageCode);

        /// <summary>
        /// Verfügbare Sprachen
        /// </summary>
        Dictionary<string, string> GetAvailableLanguages();

        /// <summary>
        /// Event, das ausgelöst wird, wenn die Sprache geändert wird
        /// </summary>
        event EventHandler? LanguageChanged;
    }
}

