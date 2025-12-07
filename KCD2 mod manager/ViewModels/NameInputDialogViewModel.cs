using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für NameInputDialog
    /// WICHTIG: Unterstützt vollständige Lokalisierung und reagiert auf Sprachänderungen
    /// </summary>
    public class NameInputDialogViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private string _prompt = string.Empty;
        private string _title = string.Empty;
        private string _okButtonText = string.Empty;
        private string _defaultValue = string.Empty;
        private bool _isMultiline = false;

        public NameInputDialogViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new System.ArgumentNullException(nameof(localizationService));
            
            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        /// <summary>
        /// Aktualisiert alle lokalisierten Strings
        /// WICHTIG: Verwendet aktuelle Culture für Resource-Lookup
        /// </summary>
        private void UpdateLocalizedStrings()
        {
            // WICHTIG: Nur aktualisieren, wenn Title nicht explizit gesetzt wurde (z.B. durch DialogService)
            // Wenn Title leer ist oder der Standard-Title, lade aus Resources
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            var defaultTitle = Strings.ResourceManager.GetString("NameInputDialogTitle", culture) ?? "Input";
            if (string.IsNullOrEmpty(_title) || _title == defaultTitle || _title == "Input")
            {
                Title = defaultTitle;
            }
            OkButtonText = Strings.ResourceManager.GetString("OkButton", culture) ?? "OK";
        }

        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string OkButtonText
        {
            get => _okButtonText;
            set => SetProperty(ref _okButtonText, value);
        }

        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        public bool IsMultiline
        {
            get => _isMultiline;
            set => SetProperty(ref _isMultiline, value);
        }
    }
}

