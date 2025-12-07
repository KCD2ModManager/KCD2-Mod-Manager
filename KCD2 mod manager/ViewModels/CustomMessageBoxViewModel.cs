using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;
using System.Windows;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für CustomMessageBoxWindow
    /// WICHTIG: Unterstützt vollständige Lokalisierung und reagiert auf Sprachänderungen
    /// </summary>
    public class CustomMessageBoxViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private string _title = string.Empty;
        private string _message = string.Empty;
        private string _okButtonText = string.Empty;
        private string _yesButtonText = string.Empty;
        private string _noButtonText = string.Empty;
        private string _cancelButtonText = string.Empty;
        private MessageBoxButton _buttons = MessageBoxButton.OK;
        private MessageBoxImage _icon = MessageBoxImage.None;
        private bool _showOkButton = true;
        private bool _showYesButton = false;
        private bool _showNoButton = false;
        private bool _showCancelButton = false;

        public CustomMessageBoxViewModel(ILocalizationService localizationService)
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
            OKButtonText = Strings.ResourceManager.GetString("OKButton", culture) ?? "OK";
            YesButtonText = Strings.ResourceManager.GetString("YesButton", culture) ?? "Yes";
            NoButtonText = Strings.ResourceManager.GetString("NoButton", culture) ?? "No";
            CancelButtonText = Strings.ResourceManager.GetString("CancelButton", culture) ?? "Cancel";
        }

        public void SetContent(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            Message = message;
            Title = title;
            Buttons = buttons;
            Icon = icon;
            
            // Button-Sichtbarkeit basierend auf MessageBoxButton
            ShowOKButton = buttons == MessageBoxButton.OK || buttons == MessageBoxButton.OKCancel;
            ShowYesButton = buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel;
            ShowNoButton = buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel;
            ShowCancelButton = buttons == MessageBoxButton.OKCancel || buttons == MessageBoxButton.YesNoCancel;
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

        public MessageBoxButton Buttons
        {
            get => _buttons;
            set => SetProperty(ref _buttons, value);
        }

        public MessageBoxImage Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public string OKButtonText
        {
            get => _okButtonText;
            set => SetProperty(ref _okButtonText, value);
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

        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        public bool ShowOKButton
        {
            get => _showOkButton;
            set => SetProperty(ref _showOkButton, value);
        }

        public bool ShowYesButton
        {
            get => _showYesButton;
            set => SetProperty(ref _showYesButton, value);
        }

        public bool ShowNoButton
        {
            get => _showNoButton;
            set => SetProperty(ref _showNoButton, value);
        }

        public bool ShowCancelButton
        {
            get => _showCancelButton;
            set => SetProperty(ref _showCancelButton, value);
        }
    }
}

