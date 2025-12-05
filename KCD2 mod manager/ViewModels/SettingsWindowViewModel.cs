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

        private const string DefaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\KingdomComeDeliverance2\Bin\Win64MasterMasterSteamPGO\KingdomCome.exe";

        private bool _isDarkMode;
        private bool _isDevMode;
        private bool _askOnDelete;
        private bool _enableUpdateNotifications;
        private bool _modOrderEnabled;
        private bool _createBackup;
        private bool _backupOnStartup;
        private int _backupMaxCount;
        private string _selectedLanguage;
        
        // Lokalisierte Text-Properties - werden in UpdateLocalizedStrings() initialisiert
        private string _generalSettingsText = string.Empty;
        private string _setGamePathText = string.Empty;
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

        public SettingsWindowViewModel(IAppSettings settings, IDialogService dialogService, ILog logger, ILocalizationService localizationService)
        {
            _settings = settings;
            _dialogService = dialogService;
            _logger = logger;
            _localizationService = localizationService;

            LoadSettings();
            InitializeCommands();
            
            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        private void InitializeCommands()
        {
            SetGamePathCommand = new RelayCommand(_ => SetGamePath());
            SetMaxBackupsCommand = new RelayCommand(_ => SetMaxBackups());
            ToggleDarkModeCommand = new RelayCommand<bool?>(isChecked => ToggleDarkMode(isChecked ?? false));
            ToggleDevModeCommand = new RelayCommand<bool?>(isChecked => ToggleDevMode(isChecked ?? false));
            ToggleDeleteConfirmationCommand = new RelayCommand<bool?>(isChecked => ToggleDeleteConfirmation(isChecked ?? false));
            ToggleUpdateNotificationsCommand = new RelayCommand<bool?>(isChecked => ToggleUpdateNotifications(isChecked ?? false));
            ToggleModOrderCreationCommand = new RelayCommand<bool?>(isChecked => ToggleModOrderCreation(isChecked ?? false));
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

        public string SetGamePathText
        {
            get => _setGamePathText;
            set => SetProperty(ref _setGamePathText, value);
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

        public string SelectLanguageText
        {
            get => _selectLanguageText;
            set => SetProperty(ref _selectLanguageText, value);
        }

        public IAppSettings Settings => _settings;

        // Commands
        public ICommand SetGamePathCommand { get; private set; } = null!;
        public ICommand SetMaxBackupsCommand { get; private set; } = null!;
        public ICommand ToggleDarkModeCommand { get; private set; } = null!;
        public ICommand ToggleDevModeCommand { get; private set; } = null!;
        public ICommand ToggleDeleteConfirmationCommand { get; private set; } = null!;
        public ICommand ToggleUpdateNotificationsCommand { get; private set; } = null!;
        public ICommand ToggleModOrderCreationCommand { get; private set; } = null!;
        public ICommand ToggleBackupCreationCommand { get; private set; } = null!;
        public ICommand ToggleBackupOnStartupCommand { get; private set; } = null!;
        public ICommand ChangeLanguageCommand { get; private set; } = null!;

        public event EventHandler? ThemeChanged;
        public event EventHandler? LanguageChanged;

        private void SetGamePath()
        {
            string? selectedPath = _dialogService.ShowOpenFileDialog("Game Executable (*.exe)|*.exe", "Select Kingdom Come Deliverance 2 Executable");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _settings.GamePath = selectedPath;
                _settings.Save();
                _logger.Info($"Game path set to: {selectedPath}");
            }
        }

        private void SetMaxBackups()
        {
            string? input = _dialogService.ShowInputDialog("Enter the maximum number of backups to keep:", "Set Max Backups", _settings.BackupMaxCount.ToString());
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

        private void ToggleModOrderCreation(bool isChecked)
        {
            ModOrderEnabled = isChecked;
            _settings.ModOrderEnabled = isChecked;
            _settings.Save();
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
            SetGamePathText = Resources.Strings.ResourceManager.GetString("SetGamePath", culture) ?? Resources.Strings.SetGamePath;
            ToggleDarkModeText = Resources.Strings.ResourceManager.GetString("ToggleDarkMode", culture) ?? Resources.Strings.ToggleDarkMode;
            ToggleDevModeText = Resources.Strings.ResourceManager.GetString("ToggleDevMode", culture) ?? Resources.Strings.ToggleDevMode;
            ToggleDeleteConfirmationText = Resources.Strings.ResourceManager.GetString("ToggleDeleteConfirmation", culture) ?? Resources.Strings.ToggleDeleteConfirmation;
            ToggleUpdateNotificationsText = Resources.Strings.ResourceManager.GetString("ToggleUpdateNotifications", culture) ?? Resources.Strings.ToggleUpdateNotifications;
            ToggleModOrderCreationText = Resources.Strings.ResourceManager.GetString("ToggleModOrderCreation", culture) ?? Resources.Strings.ToggleModOrderCreation;
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

