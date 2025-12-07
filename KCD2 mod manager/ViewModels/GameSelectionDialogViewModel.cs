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
        /// WICHTIG: Verwendet EXE-Auswahl statt Folder-Picker für bessere Kompatibilität
        /// </summary>
        private async Task SelectFolderAsync(GameType gameType)
        {
            try
            {
                string gameName = gameType == GameType.KCD1 ? KCD1DisplayName : KCD2DisplayName;
                string exeName = "KingdomCome.exe";
                
                // Zeige Dialog zur EXE-Auswahl
                string filter = $"Game Executable ({exeName})|{exeName}|All Files (*.*)|*.*";
                string title = string.Format(
                    Strings.ResourceManager.GetString("SelectGameExecutablePrompt") ?? 
                    "Select {0} Executable ({1})",
                    gameName, exeName);
                
                string? selectedExePath = _dialogService.ShowOpenFileDialog(filter, title);
                if (!string.IsNullOrEmpty(selectedExePath) && System.IO.File.Exists(selectedExePath))
                {
                    // Prüfe zuerst, ob es die richtige EXE ist
                    string fileName = System.IO.Path.GetFileName(selectedExePath);
                    if (!fileName.Equals("KingdomCome.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        _dialogService.ShowMessageBox(
                            Strings.ResourceManager.GetString("ErrorInvalidExeName") ?? 
                            $"The selected file is not a valid game executable. Expected: KingdomCome.exe, Found: {fileName}",
                            Strings.ResourceManager.GetString("DialogTitleError") ?? "Invalid Executable",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                    
                    // Ermittle den Game-Ordner aus dem EXE-Pfad
                    string? gameFolder = GetGameFolderFromExePath(gameType, selectedExePath);
                    
                    // Wenn Game-Ordner nicht gefunden wurde, versuche es mit dem Verzeichnis der EXE
                    if (string.IsNullOrEmpty(gameFolder) || !System.IO.Directory.Exists(gameFolder))
                    {
                        // Fallback: Verwende das Verzeichnis der EXE als Game-Ordner
                        gameFolder = System.IO.Path.GetDirectoryName(selectedExePath);
                        
                        // Wenn die EXE direkt im Root liegt, verwende das Parent-Verzeichnis
                        if (gameFolder != null && System.IO.Path.GetFileName(gameFolder).Equals("Bin", StringComparison.OrdinalIgnoreCase))
                        {
                            gameFolder = System.IO.Directory.GetParent(gameFolder)?.FullName;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(gameFolder) && System.IO.Directory.Exists(gameFolder))
                    {
                        // Validiere den Pfad (mit erweiterter Suche)
                        bool isValid = await ValidateGameFolderAsync(gameType, gameFolder);
                        if (isValid)
                        {
                            // Speichere den Pfad
                            if (gameType == GameType.KCD1)
                                _settings.GamePath_KCD1 = gameFolder;
                            else
                                _settings.GamePath_KCD2 = gameFolder;
                            _settings.Save();
                            
                            _logger.Info($"Game folder selected for {gameType}: {gameFolder} (from EXE: {selectedExePath})");
                            
                            // Status aktualisieren
                            UpdateFolderStatus();
                            
                            // Info-Dialog
                            _dialogService.ShowMessageBox(
                                string.Format(Strings.ResourceManager.GetString("GameFolderSaved") ?? "Path saved: {0}", gameFolder),
                                Strings.ResourceManager.GetString("DialogTitleInformation") ?? "Information",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            // Erweiterte Fehlermeldung mit Hinweis
                            string errorMessage = string.Format(
                                Strings.ResourceManager.GetString("ErrorInvalidPathDetailed") ??
                                "The selected executable does not appear to be a valid {0} installation.\n\n" +
                                "Please ensure you selected the correct KingdomCome.exe file.\n" +
                                "The file should be located in a 'Bin' subfolder of your game installation.",
                                gameName);
                            
                            _dialogService.ShowMessageBox(
                                errorMessage,
                                Strings.ResourceManager.GetString("DialogTitleError") ?? "Invalid Path",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        _dialogService.ShowMessageBox(
                            Strings.ResourceManager.GetString("ErrorInvalidExePath") ?? "Could not determine the game folder from the selected executable path.",
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
        /// Ermittelt den Game-Ordner aus dem EXE-Pfad
        /// WICHTIG: Unterstützt alle möglichen Pfadstrukturen (Steam, GOG, etc.)
        /// </summary>
        private string? GetGameFolderFromExePath(GameType gameType, string exePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                    return null;
                
                string directory = System.IO.Path.GetDirectoryName(exePath) ?? "";
                
                // Strategie 1: Prüfe bekannte Pfadstrukturen
                // Für KCD1: EXE ist normalerweise in Bin\Win64
                // Für KCD2: EXE kann in Bin\Win64MasterMasterSteamPGO oder Bin\Win64 sein
                if (gameType == GameType.KCD1)
                {
                    // Prüfe ob wir in Bin\Win64 sind
                    if (directory.EndsWith("Bin\\Win64", StringComparison.OrdinalIgnoreCase) ||
                        directory.EndsWith("Bin/Win64", StringComparison.OrdinalIgnoreCase))
                    {
                        // Gehe 2 Ebenen nach oben: Bin\Win64 -> Bin -> Game-Ordner
                        var parent = System.IO.Directory.GetParent(directory);
                        if (parent != null)
                        {
                            var gameFolder = System.IO.Directory.GetParent(parent.FullName);
                            if (gameFolder != null)
                                return gameFolder.FullName;
                        }
                    }
                }
                else // KCD2
                {
                    // Prüfe ob wir in Bin\Win64MasterMasterSteamPGO oder Bin\Win64 sind
                    if (directory.EndsWith("Bin\\Win64MasterMasterSteamPGO", StringComparison.OrdinalIgnoreCase) ||
                        directory.EndsWith("Bin/Win64MasterMasterSteamPGO", StringComparison.OrdinalIgnoreCase))
                    {
                        // Gehe 2 Ebenen nach oben: Bin\Win64MasterMasterSteamPGO -> Bin -> Game-Ordner
                        var parent = System.IO.Directory.GetParent(directory);
                        if (parent != null)
                        {
                            var gameFolder = System.IO.Directory.GetParent(parent.FullName);
                            if (gameFolder != null)
                                return gameFolder.FullName;
                        }
                    }
                    else if (directory.EndsWith("Bin\\Win64", StringComparison.OrdinalIgnoreCase) ||
                             directory.EndsWith("Bin/Win64", StringComparison.OrdinalIgnoreCase))
                    {
                        // Gehe 2 Ebenen nach oben: Bin\Win64 -> Bin -> Game-Ordner
                        var parent = System.IO.Directory.GetParent(directory);
                        if (parent != null)
                        {
                            var gameFolder = System.IO.Directory.GetParent(parent.FullName);
                            if (gameFolder != null)
                                return gameFolder.FullName;
                        }
                    }
                }
                
                // Strategie 2: Suche nach "Bin"-Ordner im Pfad (funktioniert für alle Strukturen)
                string? currentDir = directory;
                while (!string.IsNullOrEmpty(currentDir) && currentDir != System.IO.Path.GetPathRoot(currentDir))
                {
                    // Prüfe ob "Bin" in diesem Verzeichnis existiert
                    string binPath = System.IO.Path.Combine(currentDir, "Bin");
                    if (System.IO.Directory.Exists(binPath))
                    {
                        // Prüfe ob dies ein gültiger Game-Ordner ist (hat Data-Ordner oder Mods-Ordner)
                        string dataPath = System.IO.Path.Combine(currentDir, "Data");
                        string modsPath = System.IO.Path.Combine(currentDir, "Mods");
                        if (System.IO.Directory.Exists(dataPath) || System.IO.Directory.Exists(modsPath))
                        {
                            return currentDir;
                        }
                    }
                    currentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
                }
                
                // Strategie 3: Suche nach typischen Game-Ordner-Markern
                currentDir = directory;
                while (!string.IsNullOrEmpty(currentDir) && currentDir != System.IO.Path.GetPathRoot(currentDir))
                {
                    // Prüfe auf typische Game-Ordner-Namen
                    string folderName = System.IO.Path.GetFileName(currentDir);
                    if ((gameType == GameType.KCD1 && 
                         (folderName.Contains("KingdomCome", StringComparison.OrdinalIgnoreCase) ||
                          folderName.Contains("KCD", StringComparison.OrdinalIgnoreCase))) ||
                        (gameType == GameType.KCD2 && 
                         (folderName.Contains("KingdomCome", StringComparison.OrdinalIgnoreCase) ||
                          folderName.Contains("KCD2", StringComparison.OrdinalIgnoreCase) ||
                          folderName.Contains("Deliverance2", StringComparison.OrdinalIgnoreCase))))
                    {
                        // Prüfe ob Data-Ordner oder Mods-Ordner existiert
                        string dataPath = System.IO.Path.Combine(currentDir, "Data");
                        string modsPath = System.IO.Path.Combine(currentDir, "Mods");
                        if (System.IO.Directory.Exists(dataPath) || System.IO.Directory.Exists(modsPath))
                        {
                            return currentDir;
                        }
                    }
                    currentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
                }
                
                // Strategie 4: Letzter Fallback - verwende das Verzeichnis der EXE
                // Dies funktioniert, wenn die EXE direkt im Game-Ordner liegt (selten, aber möglich)
                return directory;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Fehler beim Ermitteln des Game-Ordners aus EXE-Pfad: {ex.Message}", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Validiert einen Game-Folder
        /// WICHTIG: Flexiblere Validierung, die auch GOG-Versionen und andere Strukturen unterstützt
        /// </summary>
        private async Task<bool> ValidateGameFolderAsync(GameType gameType, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
                    return false;
                
                // Prüfe ob Executable existiert - erweiterte Suche
                string exeName = "KingdomCome.exe";
                
                // Standard-Pfade (Steam)
                List<string> possiblePaths = new List<string>();
                
                if (gameType == GameType.KCD1)
                {
                    possiblePaths.Add(System.IO.Path.Combine(path, "Bin", "Win64", exeName));
                }
                else // KCD2
                {
                    possiblePaths.Add(System.IO.Path.Combine(path, "Bin", "Win64MasterMasterSteamPGO", exeName));
                    possiblePaths.Add(System.IO.Path.Combine(path, "Bin", "Win64", exeName));
                }
                
                // Prüfe Standard-Pfade zuerst
                foreach (var exePath in possiblePaths)
                {
                    if (System.IO.File.Exists(exePath))
                    {
                        _logger?.Info($"Game-Ordner validiert (Standard-Pfad): {path} -> {exePath}");
                        return true;
                    }
                }
                
                // Erweiterte Suche: Suche rekursiv nach der EXE im Bin-Ordner
                // Dies unterstützt GOG-Versionen und andere Strukturen
                string binPath = System.IO.Path.Combine(path, "Bin");
                if (System.IO.Directory.Exists(binPath))
                {
                    // Suche rekursiv nach KingdomCome.exe
                    var foundExe = FindExecutableRecursive(binPath, exeName);
                    if (!string.IsNullOrEmpty(foundExe) && System.IO.File.Exists(foundExe))
                    {
                        _logger?.Info($"Game-Ordner validiert (rekursive Suche): {path} -> {foundExe}");
                        return true;
                    }
                }
                
                // Letzter Fallback: Prüfe ob EXE direkt im Root-Ordner liegt (selten, aber möglich)
                string rootExe = System.IO.Path.Combine(path, exeName);
                if (System.IO.File.Exists(rootExe))
                {
                    _logger?.Info($"Game-Ordner validiert (Root-Pfad): {path} -> {rootExe}");
                    return true;
                }
                
                _logger?.Warning($"Game-Ordner konnte nicht validiert werden: {path} (keine EXE gefunden)");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Fehler beim Validieren des Game-Ordners: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Sucht rekursiv nach einer EXE-Datei in einem Verzeichnis
        /// </summary>
        private string? FindExecutableRecursive(string directory, string exeName)
        {
            try
            {
                if (!System.IO.Directory.Exists(directory))
                    return null;
                
                // Prüfe direkt in diesem Verzeichnis
                string directPath = System.IO.Path.Combine(directory, exeName);
                if (System.IO.File.Exists(directPath))
                    return directPath;
                
                // Suche rekursiv in Unterordnern (max. 3 Ebenen tief für Performance)
                return FindExecutableRecursive(directory, exeName, 0, 3);
            }
            catch
            {
                return null;
            }
        }
        
        private string? FindExecutableRecursive(string directory, string exeName, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return null;
            
            try
            {
                // Prüfe direkt in diesem Verzeichnis
                string directPath = System.IO.Path.Combine(directory, exeName);
                if (System.IO.File.Exists(directPath))
                    return directPath;
                
                // Suche in Unterordnern
                var subdirs = System.IO.Directory.GetDirectories(directory);
                foreach (var subdir in subdirs)
                {
                    var found = FindExecutableRecursive(subdir, exeName, currentDepth + 1, maxDepth);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }
            catch
            {
                // Ignoriere Fehler (z.B. Zugriffsrechte)
            }
            
            return null;
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

