using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für den Spiel-Auswahl-Dialog (Feature 11)
    /// WICHTIG: Unterstützt vollständige Lokalisierung und Folder-Auswahl
    /// </summary>
    public class GameSelectionDialogViewModel : ViewModelBase
    {
        private readonly IGameInstallService _gameInstallService;
        private readonly ILocalizationService _localizationService;
        private readonly IDialogService _dialogService;
        private readonly IAppSettings _settings;
        private readonly ILog _logger;
        private GameType _selectedGame;
        
        private string _title = Strings.ResourceManager.GetString("SelectGameTitle") ?? "Select Game";
        private string _selectGameMessage = Strings.ResourceManager.GetString("SelectGameMessage") ?? "Select installed game:";
        private string _kcd1DisplayName = Strings.ResourceManager.GetString("KCD1DisplayName") ?? "Kingdom Come: Deliverance";
        private string _kcd2DisplayName = Strings.ResourceManager.GetString("KCD2DisplayName") ?? "Kingdom Come: Deliverance II";
        private string _okButtonText = Strings.ResourceManager.GetString("OkButton") ?? "OK";
        private string _cancelButtonText = Strings.ResourceManager.GetString("CancelButton") ?? "Cancel";
        private string _selectFolderButtonText = Strings.ResourceManager.GetString("SelectFolderButton") ?? "Select Folder";
        private string _selectFolderTooltipText = Strings.ResourceManager.GetString("SelectFolderTooltip") ?? "Manually select or change the game installation folder";
        private string _clickCardToSelectText = Strings.ResourceManager.GetString("ClickCardToSelect") ?? "Click on a card to select the game";
        
        // Folder-Status Properties
        private bool _kcd1FolderFound;
        private bool _kcd2FolderFound;
        private string _kcd1FolderStatus = "";
        private string _kcd2FolderStatus = "";
        private string _kcd1FolderPath = "";
        private string _kcd2FolderPath = "";

        /// <summary>
        /// DI-kompatibler Constructor
        /// WICHTIG: SelectedGame wird sicher initialisiert, auch wenn noch kein Spiel ausgewählt wurde
        /// </summary>
        public GameSelectionDialogViewModel(
            IGameInstallService gameInstallService, 
            ILocalizationService localizationService,
            IDialogService dialogService,
            IAppSettings settings,
            ILog logger)
        {
            _gameInstallService = gameInstallService ?? throw new ArgumentNullException(nameof(gameInstallService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialisiere SelectedGame sicher - verwende default(GameType) wenn noch nichts gesetzt ist
            // Dies verhindert Exceptions beim Zugriff auf SelectedGame
            var currentSelected = _gameInstallService.SelectedGame;
            if (currentSelected != default(GameType))
            {
                SelectedGame = currentSelected;
            }
            // Ansonsten bleibt SelectedGame auf default(GameType), wird dann im Dialog gesetzt
            
            // Commands initialisieren
            SelectKCD1FolderCommand = new RelayCommand(async _ => await SelectFolderAsync(GameType.KCD1));
            SelectKCD2FolderCommand = new RelayCommand(async _ => await SelectFolderAsync(GameType.KCD2));
            SelectGameCommand = new RelayCommand(obj => 
            {
                if (obj is GameType gameType)
                {
                    SelectGame(gameType);
                    // Dialog schließen über Event
                    GameSelected?.Invoke(this, gameType);
                }
            });
            
            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
            
            // Initialisiere Folder-Status
            UpdateFolderStatus();
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
            SelectFolderButtonText = Strings.ResourceManager.GetString("SelectFolderButton") ?? "Select Folder";
            SelectFolderTooltipText = Strings.ResourceManager.GetString("SelectFolderTooltip") ?? "Manually select or change the game installation folder";
            ClickCardToSelectText = Strings.ResourceManager.GetString("ClickCardToSelect") ?? "Click on a card to select the game";
            
            // Folder-Status aktualisieren
            UpdateFolderStatus();
        }
        
        /// <summary>
        /// Aktualisiert den Folder-Status für beide Spiele
        /// </summary>
        private void UpdateFolderStatus()
        {
            // KCD1 Status
            string kcd1Path = _settings.GamePath_KCD1;
            KCD1FolderFound = !string.IsNullOrEmpty(kcd1Path) && 
                            (_gameInstallService.KCD1Install != null || System.IO.Directory.Exists(kcd1Path));
            KCD1FolderPath = kcd1Path;
            KCD1FolderStatus = KCD1FolderFound 
                ? (Strings.ResourceManager.GetString("GameFolderFound") ?? "Game folder found")
                : (Strings.ResourceManager.GetString("GameFolderNotFound") ?? "Game folder not found");
            
            // KCD2 Status
            string kcd2Path = _settings.GamePath_KCD2;
            KCD2FolderFound = !string.IsNullOrEmpty(kcd2Path) && 
                            (_gameInstallService.KCD2Install != null || System.IO.Directory.Exists(kcd2Path));
            KCD2FolderPath = kcd2Path;
            KCD2FolderStatus = KCD2FolderFound 
                ? (Strings.ResourceManager.GetString("GameFolderFound") ?? "Game folder found")
                : (Strings.ResourceManager.GetString("GameFolderNotFound") ?? "Game folder not found");
        }
        
        /// <summary>
        /// Wählt einen Folder für ein Spiel aus
        /// </summary>
        private async Task SelectFolderAsync(GameType gameType)
        {
            try
            {
                string gameName = gameType == GameType.KCD1 ? KCD1DisplayName : KCD2DisplayName;
                string description = string.Format(
                    Strings.ResourceManager.GetString("SelectGameFolderPrompt") ?? 
                    "Please select the installation folder for {0}.\n\nThe folder should contain the file 'KingdomCome.exe'.",
                    gameName);
                
                string? selectedPath = _dialogService.ShowFolderPicker(description);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Validiere den Pfad
                    bool isValid = await ValidateGameFolderAsync(gameType, selectedPath);
                    if (isValid)
                    {
                        // Speichere den Pfad
                        if (gameType == GameType.KCD1)
                            _settings.GamePath_KCD1 = selectedPath;
                        else
                            _settings.GamePath_KCD2 = selectedPath;
                        _settings.Save();
                        
                        _logger.Info($"Game folder selected for {gameType}: {selectedPath}");
                        
                        // Status aktualisieren
                        UpdateFolderStatus();
                        
                        // Info-Dialog
                        _dialogService.ShowMessageBox(
                            string.Format(Strings.ResourceManager.GetString("GameFolderSaved") ?? "Path saved: {0}", selectedPath),
                            Strings.ResourceManager.GetString("DialogTitleInformation") ?? "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        _dialogService.ShowMessageBox(
                            Strings.ResourceManager.GetString("ErrorInvalidPath") ?? "The selected folder does not contain a valid game installation.",
                            Strings.ResourceManager.GetString("DialogTitleError") ?? "Invalid Path",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error selecting folder for {gameType}: {ex.Message}", ex);
                _dialogService.ShowMessageBox(
                    string.Format(Strings.ResourceManager.GetString("ErrorSelectingFolder") ?? "Error selecting folder: {0}", ex.Message),
                    Strings.ResourceManager.GetString("DialogTitleError") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Validiert einen Game-Folder
        /// </summary>
        private async Task<bool> ValidateGameFolderAsync(GameType gameType, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
                    return false;
                
                // Prüfe ob Executable existiert
                string exeName = "KingdomCome.exe";
                string[] possiblePaths = gameType == GameType.KCD1
                    ? new[] { System.IO.Path.Combine(path, "Bin", "Win64", exeName) }
                    : new[]
                    {
                        System.IO.Path.Combine(path, "Bin", "Win64MasterMasterSteamPGO", exeName),
                        System.IO.Path.Combine(path, "Bin", "Win64", exeName)
                    };
                
                foreach (var exePath in possiblePaths)
                {
                    if (System.IO.File.Exists(exePath))
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
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
        
        public string SelectFolderButtonText
        {
            get => _selectFolderButtonText;
            set => SetProperty(ref _selectFolderButtonText, value);
        }
        
        public string SelectFolderTooltipText
        {
            get => _selectFolderTooltipText;
            set => SetProperty(ref _selectFolderTooltipText, value);
        }
        
        public string ClickCardToSelectText
        {
            get => _clickCardToSelectText;
            set => SetProperty(ref _clickCardToSelectText, value);
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
        
        // Folder-Status Properties
        public bool KCD1FolderFound
        {
            get => _kcd1FolderFound;
            set => SetProperty(ref _kcd1FolderFound, value);
        }
        
        public bool KCD2FolderFound
        {
            get => _kcd2FolderFound;
            set => SetProperty(ref _kcd2FolderFound, value);
        }
        
        public string KCD1FolderStatus
        {
            get => _kcd1FolderStatus;
            set => SetProperty(ref _kcd1FolderStatus, value);
        }
        
        public string KCD2FolderStatus
        {
            get => _kcd2FolderStatus;
            set => SetProperty(ref _kcd2FolderStatus, value);
        }
        
        public string KCD1FolderPath
        {
            get => _kcd1FolderPath;
            set => SetProperty(ref _kcd1FolderPath, value);
        }
        
        public string KCD2FolderPath
        {
            get => _kcd2FolderPath;
            set => SetProperty(ref _kcd2FolderPath, value);
        }
        
        // Commands
        public ICommand SelectKCD1FolderCommand { get; }
        public ICommand SelectKCD2FolderCommand { get; }
        public ICommand SelectGameCommand { get; }
        
        /// <summary>
        /// Wählt ein Spiel aus (wird von Karten-Click aufgerufen)
        /// </summary>
        public void SelectGame(GameType gameType)
        {
            SelectedGame = gameType;
        }
        
        /// <summary>
        /// Event das ausgelöst wird, wenn ein Spiel ausgewählt wurde
        /// </summary>
        public event EventHandler<GameType>? GameSelected;
    }
}

