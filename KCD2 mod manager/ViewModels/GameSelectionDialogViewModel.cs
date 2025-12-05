using System;
using System.Windows.Input;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für den Spiel-Auswahl-Dialog (Feature 11)
    /// WICHTIG: Unterstützt vollständige Lokalisierung
    /// </summary>
    public class GameSelectionDialogViewModel : ViewModelBase
    {
        private readonly IGameInstallService _gameInstallService;
        private readonly ILocalizationService _localizationService;
        private GameType _selectedGame;
        
        private string _title = Strings.ResourceManager.GetString("SelectGameTitle") ?? "Select Game";
        private string _selectGameMessage = Strings.ResourceManager.GetString("SelectGameMessage") ?? "Select installed game:";
        private string _kcd1DisplayName = Strings.ResourceManager.GetString("KCD1DisplayName") ?? "Kingdom Come: Deliverance";
        private string _kcd2DisplayName = Strings.ResourceManager.GetString("KCD2DisplayName") ?? "Kingdom Come: Deliverance II";
        private string _okButtonText = Strings.ResourceManager.GetString("OkButton") ?? "OK";
        private string _cancelButtonText = Strings.ResourceManager.GetString("CancelButton") ?? "Cancel";

        /// <summary>
        /// DI-kompatibler Constructor
        /// WICHTIG: SelectedGame wird sicher initialisiert, auch wenn noch kein Spiel ausgewählt wurde
        /// </summary>
        public GameSelectionDialogViewModel(IGameInstallService gameInstallService, ILocalizationService localizationService)
        {
            _gameInstallService = gameInstallService ?? throw new ArgumentNullException(nameof(gameInstallService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            
            // Initialisiere SelectedGame sicher - verwende default(GameType) wenn noch nichts gesetzt ist
            // Dies verhindert Exceptions beim Zugriff auf SelectedGame
            var currentSelected = _gameInstallService.SelectedGame;
            if (currentSelected != default(GameType))
            {
                SelectedGame = currentSelected;
            }
            // Ansonsten bleibt SelectedGame auf default(GameType), wird dann im Dialog gesetzt
            
            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        /// <summary>
        /// Aktualisiert alle lokalisierten Strings
        /// </summary>
        private void UpdateLocalizedStrings()
        {
            Title = Strings.ResourceManager.GetString("SelectGameTitle") ?? "Select Game";
            SelectGameMessage = Strings.ResourceManager.GetString("SelectGameMessage") ?? "Select installed game:";
            KCD1DisplayName = Strings.ResourceManager.GetString("KCD1DisplayName") ?? "Kingdom Come: Deliverance";
            KCD2DisplayName = Strings.ResourceManager.GetString("KCD2DisplayName") ?? "Kingdom Come: Deliverance II";
            OkButtonText = Strings.ResourceManager.GetString("OkButton") ?? "OK";
            CancelButtonText = Strings.ResourceManager.GetString("CancelButton") ?? "Cancel";
        }

        public GameType SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SelectGameMessage
        {
            get => _selectGameMessage;
            set => SetProperty(ref _selectGameMessage, value);
        }

        public string KCD1DisplayName
        {
            get => _kcd1DisplayName;
            set => SetProperty(ref _kcd1DisplayName, value);
        }

        public string KCD2DisplayName
        {
            get => _kcd2DisplayName;
            set => SetProperty(ref _kcd2DisplayName, value);
        }

        public string OkButtonText
        {
            get => _okButtonText;
            set => SetProperty(ref _okButtonText, value);
        }

        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        /// <summary>
        /// Prüft, ob KCD1 installiert ist
        /// WICHTIG: Sicherer Zugriff, verhindert Exceptions wenn InstallType noch nicht gesetzt ist
        /// </summary>
        public bool HasKCD1
        {
            get
            {
                try
                {
                    return _gameInstallService.InstallType == GameInstallType.KCD1 || 
                           _gameInstallService.InstallType == GameInstallType.Both;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Prüft, ob KCD2 installiert ist
        /// WICHTIG: Sicherer Zugriff, verhindert Exceptions wenn InstallType noch nicht gesetzt ist
        /// </summary>
        public bool HasKCD2
        {
            get
            {
                try
                {
                    return _gameInstallService.InstallType == GameInstallType.KCD2 || 
                           _gameInstallService.InstallType == GameInstallType.Both;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}

