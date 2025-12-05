using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für DeleteConfirmationWindow
    /// WICHTIG: Unterstützt vollständige Lokalisierung und reagiert auf Sprachänderungen
    /// </summary>
    public class DeleteConfirmationDialogViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private string _title = string.Empty;
        private string _message = string.Empty;
        private string _dontAskAgainText = string.Empty;
        private string _yesButtonText = string.Empty;
        private string _noButtonText = string.Empty;

        public DeleteConfirmationDialogViewModel(ILocalizationService localizationService)
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
            // WICHTIG: Verwende aktuelle Culture für Resource-Lookup
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            Title = Strings.ResourceManager.GetString("DeleteModTitle", culture) ?? "Delete Mod";
            Message = Strings.ResourceManager.GetString("DeleteModMessage", culture) ?? "Are you sure you want to delete this mod?";
            DontAskAgainText = Strings.ResourceManager.GetString("DontAskAgain", culture) ?? "Don't ask again";
            YesButtonText = Strings.ResourceManager.GetString("YesButton", culture) ?? "Yes";
            NoButtonText = Strings.ResourceManager.GetString("NoButton", culture) ?? "No";
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string DontAskAgainText
        {
            get => _dontAskAgainText;
            set => SetProperty(ref _dontAskAgainText, value);
        }

        public string YesButtonText
        {
            get => _yesButtonText;
            set => SetProperty(ref _yesButtonText, value);
        }

        public string NoButtonText
        {
            get => _noButtonText;
            set => SetProperty(ref _noButtonText, value);
        }
    }
}

