using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für SettingsWindow - verwaltet alle Einstellungen
    /// </summary>
    public class SettingsWindowViewModel : ViewModelBase
    {
        private readonly IAppSettings _settings;
        private readonly IDialogService _dialogService;
        private readonly ILog _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IModOrderFileManager? _modOrderFileManager;
        private readonly IGameInstallService? _gameInstallService;
        private readonly IFileService? _fileService;

        private const string DefaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\KingdomComeDeliverance2\Bin\Win64MasterMasterSteamPGO\KingdomCome.exe";

        private bool _isDarkMode;
        private bool _isDevMode;
        private bool _askOnDelete;
        private bool _enableUpdateNotifications;
        private bool _modOrderEnabled;
        private bool _enableFileRenaming;
        private bool _allowWorkshopModActions;
        private bool _createBackup;
        private bool _backupOnStartup;
        private int _backupMaxCount;
        private string _selectedLanguage;
        
        // Lokalisierte Text-Properties - werden in UpdateLocalizedStrings() initialisiert
        private string _generalSettingsText = string.Empty;
        private string _toggleDarkModeText = string.Empty;
        private string _toggleDevModeText = string.Empty;
        private string _toggleDeleteConfirmationText = string.Empty;
        private string _toggleUpdateNotificationsText = string.Empty;
        private string _toggleModOrderCreationText = string.Empty;
        private string _languageSettingsText = string.Empty;
        private string _selectLanguageText = string.Empty;
        private string _languageText = string.Empty;
        private string _backupSettingsText = string.Empty;
        private string _toggleBackupCreationText = string.Empty;
        private string _backupOnStartupText = string.Empty;
        private string _setMaxBackupsText = string.Empty;
        private string _setButtonText = string.Empty;
        private string _enableDeleteConfirmationText = string.Empty;
        private string _enableFileRenamingText = string.Empty;
        private string _enableFileRenamingTooltip = string.Empty;
        private string _allowWorkshopModActionsText = string.Empty;
        private string _allowWorkshopModActionsTooltip = string.Empty;

        public SettingsWindowViewModel(
            IAppSettings settings, 
            IDialogService dialogService, 
            ILog logger, 
            ILocalizationService localizationService,
            IModOrderFileManager? modOrderFileManager = null,
            IGameInstallService? gameInstallService = null,
            IFileService? fileService = null)
        {
            _settings = settings;
            _dialogService = dialogService;
            _logger = logger;
            _localizationService = localizationService;
            _modOrderFileManager = modOrderFileManager;
            _gameInstallService = gameInstallService;
            _fileService = fileService;

            LoadSettings();
            InitializeCommands();
            
            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        private void InitializeCommands()
        {
            SetMaxBackupsCommand = new RelayCommand(_ => SetMaxBackups());
            ToggleDarkModeCommand = new RelayCommand<bool?>(isChecked => ToggleDarkMode(isChecked ?? false));
            ToggleDevModeCommand = new RelayCommand<bool?>(isChecked => ToggleDevMode(isChecked ?? false));
            ToggleDeleteConfirmationCommand = new RelayCommand<bool?>(isChecked => ToggleDeleteConfirmation(isChecked ?? false));
            ToggleUpdateNotificationsCommand = new RelayCommand<bool?>(isChecked => ToggleUpdateNotifications(isChecked ?? false));
            ToggleModOrderCreationCommand = new RelayCommand<bool?>(isChecked => ToggleModOrderCreation(isChecked ?? false));
            ToggleFileRenamingCommand = new RelayCommand<bool?>(isChecked => ToggleFileRenaming(isChecked ?? false));
            ToggleWorkshopActionsCommand = new RelayCommand<bool?>(isChecked => ToggleWorkshopActions(isChecked ?? false));
            ToggleBackupCreationCommand = new RelayCommand<bool?>(isChecked => ToggleBackupCreation(isChecked ?? false));
            ToggleBackupOnStartupCommand = new RelayCommand<bool?>(isChecked => ToggleBackupOnStartup(isChecked ?? false));
            ChangeLanguageCommand = new RelayCommand<string>(language => ChangeLanguage(language));
        }

        private void LoadSettings()
        {
            IsDarkMode = _settings.IsDarkMode;
            IsDevMode = _settings.IsDevMode;
            AskOnDelete = _settings.AskOnDelete;
            EnableUpdateNotifications = _settings.EnableUpdateNotifications;
            ModOrderEnabled = _settings.ModOrderEnabled;
            EnableFileRenaming = _settings.EnableFileRenaming;
            AllowWorkshopModActions = _settings.AllowWorkshopModActions;
            CreateBackup = _settings.CreateBackup;
            BackupOnStartup = _settings.BackupOnStartup;
            BackupMaxCount = _settings.BackupMaxCount;
            SelectedLanguage = _settings.Language ?? "en";
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        public bool IsDevMode
        {
            get => _isDevMode;
            set => SetProperty(ref _isDevMode, value);
        }

        public bool AskOnDelete
        {
            get => _askOnDelete;
            set => SetProperty(ref _askOnDelete, value);
        }

        public bool EnableUpdateNotifications
        {
            get => _enableUpdateNotifications;
            set => SetProperty(ref _enableUpdateNotifications, value);
        }

        public bool ModOrderEnabled
        {
            get => _modOrderEnabled;
            set => SetProperty(ref _modOrderEnabled, value);
        }

        public bool EnableFileRenaming
        {
            get => _enableFileRenaming;
            set => SetProperty(ref _enableFileRenaming, value);
        }

        public bool AllowWorkshopModActions
        {
            get => _allowWorkshopModActions;
            set => SetProperty(ref _allowWorkshopModActions, value);
        }

        public bool CreateBackup
        {
            get => _createBackup;
            set => SetProperty(ref _createBackup, value);
        }

        public bool BackupOnStartup
        {
            get => _backupOnStartup;
            set => SetProperty(ref _backupOnStartup, value);
        }

        public int BackupMaxCount
        {
            get => _backupMaxCount;
            set => SetProperty(ref _backupMaxCount, value);
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        public Dictionary<string, string> AvailableLanguages => _localizationService.GetAvailableLanguages();

        public string GeneralSettingsText
        {
            get => _generalSettingsText;
            set => SetProperty(ref _generalSettingsText, value);
        }

        public string SetButtonText
        {
            get => _setButtonText;
            set => SetProperty(ref _setButtonText, value);
        }

        public string EnableDeleteConfirmationText
        {
            get => _enableDeleteConfirmationText;
            set => SetProperty(ref _enableDeleteConfirmationText, value);
        }

        public string ToggleDarkModeText
        {
            get => _toggleDarkModeText;
            set => SetProperty(ref _toggleDarkModeText, value);
        }

        public string ToggleDevModeText
        {
            get => _toggleDevModeText;
            set => SetProperty(ref _toggleDevModeText, value);
        }

        public string ToggleUpdateNotificationsText
        {
            get => _toggleUpdateNotificationsText;
            set => SetProperty(ref _toggleUpdateNotificationsText, value);
        }

        public string ToggleModOrderCreationText
        {
            get => _toggleModOrderCreationText;
            set => SetProperty(ref _toggleModOrderCreationText, value);
        }

        public string LanguageSettingsText
        {
            get => _languageSettingsText;
            set => SetProperty(ref _languageSettingsText, value);
        }

        public string LanguageText
        {
            get => _languageText;
            set => SetProperty(ref _languageText, value);
        }

        public string BackupSettingsText
        {
            get => _backupSettingsText;
            set => SetProperty(ref _backupSettingsText, value);
        }

        public string ToggleBackupCreationText
        {
            get => _toggleBackupCreationText;
            set => SetProperty(ref _toggleBackupCreationText, value);
        }

        public string BackupOnStartupText
        {
            get => _backupOnStartupText;
            set => SetProperty(ref _backupOnStartupText, value);
        }

        public string SetMaxBackupsText
        {
            get => _setMaxBackupsText;
            set => SetProperty(ref _setMaxBackupsText, value);
        }

        public string ToggleDeleteConfirmationText
        {
            get => _toggleDeleteConfirmationText;
            set => SetProperty(ref _toggleDeleteConfirmationText, value);
        }

        public string EnableFileRenamingText
        {
            get => _enableFileRenamingText;
            set => SetProperty(ref _enableFileRenamingText, value);
        }

        public string EnableFileRenamingTooltip
        {
            get => _enableFileRenamingTooltip;
            set => SetProperty(ref _enableFileRenamingTooltip, value);
        }

        public string AllowWorkshopModActionsText
        {
            get => _allowWorkshopModActionsText;
            set => SetProperty(ref _allowWorkshopModActionsText, value);
        }

        public string AllowWorkshopModActionsTooltip
        {
            get => _allowWorkshopModActionsTooltip;
            set => SetProperty(ref _allowWorkshopModActionsTooltip, value);
        }

        public string SelectLanguageText
        {
            get => _selectLanguageText;
            set => SetProperty(ref _selectLanguageText, value);
        }

        public IAppSettings Settings => _settings;

        // Commands
        public ICommand SetMaxBackupsCommand { get; private set; } = null!;
        public ICommand ToggleDarkModeCommand { get; private set; } = null!;
        public ICommand ToggleDevModeCommand { get; private set; } = null!;
        public ICommand ToggleDeleteConfirmationCommand { get; private set; } = null!;
        public ICommand ToggleUpdateNotificationsCommand { get; private set; } = null!;
        public ICommand ToggleModOrderCreationCommand { get; private set; } = null!;
        public ICommand ToggleFileRenamingCommand { get; private set; } = null!;
        public ICommand ToggleWorkshopActionsCommand { get; private set; } = null!;
        public ICommand ToggleBackupCreationCommand { get; private set; } = null!;
        public ICommand ToggleBackupOnStartupCommand { get; private set; } = null!;
        public ICommand ChangeLanguageCommand { get; private set; } = null!;

        public event EventHandler? ThemeChanged;
        public event EventHandler? LanguageChanged;

        private void SetMaxBackups()
        {
            string? input = _dialogService.ShowInputDialog(
                Resources.Messages.PromptSetMaxBackups, 
                Resources.Messages.TitleSetMaxBackups, 
                _settings.BackupMaxCount.ToString());
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int maxBackups) && maxBackups > 0)
            {
                BackupMaxCount = maxBackups;
                _settings.BackupMaxCount = maxBackups;
                _settings.Save();
                _dialogService.ShowMessageBox(string.Format(KCD2_mod_manager.Resources.Messages.InfoMaxBackupsSet, maxBackups), KCD2_mod_manager.Resources.Messages.DialogTitleInformation, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else if (!string.IsNullOrEmpty(input))
            {
                _dialogService.ShowMessageBox(KCD2_mod_manager.Resources.Messages.ErrorInvalidNumber, KCD2_mod_manager.Resources.Messages.DialogTitleError, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ToggleDarkMode(bool isChecked)
        {
            IsDarkMode = isChecked;
            _settings.IsDarkMode = isChecked;
            _settings.Save();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleDevMode(bool isChecked)
        {
            IsDevMode = isChecked;
            _settings.IsDevMode = isChecked;
            _settings.Save();
        }

        private void ToggleDeleteConfirmation(bool isChecked)
        {
            AskOnDelete = isChecked;
            _settings.AskOnDelete = isChecked;
            _settings.Save();
        }

        private void ToggleUpdateNotifications(bool isChecked)
        {
            EnableUpdateNotifications = isChecked;
            _settings.EnableUpdateNotifications = isChecked;
            _settings.Save();
        }

        private async void ToggleModOrderCreation(bool isChecked)
        {
            bool oldValue = ModOrderEnabled;
            ModOrderEnabled = isChecked;
            _settings.ModOrderEnabled = isChecked;
            _settings.Save();
            
            // WICHTIG: Wende das Setting sofort an (verschiebe Dateien atomar)
            if (_modOrderFileManager != null && _gameInstallService != null)
            {
                try
                {
                    // Bestimme Mod-Ordner basierend auf ausgewähltem Spiel
                    // Da wir im SettingsWindow sind, nehmen wir das aktuelle Spiel aus GameInstallService
                    var selectedGame = _gameInstallService.SelectedGame;
                    string? modFolder = null;
                    
                    if (selectedGame == GameType.KCD1 && _gameInstallService.KCD1Install != null)
                    {
                        modFolder = _gameInstallService.KCD1Install.ModsPath;
                    }
                    else if (selectedGame == GameType.KCD2 && _gameInstallService.KCD2Install != null)
                    {
                        modFolder = _gameInstallService.KCD2Install.ModsPath;
                    }
                    
                    if (!string.IsNullOrEmpty(modFolder) && _fileService.DirectoryExists(modFolder))
                    {
                        _logger.Info($"Toggle ModOrderEnabled: {oldValue} -> {isChecked}, wende auf Mod-Ordner an: {modFolder}");
                        await _modOrderFileManager.ApplyModOrderSettingAsync(isChecked, modFolder);
                        _logger.Info($"ModOrderEnabled-Setting erfolgreich angewendet");
                    }
                    else
                    {
                        _logger.Warning($"Mod-Ordner nicht gefunden, kann ModOrderEnabled-Setting nicht anwenden");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler beim Anwenden des ModOrderEnabled-Settings: {ex.Message}", ex);
                    // Stelle alten Wert wieder her
                    ModOrderEnabled = oldValue;
                    _settings.ModOrderEnabled = oldValue;
                    _settings.Save();
                    
                    _dialogService.ShowMessageBox(
                        $"Fehler beim Ändern der Mod-Order-Einstellung: {ex.Message}",
                        Resources.Messages.DialogTitleError,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ToggleFileRenaming(bool isChecked)
        {
            EnableFileRenaming = isChecked;
            _settings.EnableFileRenaming = isChecked;
            _settings.Save();
        }

        private void ToggleWorkshopActions(bool isChecked)
        {
            AllowWorkshopModActions = isChecked;
            _settings.AllowWorkshopModActions = isChecked;

            if (isChecked && !_settings.AllowWorkshopActionsWarningShown)
            {
                _settings.AllowWorkshopActionsWarningShown = true;
                _dialogService.ShowMessageBox(
                    Resources.Messages.WorkshopActionsWarning,
                    Resources.Messages.DialogTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            _settings.Save();
            _logger.Info($"AllowWorkshopModActions toggled: {isChecked}");
        }

        private void ToggleBackupCreation(bool isChecked)
        {
            CreateBackup = isChecked;
            _settings.CreateBackup = isChecked;
            _settings.Save();
        }

        private void ToggleBackupOnStartup(bool isChecked)
        {
            BackupOnStartup = isChecked;
            _settings.BackupOnStartup = isChecked;
            _settings.Save();
        }

        private void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return;
            
            // Nur ändern, wenn sich die Sprache tatsächlich geändert hat
            string currentLanguage = _settings.Language ?? "en";
            if (currentLanguage == languageCode) return;

            SelectedLanguage = languageCode;
            _localizationService.SetLanguage(languageCode);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateTheme(ResourceDictionary resources)
        {
            if (IsDarkMode)
            {
                // Dark Mode Colors
                resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                resources["ModListItemEvenBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                resources["ModListItemOddBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.White);
                resources["SelectedItemBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 120));
            }
            else
            {
                // Light Mode Colors
                resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["ModListItemEvenBrush"] = new SolidColorBrush(Colors.White);
                resources["ModListItemOddBrush"] = new SolidColorBrush(Colors.LightGray);
                resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.Black);
                resources["SelectedItemBrush"] = new SolidColorBrush(Colors.LightBlue);
            }
        }

        /// <summary>
        /// Aktualisiert alle lokalisierten Strings
        /// WICHTIG: Wird beim Start und bei Sprachänderungen aufgerufen
        /// </summary>
        private void UpdateLocalizedStrings()
        {
            // WICHTIG: Verwende aktuelle Culture für Resource-Lookup
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            GeneralSettingsText = Resources.Strings.ResourceManager.GetString("GeneralSettings", culture) ?? Resources.Strings.GeneralSettings;
            ToggleDarkModeText = Resources.Strings.ResourceManager.GetString("ToggleDarkMode", culture) ?? Resources.Strings.ToggleDarkMode;
            ToggleDevModeText = Resources.Strings.ResourceManager.GetString("ToggleDevMode", culture) ?? Resources.Strings.ToggleDevMode;
            ToggleDeleteConfirmationText = Resources.Strings.ResourceManager.GetString("ToggleDeleteConfirmation", culture) ?? Resources.Strings.ToggleDeleteConfirmation;
            ToggleUpdateNotificationsText = Resources.Strings.ResourceManager.GetString("ToggleUpdateNotifications", culture) ?? Resources.Strings.ToggleUpdateNotifications;
            ToggleModOrderCreationText = Resources.Strings.ResourceManager.GetString("ToggleModOrderCreation", culture) ?? Resources.Strings.ToggleModOrderCreation;
            EnableFileRenamingText = Resources.Strings.ResourceManager.GetString("EnableFileRenaming", culture) ?? Resources.Strings.EnableFileRenaming;
            EnableFileRenamingTooltip = Resources.Strings.ResourceManager.GetString("EnableFileRenamingTooltip", culture) ?? Resources.Strings.EnableFileRenamingTooltip;
            AllowWorkshopModActionsText = Resources.Strings.ResourceManager.GetString("AllowWorkshopActionsText", culture) ?? "Enable extra actions for Workshop mods";
            AllowWorkshopModActionsTooltip = Resources.Strings.ResourceManager.GetString("AllowWorkshopActionsTooltip", culture) ?? "Enabling this will allow actions such as visiting the mod webpage or forcing update checks on mods installed from the Steam Workshop. Not recommended as Workshop files are typically managed by Steam.";
            LanguageSettingsText = Resources.Strings.ResourceManager.GetString("LanguageSettings", culture) ?? Resources.Strings.LanguageSettings;
            SelectLanguageText = Resources.Strings.ResourceManager.GetString("SelectLanguage", culture) ?? Resources.Strings.SelectLanguage;
            LanguageText = Resources.Strings.ResourceManager.GetString("Language", culture) ?? Resources.Strings.Language;
            BackupSettingsText = Resources.Strings.ResourceManager.GetString("BackupSettings", culture) ?? Resources.Strings.BackupSettings;
            ToggleBackupCreationText = Resources.Strings.ResourceManager.GetString("ToggleBackupCreation", culture) ?? Resources.Strings.ToggleBackupCreation;
            BackupOnStartupText = Resources.Strings.ResourceManager.GetString("BackupOnStartup", culture) ?? Resources.Strings.BackupOnStartup;
            SetMaxBackupsText = Resources.Strings.ResourceManager.GetString("SetMaxBackups", culture) ?? Resources.Strings.SetMaxBackups;
            SetButtonText = Resources.Strings.ResourceManager.GetString("Set", culture) ?? Resources.Strings.Set;
            EnableDeleteConfirmationText = Resources.Strings.ResourceManager.GetString("EnableDeleteConfirmation", culture) ?? Resources.Strings.EnableDeleteConfirmation;
        }
    }
}

