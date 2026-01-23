using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// ViewModel für MainWindow - enthält die gesamte Geschäftslogik
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private enum SortMode
        {
            LoadOrder,
            Title,
            Category
        }

        public class CategoryFilterOption
        {
            public string? Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
        private readonly IModInstallerService _modInstallerService;
        private readonly INexusService _nexusService;
        private readonly IDialogService _dialogService;
        private readonly IAppSettings _settings;
        private readonly IFileService _fileService;
        private readonly IModManifestService _manifestService;
        private readonly ILog _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IUserModDataService _userModDataService;
        private readonly IProfilesService _profilesService;
        private readonly IGameInstallService _gameInstallService;
        private readonly IManifestUpdateService _manifestUpdateService;
        private readonly IConflictCheckerService _conflictCheckerService;
        private readonly ICategoryService _categoryService;
        private readonly IModCategoryAssignmentService _categoryAssignmentService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModOrderFileManager? _modOrderFileManager;

        private const string DefaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\KingdomComeDeliverance2\Bin\Win64MasterMasterSteamPGO\KingdomCome.exe";
        private const string currentManagerVersion = "4.2";

        private ObservableCollection<Mod> _mods = new();
        private ICollectionView? _modsView;
        private Mod? _selectedMod;
        private string _searchText = string.Empty;
        private string _statusText = Resources.Strings.StatusReady;
        private string _modsEnabledCount = string.Format(Resources.Strings.ModsEnabled, 0);
        private string _conflictCount = string.Format(Resources.Strings.ConflictsDetected, 0);
        private int _conflictFileCount;
        private bool _hasConflicts;
        private ObservableCollection<ModConflictGroup> _conflictGroups = new();
        private bool _isUserLoggedIn;
        private string _nexusUsername = string.Empty;
        private bool _nexusIsPremium = false;
        private bool _sortByLoadOrder = true;
        private SortMode? _savedSortMode = null;
        private string _sortButtonText = Resources.Strings.ButtonSortLoadOrder;
        private SortMode _sortMode = SortMode.LoadOrder;
        private SortMode _secondarySortMode = SortMode.Title;
        private string _windowTitle = Resources.Strings.WindowTitle;
        private string _menuSettingsText = Resources.Strings.MenuSettings;
        private string _menuLoginText = Resources.Strings.MenuLogin;
        private string _menuLogoutText = Resources.Strings.MenuLogout;
        private string _buttonStartGameText = Resources.Strings.ButtonStartGame;
        private string _buttonAddModText = Resources.Strings.ButtonAddMod;
        private string _buttonReloadModsText = Resources.Strings.ButtonReloadMods;
        private bool _allowWorkshopModActions;
        private string _workshopBadgeText = Resources.Strings.ResourceManager.GetString("WorkshopBadgeText") ?? "Workshop";
        private string _workshopBadgeTooltip = Resources.Strings.ResourceManager.GetString("WorkshopBadgeTooltip") ?? "Installed from Steam Workshop";
        private string _workshopConflictNote = Resources.Strings.ResourceManager.GetString("WorkshopConflictNote") ?? "Workshop files may be managed by Steam.";
        private string _contextMenuVisitWebsiteText = Resources.Strings.ContextMenuVisitWebsite;
        private string _contextMenuChangeModNameText = Resources.Strings.ContextMenuChangeModName;
        private string _contextMenuChangeNoteText = Resources.Strings.ContextMenuChangeNote;
        private string _contextMenuAssignCategoryText = Resources.Strings.ResourceManager.GetString("ContextMenuAssignCategory") ?? "Assign category...";
        private string _contextMenuClearCategoryText = Resources.Strings.ResourceManager.GetString("ContextMenuClearCategory") ?? "Clear category";
        private string _contextMenuToggleSeparatorText = Resources.Strings.ResourceManager.GetString("ContextMenuToggleSeparator") ?? "Toggle Separator";
        private string _contextMenuEndorseModText = Resources.Strings.ContextMenuEndorseMod;
        private string _contextMenuChangeModNumberText = Resources.Strings.ContextMenuChangeModNumber;
        private string _contextMenuToggleUpdateCheckText = Resources.Strings.ContextMenuToggleUpdateCheck;
        private string _contextMenuOverrideVersionText = Resources.Strings.ResourceManager.GetString("ContextMenuOverrideVersion") ?? "Override Version";
        private string _contextMenuCheckForUpdateText = Resources.Strings.ResourceManager.GetString("ContextMenuCheckForUpdate") ?? "Check for Update";
        private string _profilesText = Resources.Strings.ResourceManager.GetString("ProfilesText") ?? "Profile:";
        private string _createProfileText = Resources.Strings.ResourceManager.GetString("CreateProfileText") ?? "Create";
        private string _duplicateProfileText = Resources.Strings.ResourceManager.GetString("DuplicateProfileText") ?? "Duplicate";
        private string _deleteProfileText = Resources.Strings.ResourceManager.GetString("DeleteProfileText") ?? "Delete";
        private string _saveProfileText = Resources.Strings.ResourceManager.GetString("SaveProfileText") ?? "Save";
        private string _manageCategoriesText = Resources.Strings.ResourceManager.GetString("ManageCategoriesText") ?? "Manage Categories";
        private string _enableAllModsText = Resources.Strings.ResourceManager.GetString("EnableAllModsText") ?? "Enable All";
        private string _disableAllModsText = Resources.Strings.ResourceManager.GetString("DisableAllModsText") ?? "Disable All";
        private string _updateManifestText = Resources.Strings.ResourceManager.GetString("UpdateManifestText") ?? "Update Manifest (Selected)";
        private string _updateAllManifestsText = Resources.Strings.ResourceManager.GetString("UpdateAllManifestsText") ?? "Update All Manifests";
        private string _switchGameText = Resources.Strings.ResourceManager.GetString("SwitchGameText") ?? "Switch Game";
        private string _gameText = Resources.Strings.ResourceManager.GetString("GameText") ?? "Game:";
        private string _conflictsButtonText = Resources.Strings.ResourceManager.GetString("ConflictsButtonText") ?? "Conflicts";
        private string _loadingModsText = Resources.Strings.ResourceManager.GetString("LoadingModsText") ?? "Loading Mods...";
        private bool _isLoadingMods = false;
        private bool _hasCategories;
        private ObservableCollection<ModCategory> _categories = new();
        private ObservableCollection<CategoryFilterOption> _categoryFilterOptions = new();
        private string? _selectedCategoryFilterId;
        private string _uncategorizedText = Resources.Strings.ResourceManager.GetString("UncategorizedText") ?? "Uncategorized";
        private string _filterCategoryText = Resources.Strings.ResourceManager.GetString("FilterCategoryText") ?? "Category:";
        private string _filterAllText = Resources.Strings.ResourceManager.GetString("FilterCategoryAll") ?? "All";
        private string _filterUncategorizedText = Resources.Strings.ResourceManager.GetString("FilterCategoryUncategorized") ?? "Uncategorized";

        private string _gamePath = string.Empty;
        private string _modFolder = string.Empty;
        private GameInstallDescriptor? _selectedGameInstall;
        private Dictionary<string, string> _modNotes = new();
        private Dictionary<string, ModVersionInfo> _modVersions = new();
        private ObservableCollection<ModProfile> _profiles = new();
        private ModProfile? _selectedProfile;
        private GameType _selectedGame = GameType.KCD2;
        private string _selectedGameDisplay = "Kingdom Come: Deliverance II";

        public MainWindowViewModel(
            IModInstallerService modInstallerService,
            INexusService nexusService,
            IDialogService dialogService,
            IAppSettings settings,
            IFileService fileService,
            IModManifestService manifestService,
            ILog logger,
            ILocalizationService localizationService,
            IUserModDataService userModDataService,
            IProfilesService profilesService,
            IGameInstallService gameInstallService,
            IManifestUpdateService manifestUpdateService,
            IConflictCheckerService conflictCheckerService,
            ICategoryService categoryService,
            IModCategoryAssignmentService categoryAssignmentService,
            IServiceProvider serviceProvider,
            IModOrderFileManager? modOrderFileManager = null)
        {
            _modInstallerService = modInstallerService;
            _nexusService = nexusService;
            _dialogService = dialogService;
            _settings = settings;
            _fileService = fileService;
            _manifestService = manifestService;
            _logger = logger;
            _localizationService = localizationService;
            _userModDataService = userModDataService;
            _profilesService = profilesService;
            _gameInstallService = gameInstallService;
            _manifestUpdateService = manifestUpdateService;
            _conflictCheckerService = conflictCheckerService;
            _categoryService = categoryService;
            _categoryAssignmentService = categoryAssignmentService;
            _serviceProvider = serviceProvider;
            _modOrderFileManager = modOrderFileManager;
            _allowWorkshopModActions = _settings.AllowWorkshopModActions;

            // Auf Sprachänderungen reagieren
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            KCD2_mod_manager.Settings.Default.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(KCD2_mod_manager.Settings.Default.AllowWorkshopModActions))
                {
                    AllowWorkshopModActions = _settings.AllowWorkshopModActions;
                }
            };

            _categoryService.CategoriesChanged += (s, e) => SyncCategoriesFromService();

            InitializeCommands();
            InitializeAsync();
            UpdateLocalizedStrings();
        }

        private void InitializeCommands()
        {
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            LoginCommand = new RelayCommand(_ => LoginAsync());
            LogoutCommand = new RelayCommand(_ => Logout());
            AddModCommand = new RelayCommand(_ => AddModAsync());
            ReloadCommand = new RelayCommand(_ => ReloadModsAsync());
            StartGameCommand = new RelayCommand(_ => StartGame());
            SortCommand = new RelayCommand(_ => ToggleSort());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());
            UpdateModCommand = new RelayCommand<Mod>(mod => UpdateModAsync(mod), mod => mod != null);
            MoveUpCommand = new RelayCommand<Mod>(mod => MoveUp(mod), mod => mod != null && SortByLoadOrder);
            MoveDownCommand = new RelayCommand<Mod>(mod => MoveDown(mod), mod => mod != null && SortByLoadOrder);
            DeleteModCommand = new RelayCommand<Mod>(mod => DeleteModAsync(mod), mod => mod != null);
            OpenFolderCommand = new RelayCommand<Mod>(mod => OpenFolder(mod), mod => mod != null);
            OpenModPageCommand = new RelayCommand<Mod>(mod => OpenModPage(mod), mod => mod != null);
            ChangeModNameCommand = new RelayCommand<Mod>(mod => ChangeModNameAsync(mod), mod => mod != null);
            ChangeModNoteCommand = new RelayCommand<Mod>(mod => ChangeModNoteAsync(mod), mod => mod != null);
            AssignCategoryCommand = new RelayCommand<Mod>(mod => AssignCategoryAsync(mod), mod => mod != null);
            ClearCategoryCommand = new RelayCommand<Mod>(mod => ClearCategoryAsync(mod), mod => mod != null);
            ToggleSeparatorCommand = new RelayCommand<Mod>(mod => ToggleSeparatorAsync(mod), mod => mod != null);
            EndorseModCommand = new RelayCommand<Mod>(mod => EndorseModAsync(mod), mod => mod != null);
            ChangeModNumberCommand = new RelayCommand<Mod>(mod => ChangeModNumberAsync(mod), mod => mod != null);
            ToggleUpdateCheckCommand = new RelayCommand<Mod>(mod => ToggleUpdateCheckAsync(mod), mod => mod != null);
            CheckForUpdateCommand = new RelayCommand<Mod>(mod => CheckForModUpdateAsync(mod), mod => mod != null && mod.ModNumber > 0);
            ModCheckBoxCommand = new RelayCommand<Mod>(mod => ModCheckBoxChanged(mod), mod => mod != null);
            SetModVersionCommand = new RelayCommand<Mod>(mod => SetModVersionAsync(mod), mod => mod != null);
            EnableAllModsCommand = new RelayCommand(async _ => await EnableAllModsAsync());
            DisableAllModsCommand = new RelayCommand(async _ => await DisableAllModsAsync());
            CreateProfileCommand = new RelayCommand(_ => CreateProfileAsync());
            LoadProfileCommand = new RelayCommand<string>(profileName => LoadProfileAsync(profileName));
            DeleteProfileCommand = new RelayCommand<string>(profileName => DeleteProfileAsync(profileName));
            SaveCurrentProfileCommand = new RelayCommand(_ => SaveCurrentProfileAsync());
            SwitchGameCommand = new RelayCommand(_ => SwitchGameAsync());
            UpdateManifestCommand = new RelayCommand<Mod>(mod => UpdateManifestAsync(mod), mod => mod != null);
            UpdateAllManifestsCommand = new RelayCommand(_ => UpdateAllManifestsAsync());
            DuplicateProfileCommand = new RelayCommand<string>(profileName => DuplicateProfileAsync(profileName));
            OpenConflictCheckerCommand = new RelayCommand(_ => OpenConflictChecker(), _ => HasConflicts);
            OpenCategoryManagerCommand = new RelayCommand(_ => OpenCategoryManager());
        }

        /// <summary>
        /// Initialisierung beim Start
        /// 
        /// WICHTIG: Die Spiel-Auswahl erfolgt bereits in App.xaml.cs, bevor MainWindow erstellt wird.
        /// Hier wird nur noch die Installation aus dem Service geholt und die Mods geladen.
        /// </summary>
        private async void InitializeAsync()
        {
            // WICHTIG: SelectedGame wurde bereits in App.xaml.cs gesetzt
            // Hier nur noch die Installation aus dem Service holen
            var selectedInstall = _gameInstallService.SelectedGame == GameType.KCD1 
                ? _gameInstallService.KCD1Install 
                : _gameInstallService.KCD2Install;

            if (selectedInstall == null)
            {
                _logger.Error("Keine Spiel-Installation gefunden nach Auswahl", null);
                _dialogService.ShowMessageBox(
                    Resources.Messages.ErrorGameInstallationNotFound, 
                    Resources.Messages.DialogTitleError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // SelectedGameInstall setzen
            SelectedGame = _gameInstallService.SelectedGame;
            SelectedGameInstall = selectedInstall;
            GamePath = selectedInstall.ExecutablePath;
            ModFolder = selectedInstall.ModsPath;
            
            // Einstellungen speichern
            _settings.GamePath = GamePath;
            _settings.LastSelectedGame = SelectedGame.ToString();
            _settings.Save();

            // Jetzt können Mods, Profile etc. geladen werden
            await InitializeAfterGameSelectionAsync();
        }

        /// <summary>
        /// Initialisiert alle Komponenten NACH der Spiel-Auswahl
        /// 
        /// Diese Methode lädt:
        /// - Mods aus dem korrekten ModsPath
        /// - Profile
        /// - Kompatibilitätsprüfungen
        /// - Updates
        /// </summary>
        private async Task InitializeAfterGameSelectionAsync()
        {
            try
            {
                IsLoadingMods = true;
                
                // WICHTIG: Konsolidiere Mod-Order-Dateien BEVOR Mods geladen werden
                if (_modOrderFileManager != null && !string.IsNullOrEmpty(ModFolder))
                {
                    try
                    {
                        _logger.Info($"Konsolidiere Mod-Order-Dateien beim Start (ModOrderEnabled={_settings.ModOrderEnabled})");
                        await _modOrderFileManager.ConsolidateModOrderFilesAsync(_settings.ModOrderEnabled, ModFolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Fehler bei der Konsolidierung der Mod-Order-Dateien: {ex.Message}", ex);
                        // Nicht weiterwerfen - Konsolidierung ist nicht kritisch
                    }
                }

                // Backup erstellen, falls aktiviert
                if (_settings.BackupOnStartup && !string.IsNullOrEmpty(ModFolder))
                {
                    await _modInstallerService.CreateModsBackupAsync(ModFolder);
                }

                await LoadCategoriesAsync();

                // Mods laden (verwendet jetzt korrekten ModFolder aus SelectedGameInstall)
                await LoadModsAsync();
                
                // Profile laden
                await LoadProfilesAsync();
                
                // Nexus-Login-Status prüfen und UI aktualisieren
                IsUserLoggedIn = _nexusService.IsUserLoggedIn();
                if (IsUserLoggedIn)
                {
                    NexusUsername = _settings.NexusUsername;
                    NexusIsPremium = _settings.NexusIsPremium;
                }
                else
                {
                    NexusUsername = string.Empty;
                    NexusIsPremium = false;
                }
                
                // Updates prüfen
                await CheckForUpdateAsync();
                await CheckForModUpdatesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei der Initialisierung nach Spiel-Auswahl: {ex.Message}", ex);
                _dialogService.ShowMessageBox(
                    string.Format(Resources.Messages.ErrorInitialization, ex.Message), 
                    Resources.Messages.DialogTitleError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingMods = false;
            }
        }

        public ObservableCollection<Mod> Mods
        {
            get => _mods;
            set => SetProperty(ref _mods, value);
        }

        public Mod? SelectedMod
        {
            get => _selectedMod;
            set => SetProperty(ref _selectedMod, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplySearchFilter();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ModsEnabledCount
        {
            get => _modsEnabledCount;
            set => SetProperty(ref _modsEnabledCount, value);
        }

        public string ConflictCount
        {
            get => _conflictCount;
            set => SetProperty(ref _conflictCount, value);
        }

        public int ConflictFileCount
        {
            get => _conflictFileCount;
            set => SetProperty(ref _conflictFileCount, value);
        }

        public bool HasConflicts
        {
            get => _hasConflicts;
            set => SetProperty(ref _hasConflicts, value);
        }

        public ObservableCollection<ModConflictGroup> ConflictGroups
        {
            get => _conflictGroups;
            set => SetProperty(ref _conflictGroups, value);
        }

        public bool IsUserLoggedIn
        {
            get => _isUserLoggedIn;
            set => SetProperty(ref _isUserLoggedIn, value);
        }

        public string NexusUsername
        {
            get => _nexusUsername;
            set => SetProperty(ref _nexusUsername, value);
        }

        public bool NexusIsPremium
        {
            get => _nexusIsPremium;
            set => SetProperty(ref _nexusIsPremium, value);
        }

        public bool SortByLoadOrder
        {
            get => _sortMode == SortMode.LoadOrder;
            private set => SetProperty(ref _sortByLoadOrder, value);
        }

        public string SortButtonText
        {
            get => _sortButtonText;
            set => SetProperty(ref _sortButtonText, value);
        }

        public string GamePath
        {
            get => _gamePath;
            private set => SetProperty(ref _gamePath, value);
        }

        public string ModFolder
        {
            get => _modFolder;
            private set => SetProperty(ref _modFolder, value);
        }

        /// <summary>
        /// Feature 11: Aktuell ausgewählte Spiel-Installation mit allen Pfaden
        /// </summary>
        public GameInstallDescriptor? SelectedGameInstall
        {
            get => _selectedGameInstall;
            private set
            {
                if (SetProperty(ref _selectedGameInstall, value))
                {
                    // ModFolder aktualisieren, wenn sich die Installation ändert
                    if (value != null)
                    {
                        ModFolder = value.ModsPath;
                        GamePath = value.ExecutablePath;
                    }
                }
            }
        }

        public string MenuSettingsText
        {
            get => _menuSettingsText;
            set => SetProperty(ref _menuSettingsText, value);
        }

        public string MenuLoginText
        {
            get => _menuLoginText;
            set => SetProperty(ref _menuLoginText, value);
        }

        public string MenuLogoutText
        {
            get => _menuLogoutText;
            set => SetProperty(ref _menuLogoutText, value);
        }

        public string ButtonStartGameText
        {
            get => _buttonStartGameText;
            set => SetProperty(ref _buttonStartGameText, value);
        }

        public string ButtonAddModText
        {
            get => _buttonAddModText;
            set => SetProperty(ref _buttonAddModText, value);
        }

        public string ButtonReloadModsText
        {
            get => _buttonReloadModsText;
            set => SetProperty(ref _buttonReloadModsText, value);
        }

        public bool AllowWorkshopModActions
        {
            get => _allowWorkshopModActions;
            private set => SetProperty(ref _allowWorkshopModActions, value);
        }

        public string WorkshopBadgeText
        {
            get => _workshopBadgeText;
            set => SetProperty(ref _workshopBadgeText, value);
        }

        public string WorkshopBadgeTooltip
        {
            get => _workshopBadgeTooltip;
            set => SetProperty(ref _workshopBadgeTooltip, value);
        }

        public string WorkshopConflictNote
        {
            get => _workshopConflictNote;
            set => SetProperty(ref _workshopConflictNote, value);
        }

        public string ContextMenuVisitWebsiteText
        {
            get => _contextMenuVisitWebsiteText;
            set => SetProperty(ref _contextMenuVisitWebsiteText, value);
        }

        public string ContextMenuChangeModNameText
        {
            get => _contextMenuChangeModNameText;
            set => SetProperty(ref _contextMenuChangeModNameText, value);
        }

        public string ContextMenuChangeNoteText
        {
            get => _contextMenuChangeNoteText;
            set => SetProperty(ref _contextMenuChangeNoteText, value);
        }

        public string ContextMenuAssignCategoryText
        {
            get => _contextMenuAssignCategoryText;
            set => SetProperty(ref _contextMenuAssignCategoryText, value);
        }

        public string ContextMenuClearCategoryText
        {
            get => _contextMenuClearCategoryText;
            set => SetProperty(ref _contextMenuClearCategoryText, value);
        }

        public string ContextMenuToggleSeparatorText
        {
            get => _contextMenuToggleSeparatorText;
            set => SetProperty(ref _contextMenuToggleSeparatorText, value);
        }

        public string ContextMenuEndorseModText
        {
            get => _contextMenuEndorseModText;
            set => SetProperty(ref _contextMenuEndorseModText, value);
        }

        public string ContextMenuChangeModNumberText
        {
            get => _contextMenuChangeModNumberText;
            set => SetProperty(ref _contextMenuChangeModNumberText, value);
        }

        public string ContextMenuToggleUpdateCheckText
        {
            get => _contextMenuToggleUpdateCheckText;
            set => SetProperty(ref _contextMenuToggleUpdateCheckText, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Gibt an, ob Mods gerade geladen werden (für Loading-Animation)
        /// </summary>
        public bool IsLoadingMods
        {
            get => _isLoadingMods;
            set => SetProperty(ref _isLoadingMods, value);
        }

        public string ProfilesText
        {
            get => _profilesText;
            set => SetProperty(ref _profilesText, value);
        }

        public string CreateProfileText
        {
            get => _createProfileText;
            set => SetProperty(ref _createProfileText, value);
        }

        public string DuplicateProfileText
        {
            get => _duplicateProfileText;
            set => SetProperty(ref _duplicateProfileText, value);
        }

        public string DeleteProfileText
        {
            get => _deleteProfileText;
            set => SetProperty(ref _deleteProfileText, value);
        }

        public string SaveProfileText
        {
            get => _saveProfileText;
            set => SetProperty(ref _saveProfileText, value);
        }

        public string ManageCategoriesText
        {
            get => _manageCategoriesText;
            set => SetProperty(ref _manageCategoriesText, value);
        }

        public string EnableAllModsText
        {
            get => _enableAllModsText;
            set => SetProperty(ref _enableAllModsText, value);
        }

        public string DisableAllModsText
        {
            get => _disableAllModsText;
            set => SetProperty(ref _disableAllModsText, value);
        }

        public string UpdateManifestText
        {
            get => _updateManifestText;
            set => SetProperty(ref _updateManifestText, value);
        }

        public string UpdateAllManifestsText
        {
            get => _updateAllManifestsText;
            set => SetProperty(ref _updateAllManifestsText, value);
        }

        public string SwitchGameText
        {
            get => _switchGameText;
            set => SetProperty(ref _switchGameText, value);
        }

        public string GameText
        {
            get => _gameText;
            set => SetProperty(ref _gameText, value);
        }

        public string ConflictsButtonText
        {
            get => _conflictsButtonText;
            set => SetProperty(ref _conflictsButtonText, value);
        }

        public string LoadingModsText
        {
            get => _loadingModsText;
            set => SetProperty(ref _loadingModsText, value);
        }

        public ObservableCollection<ModCategory> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<CategoryFilterOption> CategoryFilterOptions
        {
            get => _categoryFilterOptions;
            private set => SetProperty(ref _categoryFilterOptions, value);
        }

        public string? SelectedCategoryFilterId
        {
            get => _selectedCategoryFilterId;
            set
            {
                if (SetProperty(ref _selectedCategoryFilterId, value))
                {
                    ApplySearchFilter();
                }
            }
        }

        public bool HasCategories
        {
            get => _hasCategories;
            private set => SetProperty(ref _hasCategories, value);
        }

        public string UncategorizedText
        {
            get => _uncategorizedText;
            set => SetProperty(ref _uncategorizedText, value);
        }

        public string FilterCategoryText
        {
            get => _filterCategoryText;
            set => SetProperty(ref _filterCategoryText, value);
        }

        public string FilterAllText
        {
            get => _filterAllText;
            set => SetProperty(ref _filterAllText, value);
        }

        public string FilterUncategorizedText
        {
            get => _filterUncategorizedText;
            set => SetProperty(ref _filterUncategorizedText, value);
        }

        public string ContextMenuOverrideVersionText
        {
            get => _contextMenuOverrideVersionText;
            set => SetProperty(ref _contextMenuOverrideVersionText, value);
        }

        public string ContextMenuCheckForUpdateText
        {
            get => _contextMenuCheckForUpdateText;
            set => SetProperty(ref _contextMenuCheckForUpdateText, value);
        }

        public IAppSettings Settings => _settings;

        // Commands
        public ICommand OpenSettingsCommand { get; private set; } = null!;
        public ICommand LoginCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand AddModCommand { get; private set; } = null!;
        public ICommand ReloadCommand { get; private set; } = null!;
        public ICommand StartGameCommand { get; private set; } = null!;
        public ICommand SortCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand UpdateModCommand { get; private set; } = null!;
        public ICommand MoveUpCommand { get; private set; } = null!;
        public ICommand MoveDownCommand { get; private set; } = null!;
        public ICommand DeleteModCommand { get; private set; } = null!;
        public ICommand OpenFolderCommand { get; private set; } = null!;
        public ICommand OpenModPageCommand { get; private set; } = null!;
        public ICommand ChangeModNameCommand { get; private set; } = null!;
        public ICommand ChangeModNoteCommand { get; private set; } = null!;
        public ICommand AssignCategoryCommand { get; private set; } = null!;
        public ICommand ClearCategoryCommand { get; private set; } = null!;
        public ICommand ToggleSeparatorCommand { get; private set; } = null!;
        public ICommand EndorseModCommand { get; private set; } = null!;
        public ICommand ChangeModNumberCommand { get; private set; } = null!;
        public ICommand ToggleUpdateCheckCommand { get; private set; } = null!;
        public ICommand CheckForUpdateCommand { get; private set; } = null!;
        public ICommand ModCheckBoxCommand { get; private set; } = null!;
        public ICommand SetModVersionCommand { get; private set; } = null!;
        public ICommand EnableAllModsCommand { get; private set; } = null!;
        public ICommand DisableAllModsCommand { get; private set; } = null!;
        public ICommand CreateProfileCommand { get; private set; } = null!;
        public ICommand LoadProfileCommand { get; private set; } = null!;
        public ICommand DeleteProfileCommand { get; private set; } = null!;
        public ICommand SaveCurrentProfileCommand { get; private set; } = null!;
        public ICommand SwitchGameCommand { get; private set; } = null!;
        public ICommand UpdateManifestCommand { get; private set; } = null!;
        public ICommand UpdateAllManifestsCommand { get; private set; } = null!;
        public ICommand DuplicateProfileCommand { get; private set; } = null!;
        public ICommand OpenConflictCheckerCommand { get; private set; } = null!;
        public ICommand OpenCategoryManagerCommand { get; private set; } = null!;

        public void SetModsView(ICollectionView view)
        {
            _modsView = view;
        }

        private void CheckAndLoadGamePath()
        {
            GamePath = _settings.GamePath;

            if (!string.IsNullOrWhiteSpace(GamePath))
            {
                if (!_fileService.GetExtension(GamePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    _dialogService.ShowMessageBox(Resources.Messages.ErrorInvalidFileType, Resources.Messages.DialogTitleSecurityError, MessageBoxButton.OK, MessageBoxImage.Error);
                    GamePath = string.Empty;
                    _settings.GamePath = "";
                    _settings.Save();
                }
            }

            if (string.IsNullOrWhiteSpace(GamePath))
            {
                // Feature 11: Versuche Standard-Pfade basierend auf ausgewähltem Spiel
                string? defaultPath = null;
                if (SelectedGame == GameType.KCD1 && _gameInstallService.KCD1Install != null)
                {
                    defaultPath = _gameInstallService.KCD1Install.ExecutablePath;
                }
                else if (SelectedGame == GameType.KCD2)
                {
                    if (_gameInstallService.KCD2Install != null)
                    {
                        defaultPath = _gameInstallService.KCD2Install.ExecutablePath;
                    }
                    else if (_fileService.FileExists(DefaultGamePath))
                    {
                        defaultPath = DefaultGamePath;
                    }
                }

                if (!string.IsNullOrWhiteSpace(defaultPath))
                {
                    GamePath = defaultPath;
                    _settings.GamePath = GamePath;
                    _settings.Save();
                }
                else
                {
                    _dialogService.ShowMessageBox(Resources.Messages.ErrorGamePathRequired, Resources.Messages.DialogTitleGamePathRequired, MessageBoxButton.OK, MessageBoxImage.Information);
                    string? selectedPath = _dialogService.ShowOpenFileDialog("Game Executable (*.exe)|*.exe", $"Select {SelectedGameDisplay} Executable");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        GamePath = selectedPath;
                        _settings.GamePath = GamePath;
                        _settings.Save();
                    }
                    else
                    {
                        _dialogService.ShowMessageBox(Resources.Messages.ErrorGamePathRequiredExit, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
            }

            if (!_fileService.FileExists(GamePath))
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorInvalidPath, Resources.Messages.DialogTitleInvalidPath, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // ModFolder basierend auf ausgewähltem Spiel setzen
            string? gameDir = _fileService.GetDirectoryName(GamePath);
            if (!string.IsNullOrEmpty(gameDir))
            {
                string? parentDir = _fileService.GetDirectoryName(gameDir);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    ModFolder = _fileService.Combine(parentDir, "Mods");
                }
            }
        }

        private async Task LoadModsAsync()
        {
            try
            {
                IsLoadingMods = true;
                var mods = await _modInstallerService.LoadModsAsync(_modFolder);
                Mods.Clear();
                
                // Feature 2: Merge user mod data with detected versions
                var userModData = await _userModDataService.LoadUserModDataAsync();
                
                foreach (var mod in mods)
                {
                    // Merge user data with detected version
                    if (userModData.TryGetValue(mod.Id, out var userData))
                    {
                        // Verwende benutzerdefinierte Version, falls vorhanden
                        if (!string.IsNullOrEmpty(userData.CustomVersion))
                        {
                            mod.Version = userData.CustomVersion;
                        }
                        else
                        {
                            // Aktualisiere erkannte Version und merge
                            mod.Version = await _userModDataService.MergeVersionAsync(mod.Id, mod.Version);
                        }
                        
                        // Benutzerdefinierte Notizen übernehmen, falls vorhanden
                        if (!string.IsNullOrEmpty(userData.CustomNote))
                        {
                            mod.Note = userData.CustomNote;
                        }

                        // Benutzerdefinierte Anzeigenamen nur verwenden, wenn kein File-Renaming aktiv ist
                        if (!_settings.EnableFileRenaming && !string.IsNullOrEmpty(userData.CustomName))
                        {
                            mod.Name = userData.CustomName;
                        }
                    }
                    else
                    {
                        // Neue Mods: Speichere erkannte Version
                        await _userModDataService.MergeVersionAsync(mod.Id, mod.Version);
                    }
                    
                    Mods.Add(mod);
                }

                // WICHTIG: Metadaten werden jetzt bereits in LoadModsAsync geladen und gemappt
                // Diese globalen Dictionaries werden für Notizen und Update-Checks verwendet
                _modVersions = await _modInstallerService.LoadModVersionsAsync(_modFolder);
                _modNotes = await _modInstallerService.LoadModNotesAsync(_modFolder);
                
                // Stelle sicher, dass Notizen auf Mods gemappt sind (falls noch nicht geschehen)
                foreach (var mod in Mods)
                {
                    // WICHTIG: Verwende _modNotes als primäre Quelle, überschreibe mod.Note nur wenn leer
                    if (_modNotes.TryGetValue(mod.Id, out var note) && !string.IsNullOrEmpty(note))
                    {
                        mod.Note = note;
                    }
                }
                
                _logger.Info($"Mod Notes geladen: {_modNotes.Count} Notizen für {Mods.Count} Mods");

                await ApplyCategoryAssignmentsAsync();

                await RefreshConflictsAsync();
                UpdateModsEnabledCount();
                ApplySearchFilter();
                
                // Starte Update-Prüfung im Hintergrund (nur wenn aktiviert)
                if (_settings.EnableUpdateNotifications)
                {
                    _ = Task.Run(async () => await CheckForModUpdatesAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Fehler beim Laden der Mods", ex);
                StatusText = Resources.Messages.ErrorFailedToLoadMods;
            }
            finally
            {
                IsLoadingMods = false;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            await _categoryService.LoadAsync();
            SyncCategoriesFromService();
        }

        private void SyncCategoriesFromService()
        {
            Categories = new ObservableCollection<ModCategory>(
                _categoryService.Categories.OrderBy(c => c.Order));
            HasCategories = Categories.Any();
            UpdateCategoryNames();
            if (_sortMode == SortMode.Category && !HasCategories)
            {
                SetSortMode(SortMode.Title);
            }

            UpdateCategoryFilterOptions();

            _ = ClearMissingCategoryAssignmentsAsync();
        }

        private async Task ApplyCategoryAssignmentsAsync()
        {
            var assignments = await _categoryAssignmentService.LoadCategoryAssignmentsAsync(Mods.ToList());
            var validIds = new HashSet<string>(Categories.Select(c => c.Id));
            foreach (var mod in Mods)
            {
                if (assignments.TryGetValue(mod.Id, out var categoryId) && !string.IsNullOrWhiteSpace(categoryId))
                {
                    if (validIds.Contains(categoryId))
                    {
                        mod.CategoryId = categoryId;
                        mod.CategoryName = GetCategoryNameById(categoryId) ?? string.Empty;
                    }
                    else
                    {
                        await _categoryAssignmentService.ClearCategoryAsync(mod);
                        mod.CategoryId = string.Empty;
                        mod.CategoryName = string.Empty;
                    }
                }
                else
                {
                    mod.CategoryId = string.Empty;
                    mod.CategoryName = string.Empty;
                }

                if (!mod.IsWorkshopMod)
                {
                    bool isWorkshop = await _categoryAssignmentService.GetWorkshopFlagAsync(mod);
                    if (isWorkshop)
                    {
                        mod.IsWorkshopMod = true;
                        _logger.Info($"Workshop-Mod erkannt (Meta): {mod.Name}");
                    }
                }
            }
        }

        private void UpdateCategoryNames()
        {
            if (!Mods.Any())
            {
                return;
            }

            var lookup = Categories.ToDictionary(c => c.Id, c => c.Name);
            foreach (var mod in Mods)
            {
                mod.CategoryName = lookup.TryGetValue(mod.CategoryId, out var name) ? name : string.Empty;
            }

            if (_sortMode == SortMode.Category)
            {
                ApplySort();
            }
        }

        private void UpdateCategoryFilterOptions()
        {
            var options = new List<CategoryFilterOption>
            {
                new CategoryFilterOption { Id = null, Name = FilterAllText },
                new CategoryFilterOption { Id = string.Empty, Name = FilterUncategorizedText }
            };

            options.AddRange(Categories.Select(c => new CategoryFilterOption
            {
                Id = c.Id,
                Name = c.Name
            }));

            CategoryFilterOptions = new ObservableCollection<CategoryFilterOption>(options);

            if (!HasCategories)
            {
                SelectedCategoryFilterId = null;
            }
        }

        private async Task ClearMissingCategoryAssignmentsAsync()
        {
            if (!Mods.Any())
            {
                return;
            }

            var validIds = new HashSet<string>(Categories.Select(c => c.Id));
            foreach (var mod in Mods)
            {
                if (!string.IsNullOrWhiteSpace(mod.CategoryId) && !validIds.Contains(mod.CategoryId))
                {
                    await _categoryAssignmentService.ClearCategoryAsync(mod);
                    mod.CategoryId = string.Empty;
                    mod.CategoryName = string.Empty;
                }
            }
        }

        private string? GetCategoryNameById(string? categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return null;
            }

            return Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
        }

        private void UpdateModsEnabledCount()
        {
            int enabledCount = Mods.Count(mod => mod.IsEnabled);
            ModsEnabledCount = string.Format(Resources.Strings.ModsEnabled, enabledCount);
        }

        private void ApplySearchFilter()
        {
            if (_modsView == null) return;

            string filter = SearchText.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (_savedSortMode == null)
                {
                    _savedSortMode = _sortMode;
                }
                SortByLoadOrder = false;
                SortButtonText = Resources.Strings.ButtonSortDisabled;
            }
            else
            {
                if (_savedSortMode != null)
                {
                    SetSortMode(_savedSortMode.Value);
                    _savedSortMode = null;
                }
            }

            _modsView.Filter = (item) =>
            {
                if (item is Mod mod)
                {
                    bool matchesText = mod.Name.ToLowerInvariant().Contains(filter) ||
                                       mod.Version.ToLowerInvariant().Contains(filter);

                    if (!matchesText)
                    {
                        return false;
                    }

                    if (SelectedCategoryFilterId == null)
                    {
                        return true;
                    }

                    if (SelectedCategoryFilterId == string.Empty)
                    {
                        return string.IsNullOrWhiteSpace(mod.CategoryId);
                    }

                    return string.Equals(mod.CategoryId, SelectedCategoryFilterId, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            };
            _modsView.Refresh();
        }

        private void OpenSettings()
        {
            // Wird vom View behandelt, da es ein Window erstellt
        }

        private async Task LoginAsync()
        {
            bool success = await _nexusService.StartNexusSSOAsync();
            if (success)
            {
                IsUserLoggedIn = _nexusService.IsUserLoggedIn();
                NexusUsername = _settings.NexusUsername;
                NexusIsPremium = _settings.NexusIsPremium;
                StatusText = string.Format(Resources.Messages.InfoLoggedInAs, _settings.NexusUsername);
                
                // WICHTIG: Aktualisiere Update-Status nach Login
                await CheckForModUpdatesAsync();
            }
        }

        private void Logout()
        {
            _settings.NexusUserToken = string.Empty;
            _settings.NexusUsername = string.Empty;
            _settings.NexusUserEmail = string.Empty;
            _settings.NexusUserID = 0;
            _settings.NexusIsPremium = false;
            _settings.Save();
            IsUserLoggedIn = false;
            NexusUsername = string.Empty;
            NexusIsPremium = false;
            StatusText = Resources.Messages.InfoNotLoggedIn;
        }

        private async Task AddModAsync()
        {
            string? filePath = _dialogService.ShowOpenFileDialog("Mod Files (*.rar;*.7z;*.zip)|*.rar;*.7z;*.zip", "Select Mod File", true);
            if (string.IsNullOrEmpty(filePath)) return;

            StatusText = "Installing mod...";
            var mod = await _modInstallerService.ProcessModFileAsync(filePath, _modFolder);
            if (mod != null)
            {
                Mods.Add(mod);
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
                StatusText = string.Format(Resources.Messages.StatusModAdded, mod.Name, mod.Version);
                await RefreshConflictsAsync();
                await CheckForModUpdatesAsync();
            }
            else
            {
                StatusText = Resources.Messages.StatusModInstallFailed;
            }
        }

        private async Task ReloadModsAsync()
        {
            await LoadCategoriesAsync();
            await LoadModsAsync();
        }

        private void StartGame()
        {
            if (!_fileService.FileExists(GamePath))
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorInvalidExecutable, Resources.Messages.DialogTitleInvalidPath, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_fileService.GetExtension(GamePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorSecurityBlocked, Resources.Messages.DialogTitleSecurityBlocked, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Feature 8: Immer erlauben, das Spiel zu starten, auch ohne aktive Mods
            var enabledMods = Mods.Where(mod => mod.IsEnabled).ToList();
            
            StatusText = Resources.Strings.StatusStartingGame;
            string extraArgs = string.IsNullOrWhiteSpace(_settings.GameLaunchArgs) ? "" : _settings.GameLaunchArgs + " ";

            if (_settings.IsDevMode)
            {
                extraArgs += "-devmode ";
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = GamePath,
                Arguments = extraArgs
            };

            try
            {
                Process.Start(processInfo);
                StatusText = Resources.Strings.StatusGameStarted;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Starten des Spiels: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("FailedToStartGame") ?? "Failed to start the game: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = Resources.Strings.StatusFailedToStartGame;
            }
        }

        private void ToggleSort()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorSortingDisabled, Resources.Messages.DialogTitleActionNotAllowed, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SortMode nextMode = _sortMode switch
            {
                SortMode.LoadOrder => SortMode.Title,
                SortMode.Title => HasCategories ? SortMode.Category : SortMode.LoadOrder,
                SortMode.Category => SortMode.LoadOrder,
                _ => SortMode.LoadOrder
            };

            SetSortMode(nextMode);
        }

        private void SetSortMode(SortMode mode)
        {
            if (mode == SortMode.Category && !HasCategories)
            {
                mode = SortMode.Title;
            }

            _sortMode = mode;
            if (mode != SortMode.Category)
            {
                _secondarySortMode = mode;
            }

            SortByLoadOrder = mode == SortMode.LoadOrder;
            UpdateSortButtonText();

            if (mode == SortMode.LoadOrder)
            {
                _ = LoadModsAsync();
                return;
            }

            ApplySort();
        }

        private void ApplySort()
        {
            if (_sortMode == SortMode.Title)
            {
                var sorted = Mods.OrderBy(m => m.Name).ToList();
                Mods.Clear();
                foreach (var mod in sorted)
                {
                    Mods.Add(mod);
                }
                return;
            }

            if (_sortMode == SortMode.Category)
            {
                var categoryOrder = Categories
                    .OrderBy(c => c.Order)
                    .Select((c, index) => new { c.Id, Order = index })
                    .ToDictionary(x => x.Id, x => x.Order);

                var sorted = Mods
                    .OrderBy(m => categoryOrder.TryGetValue(m.CategoryId, out var order) ? order : int.MaxValue)
                    .ThenBy(m => _secondarySortMode == SortMode.LoadOrder ? m.Number : int.MaxValue)
                    .ThenBy(m => _secondarySortMode == SortMode.Title ? m.Name : string.Empty)
                    .ToList();

                Mods.Clear();
                foreach (var mod in sorted)
                {
                    Mods.Add(mod);
                }
            }
        }

        private void UpdateSortButtonText()
        {
            SortButtonText = _sortMode switch
            {
                SortMode.LoadOrder => Resources.Strings.ButtonSortLoadOrder,
                SortMode.Title => Resources.Strings.ButtonSortTitle,
                SortMode.Category => Resources.Strings.ResourceManager.GetString("ButtonSortCategory") ?? "Sort: Category",
                _ => Resources.Strings.ButtonSortLoadOrder
            };
        }

        private void ClearSearch()
        {
            SearchText = "";
        }

        /// <summary>
        /// Aktualisiert alle lokalisierten Strings basierend auf der aktuellen Sprache
        /// </summary>
        private void UpdateLocalizedStrings()
        {
            WindowTitle = Resources.Strings.WindowTitle;
            MenuSettingsText = Resources.Strings.MenuSettings;
            MenuLoginText = Resources.Strings.MenuLogin;
            MenuLogoutText = Resources.Strings.MenuLogout;
            ButtonStartGameText = Resources.Strings.ButtonStartGame;
            ButtonAddModText = Resources.Strings.ButtonAddMod;
            ButtonReloadModsText = Resources.Strings.ButtonReloadMods;
            WorkshopBadgeText = Resources.Strings.ResourceManager.GetString("WorkshopBadgeText") ?? "Workshop";
            WorkshopBadgeTooltip = Resources.Strings.ResourceManager.GetString("WorkshopBadgeTooltip") ?? "Installed from Steam Workshop";
            WorkshopConflictNote = Resources.Strings.ResourceManager.GetString("WorkshopConflictNote") ?? "Workshop files may be managed by Steam.";
            ContextMenuVisitWebsiteText = Resources.Strings.ContextMenuVisitWebsite;
            ContextMenuChangeModNameText = Resources.Strings.ContextMenuChangeModName;
            ContextMenuChangeNoteText = Resources.Strings.ContextMenuChangeNote;
            ContextMenuAssignCategoryText = Resources.Strings.ResourceManager.GetString("ContextMenuAssignCategory") ?? "Assign category...";
            ContextMenuClearCategoryText = Resources.Strings.ResourceManager.GetString("ContextMenuClearCategory") ?? "Clear category";
            ContextMenuToggleSeparatorText = Resources.Strings.ResourceManager.GetString("ContextMenuToggleSeparator") ?? "Toggle Separator";
            ContextMenuEndorseModText = Resources.Strings.ContextMenuEndorseMod;
            ContextMenuChangeModNumberText = Resources.Strings.ContextMenuChangeModNumber;
            ContextMenuToggleUpdateCheckText = Resources.Strings.ContextMenuToggleUpdateCheck;
            ContextMenuCheckForUpdateText = Resources.Strings.ResourceManager.GetString("ContextMenuCheckForUpdate") ?? "Check for Update";
            ContextMenuOverrideVersionText = Resources.Strings.ResourceManager.GetString("ContextMenuOverrideVersion") ?? "Override Version";
            
            // WICHTIG: Game-Switch-Buttons aktualisieren (ohne App-Neustart)
            SwitchGameText = Resources.Strings.ResourceManager.GetString("SwitchGameText") ?? "Switch Game";
            GameText = Resources.Strings.ResourceManager.GetString("GameText") ?? "Game:";
            ConflictsButtonText = Resources.Strings.ResourceManager.GetString("ConflictsButtonText") ?? "Conflicts";
            ProfilesText = Resources.Strings.ResourceManager.GetString("ProfilesText") ?? "Profile:";
            CreateProfileText = Resources.Strings.ResourceManager.GetString("CreateProfileText") ?? "Create";
            DuplicateProfileText = Resources.Strings.ResourceManager.GetString("DuplicateProfileText") ?? "Duplicate";
            DeleteProfileText = Resources.Strings.ResourceManager.GetString("DeleteProfileText") ?? "Delete";
            SaveProfileText = Resources.Strings.ResourceManager.GetString("SaveProfileText") ?? "Save";
            ManageCategoriesText = Resources.Strings.ResourceManager.GetString("ManageCategoriesText") ?? "Manage Categories";
            EnableAllModsText = Resources.Strings.ResourceManager.GetString("EnableAllModsText") ?? "Enable All";
            DisableAllModsText = Resources.Strings.ResourceManager.GetString("DisableAllModsText") ?? "Disable All";
            UpdateManifestText = Resources.Strings.ResourceManager.GetString("UpdateManifestText") ?? "Update Manifest (Selected)";
            UpdateAllManifestsText = Resources.Strings.ResourceManager.GetString("UpdateAllManifestsText") ?? "Update All Manifests";
            LoadingModsText = Resources.Strings.ResourceManager.GetString("LoadingModsText") ?? "Loading Mods...";
            UncategorizedText = Resources.Strings.ResourceManager.GetString("UncategorizedText") ?? "Uncategorized";
            FilterCategoryText = Resources.Strings.ResourceManager.GetString("FilterCategoryText") ?? "Category:";
            FilterAllText = Resources.Strings.ResourceManager.GetString("FilterCategoryAll") ?? "All";
            FilterUncategorizedText = Resources.Strings.ResourceManager.GetString("FilterCategoryUncategorized") ?? "Uncategorized";
            UpdateCategoryFilterOptions();
            
            // Status-Texts aktualisieren (nur wenn noch auf Standard)
            if (StatusText == Resources.Strings.StatusReady || string.IsNullOrEmpty(StatusText))
            {
                StatusText = Resources.Strings.StatusReady;
            }
            
            // Sort-Button-Text aktualisieren
            UpdateSortButtonText();
            
            // Counts aktualisieren
            UpdateModsEnabledCount();
            UpdateConflictCount();
        }

        private void UpdateConflictCount()
        {
            ConflictCount = string.Format(Resources.Strings.ConflictsDetected, ConflictFileCount);
        }

        private bool IsWorkshopActionAllowed(Mod mod, string actionName)
        {
            if (mod.IsWorkshopMod && !_settings.AllowWorkshopModActions)
            {
                _logger.Info($"Workshop action blocked: {actionName} for {mod.Name}");
                return false;
            }

            return true;
        }

        private async Task RefreshConflictsAsync()
        {
            try
            {
                var conflicts = await _conflictCheckerService.AnalyzeConflictsAsync(Mods.ToList());
                ConflictGroups = new ObservableCollection<ModConflictGroup>(conflicts);
                ConflictFileCount = conflicts.Count;
                HasConflicts = conflicts.Count > 0;
                UpdateConflictCount();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Konflikt-Check: {ex.Message}", ex);
            }
        }

        private void OpenConflictChecker()
        {
            if (!HasConflicts)
            {
                return;
            }

            var window = _serviceProvider.GetRequiredService<Views.ConflictCheckerWindow>();
            if (window.DataContext is ConflictCheckerViewModel viewModel)
            {
                viewModel.SetConflicts(ConflictGroups);
            }

            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void OpenCategoryManager()
        {
            var window = _serviceProvider.GetRequiredService<Views.CategoryManagerWindow>();
            if (window.DataContext is ViewModels.CategoryManagerViewModel viewModel)
            {
                viewModel.Initialize(Categories.ToList(), Mods.ToList());
            }

            window.Owner = Application.Current.MainWindow;
            bool? result = window.ShowDialog();
            if (result == true)
            {
                SyncCategoriesFromService();
            }
        }

        public async Task ProcessModFileAsync(string filePath)
        {
            StatusText = "Installing mod...";
            var mod = await _modInstallerService.ProcessModFileAsync(filePath, _modFolder);
            if (mod != null)
            {
                Mods.Add(mod);
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
                StatusText = string.Format(Resources.Messages.StatusModAdded, mod.Name, mod.Version);
                await CheckForModUpdatesAsync();
            }
            else
            {
                StatusText = Resources.Messages.StatusModInstallFailed;
            }
        }

        public async Task ProcessModFolderAsync(string folderPath)
        {
            StatusText = Resources.Messages.StatusInstallingModFromFolder;
            var mod = await _modInstallerService.ProcessModFolderAsync(folderPath, _modFolder);
            if (mod != null)
            {
                Mods.Add(mod);
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
                StatusText = string.Format(Resources.Messages.StatusModAdded, mod.Name, mod.Version);
                await RefreshConflictsAsync();
            }
            else
            {
                StatusText = Resources.Messages.StatusModInstallFailed;
            }
        }

        /// <summary>
        /// Führt ein Update für einen Mod durch
        /// WICHTIG: Premium-Benutzer erhalten automatische Installation, Non-Premium öffnet Web-Seite
        /// </summary>
        private async Task UpdateModAsync(Mod? mod)
        {
            if (mod == null || mod.ModNumber <= 0)
            {
                _dialogService.ShowMessageBox(
                    Resources.Strings.ResourceManager.GetString("UpdateCheckNoModNumber") ?? "Mod number not set. Cannot update.",
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusText = Resources.Strings.ResourceManager.GetString("UpdateCheckInProgress") ?? "Checking for updates...";
                
                // Bestimme Game Domain basierend auf ausgewähltem Spiel
                string gameDomain = SelectedGame == Services.GameType.KCD1 
                    ? "kingdomcomedeliverance" 
                    : "kingdomcomedeliverance2";
                
                string? apiKey = _settings.NexusUserToken;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _dialogService.ShowMessageBox(
                        Resources.Strings.ResourceManager.GetString("ErrorNexusApiKeyMissing") ?? "Nexus API Key not configured. Please log in to Nexus Mods first.",
                        Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    StatusText = Resources.Strings.StatusReady;
                    return;
                }

                // Hole Mod-Dateien von NexusMods API
                var filesResponse = await _nexusService.GetModFilesAsync(gameDomain, mod.ModNumber, apiKey);
                if (filesResponse == null)
                {
                    _dialogService.ShowMessageBox(
                        string.Format(
                            Resources.Strings.ResourceManager.GetString("UpdateCheckError") ?? "Error checking for updates: {0}",
                            "Failed to retrieve mod files from NexusMods"),
                        Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusText = Resources.Strings.StatusReady;
                    return;
                }

                // Finde die neueste Datei
                Models.NexusModFile? latestFile = null;
                if (filesResponse.files != null && filesResponse.files.Count > 0)
                {
                    latestFile = filesResponse.files
                        .OrderByDescending(f => f.uploaded_timestamp)
                        .FirstOrDefault();
                }

                if (latestFile == null)
                {
                    _dialogService.ShowMessageBox(
                        Resources.Strings.ResourceManager.GetString("UpdateCheckNoUpdate") ?? "No update files found.",
                        Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    StatusText = Resources.Strings.StatusReady;
                    return;
                }

                // Prüfe Premium-Status
                bool isPremium = _settings.NexusIsPremium;
                
                if (isPremium)
                {
                    // Premium: Führe automatische Installation durch
                    StatusText = Resources.Strings.ResourceManager.GetString("UpdateInstalling") ?? "Installing update...";
                    
                    // WICHTIG: Verwende PerformPremiumUpdateAsync für Premium-Download
                    string? tempFilePath = await _nexusService.PerformPremiumUpdateAsync(gameDomain, mod.ModNumber, latestFile.file_id, apiKey);
                    if (string.IsNullOrEmpty(tempFilePath))
                    {
                        _dialogService.ShowMessageBox(
                            Resources.Strings.ResourceManager.GetString("UpdateDownloadLinkFailed") ?? "Failed to download update. You may need to download manually.",
                            Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        StatusText = Resources.Strings.StatusReady;
                        return;
                    }

                    // Installiere Update über ModInstallerService
                    var updatedMod = await _modInstallerService.ProcessModUpdateAsync(tempFilePath, mod.Path, mod.Id);
                    
                        if (updatedMod != null)
                        {
                            // WICHTIG: UI-Updates müssen auf dem UI-Thread erfolgen
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Aktualisiere Mod in der Liste
                                int index = Mods.IndexOf(mod);
                                if (index >= 0)
                                {
                                    Mods[index] = updatedMod;
                                    updatedMod.HasUpdate = false;
                                    updatedMod.LatestVersion = string.Empty;
                                }
                            });

                        // WICHTIG: Speichere Mod-Order nach erfolgreichem Update
                        // Dies stellt sicher, dass die Reihenfolge nach Neustart erhalten bleibt
                        await SaveModOrderAsync();

                        _logger.Info($"Update erfolgreich installiert für Mod {mod.Name}");
                        _dialogService.ShowMessageBox(
                            string.Format(
                                Resources.Strings.ResourceManager.GetString("UpdateInstalledSuccess") ?? "Update successfully installed for {0}.",
                                mod.Name),
                            Resources.Strings.ResourceManager.GetString("UpdateAvailableTitle") ?? "Update Available",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        StatusText = Resources.Strings.StatusReady;
                    }
                    else
                    {
                        _dialogService.ShowMessageBox(
                            Resources.Strings.ResourceManager.GetString("UpdateInstallFailed") ?? "Failed to install update. Please try again or download manually.",
                            Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        StatusText = Resources.Strings.StatusReady;
                    }

                    // Lösche temporäre Datei
                    try
                    {
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            System.IO.File.Delete(tempFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Konnte temporäre Datei nicht löschen: {ex.Message}");
                    }
                }
                else
                {
                    // Non-Premium: Öffne Mod-Seite im Browser
                    string url = $"https://www.nexusmods.com/{gameDomain}/mods/{mod.ModNumber}";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    StatusText = Resources.Strings.StatusReady;
                    _logger.Info($"Non-Premium-Benutzer: Öffne Mod-Seite für Update: {url}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Update für Mod {mod.Name}: {ex.Message}", ex);
                _dialogService.ShowMessageBox(
                    string.Format(
                        Resources.Strings.ResourceManager.GetString("UpdateCheckError") ?? "Error during update: {0}",
                        ex.Message),
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusText = Resources.Strings.StatusReady;
            }
        }

        /// <summary>
        /// Aktualisiert die Number-Eigenschaft aller Mods basierend auf der aktuellen Collection-Reihenfolge
        /// WICHTIG: Wird nach jeder Neusortierung aufgerufen, um die Load Order zu synchronisieren
        /// </summary>
        private void UpdateModNumbersFromCollectionOrder()
        {
            for (int i = 0; i < Mods.Count; i++)
            {
                Mods[i].Number = i + 1;
            }
        }

        /// <summary>
        /// Speichert die aktuelle Mod-Reihenfolge
        /// WICHTIG: Speichert sowohl im Profil als auch im Spiel-Mods-Verzeichnis
        /// </summary>
        public async Task SaveModOrderAsync()
        {
            // WICHTIG: Number-Eigenschaften aktualisieren, bevor gespeichert wird
            UpdateModNumbersFromCollectionOrder();

            if (SelectedProfile != null)
            {
                // Wenn ein Profil aktiv ist, speichere in Profil und Spiel-Mods-Verzeichnis
                await SaveCurrentProfileAsync();
            }
            else
            {
                // Wenn kein Profil aktiv ist, speichere nur im Spiel-Mods-Verzeichnis
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
            }
        }

        /// <summary>
        /// Verschiebt eine Mod nach oben in der Load Order
        /// WICHTIG: 
        /// - Aktualisiert Number-Eigenschaften nach dem Verschieben
        /// - Speichert sofort in Profil und Spiel-Mods-Verzeichnis
        /// - UI wird automatisch aktualisiert
        /// </summary>
        private async void MoveUp(Mod? mod)
        {
            if (mod == null || !SortByLoadOrder) return;
            int index = Mods.IndexOf(mod);
            if (index > 0)
            {
                Mods.Move(index, index - 1);
                
                // WICHTIG: Number-Eigenschaften aktualisieren, damit die Reihenfolge korrekt gespeichert wird
                UpdateModNumbersFromCollectionOrder();
                
                // WICHTIG: Sofort speichern (Profil + Spiel-Mods-Verzeichnis)
                await SaveModOrderAsync();
            }
        }

        /// <summary>
        /// Verschiebt eine Mod nach unten in der Load Order
        /// WICHTIG: 
        /// - Aktualisiert Number-Eigenschaften nach dem Verschieben
        /// - Speichert sofort in Profil und Spiel-Mods-Verzeichnis
        /// - UI wird automatisch aktualisiert
        /// </summary>
        private async void MoveDown(Mod? mod)
        {
            if (mod == null || !SortByLoadOrder) return;
            int index = Mods.IndexOf(mod);
            if (index < Mods.Count - 1)
            {
                Mods.Move(index, index + 1);
                
                // WICHTIG: Number-Eigenschaften aktualisieren, damit die Reihenfolge korrekt gespeichert wird
                UpdateModNumbersFromCollectionOrder();
                
                // WICHTIG: Sofort speichern (Profil + Spiel-Mods-Verzeichnis)
                await SaveModOrderAsync();
            }
        }

        private async Task DeleteModAsync(Mod? mod)
        {
            if (mod == null) return;

            if (_settings.AskOnDelete)
            {
                bool dontAskAgain;
                bool? result = _dialogService.ShowDeleteConfirmation(out dontAskAgain);
                if (result != true)
                    return;

                if (dontAskAgain)
                {
                    _settings.AskOnDelete = false;
                    _settings.Save();
                }
            }

            try
            {
                if (_fileService.DirectoryExists(mod.Path))
                {
                    _fileService.DeleteDirectory(mod.Path, true);
                }

                Mods.Remove(mod);
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
                StatusText = string.Format(Resources.Messages.StatusModDeleted, mod.Name);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Löschen des Mods: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("FailedToDeleteMod") ?? "Failed to delete mod: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(Mod? mod)
        {
            if (mod == null || string.IsNullOrEmpty(mod.Path) || !_fileService.DirectoryExists(mod.Path))
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorModFolderNotExists, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start("explorer.exe", mod.Path);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Öffnen des Ordners: {ex.Message}", ex);
                _dialogService.ShowMessageBox(string.Format(Resources.Messages.ErrorOpeningFolder, ex.Message), Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenModPage(Mod? mod)
        {
            if (mod == null || mod.ModNumber <= 0)
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorModNumberMissing, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsWorkshopActionAllowed(mod, "VisitWebsite"))
            {
                return;
            }

            // WICHTIG: Verwende korrekte Game Domain basierend auf ausgewähltem Spiel
            string gameDomain = SelectedGame == Services.GameType.KCD1 
                ? "kingdomcomedeliverance" 
                : "kingdomcomedeliverance2";
            string url = $"https://www.nexusmods.com/{gameDomain}/mods/{mod.ModNumber}";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private async Task ChangeModNameAsync(Mod? mod)
        {
            if (mod == null) return;
            string? newName = _dialogService.ShowInputDialog(
                Resources.Strings.ResourceManager.GetString("EnterModName") ?? "Enter mod name:",
                Resources.Strings.ResourceManager.GetString("ChangeModNameTitle") ?? "Change Mod Name",
                mod.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;

            newName = newName.Trim();
            if (string.Equals(newName, mod.Name, StringComparison.Ordinal))
            {
                return;
            }

            if (!_settings.EnableFileRenaming)
            {
                mod.Name = newName;
                await _userModDataService.SaveModDataAsync(mod.Id, customName: newName);
                StatusText = string.Format(Resources.Strings.ResourceManager.GetString("ModNameUpdated") ?? "Mod name updated: {0}", newName);
                return;
            }

            string oldModId = mod.Id;
            string newModId = _manifestService.GenerateModId(newName);
            string manifestPath = _fileService.Combine(mod.Path, "mod.manifest");

            bool updated = await _manifestService.UpdateManifestNameAndIdAsync(manifestPath, newName, newModId);
            if (!updated)
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorFailedToUpdateManifest, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string newPath = _fileService.Combine(_modFolder, newModId);
            string finalPath = mod.Path;
            if (!mod.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (_fileService.DirectoryExists(newPath))
                    {
                        _dialogService.ShowMessageBox(Resources.Messages.ErrorModFolderExists, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _fileService.MoveDirectory(mod.Path, newPath);
                    finalPath = newPath;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler beim Umbenennen des Mod-Ordners: {ex.Message}", ex);
                    _dialogService.ShowMessageBox(string.Format(Resources.Messages.ErrorRenamingModFolder, ex.Message), Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            mod.Name = newName;
            mod.Id = newModId;
            mod.Path = finalPath;

            await UpdateModIdReferencesAsync(oldModId, newModId);

            if (SelectedProfile != null)
            {
                await SaveCurrentProfileAsync();
            }
            else
            {
                await _modInstallerService.SaveModOrderAsync(Mods.ToList(), _modFolder);
            }

            StatusText = string.Format(Resources.Strings.ResourceManager.GetString("ModNameUpdated") ?? "Mod name updated: {0}", newName);
        }

        private async Task UpdateModIdReferencesAsync(string oldModId, string newModId)
        {
            if (string.Equals(oldModId, newModId, StringComparison.Ordinal))
            {
                return;
            }

            if (_modNotes.TryGetValue(oldModId, out var note))
            {
                _modNotes.Remove(oldModId);
                _modNotes[newModId] = note;
                await _modInstallerService.SaveModNotesAsync(_modNotes, _modFolder);
            }

            if (_modVersions.TryGetValue(oldModId, out var versionInfo))
            {
                _modVersions.Remove(oldModId);
                _modVersions[newModId] = versionInfo;
                await SaveModVersionsDictionaryAsync();
            }

            var userData = await _userModDataService.LoadUserModDataAsync();
            if (userData.TryGetValue(oldModId, out var modData))
            {
                userData.Remove(oldModId);
                modData.ModId = newModId;
                userData[newModId] = modData;
                await _userModDataService.SaveUserModDataAsync(userData);
            }

            foreach (var profile in Profiles)
            {
                bool changed = false;
                if (profile.ActiveMods.Remove(oldModId))
                {
                    profile.ActiveMods.Add(newModId);
                    changed = true;
                }

                if (profile.LoadOrder.Remove(oldModId))
                {
                    profile.LoadOrder.Add(newModId);
                    changed = true;
                }

                if (changed)
                {
                    await _profilesService.SaveProfileAsync(SelectedGame, profile);
                }
            }
        }

        private async Task ChangeModNoteAsync(Mod? mod)
        {
            if (mod == null) return;
            
            // WICHTIG: Verwende mod.Note als primäre Quelle, fallback zu _modNotes
            string currentNote = !string.IsNullOrEmpty(mod.Note) 
                ? mod.Note 
                : (_modNotes.ContainsKey(mod.Id) ? _modNotes[mod.Id] : "");
            
            _logger.Info($"ChangeModNote: Mod={mod.Name}, CurrentNote length={currentNote?.Length ?? 0}");
            
            // WICHTIG: isMultiline=true für Mod Notes
            string? newNote = _dialogService.ShowInputDialog(
                Resources.Messages.PromptEnterNote, 
                Resources.Messages.TitleChangeModNote, 
                currentNote,
                isMultiline: true);
            
            if (newNote == null) return;

            mod.Note = newNote;
            _modNotes[mod.Id] = newNote;
            await _modInstallerService.SaveModNotesAsync(_modNotes, _modFolder);
            
            _logger.Info($"Mod Note gespeichert: Mod={mod.Name}, Note length={newNote.Length}");
        }

        private async Task AssignCategoryAsync(Mod? mod)
        {
            if (mod == null) return;
            if (!Categories.Any())
            {
                await _categoryService.LoadAsync();
                SyncCategoriesFromService();
            }

            var dialog = _serviceProvider.GetRequiredService<Views.CategoryAssignDialog>();
            if (dialog.DataContext is ViewModels.CategoryAssignDialogViewModel viewModel)
            {
                string? selectedId = string.IsNullOrWhiteSpace(mod.CategoryId) ? null : mod.CategoryId;
                viewModel.Initialize(mod, Categories.ToList(), selectedId);
            }

            dialog.Owner = Application.Current.MainWindow;
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            if (dialog.DataContext is ViewModels.CategoryAssignDialogViewModel assignViewModel)
            {
                string? selectedCategoryId = assignViewModel.SelectedCategoryId;
                await _categoryAssignmentService.SetCategoryIdAsync(mod, selectedCategoryId);
                mod.CategoryId = selectedCategoryId ?? string.Empty;
                mod.CategoryName = GetCategoryNameById(selectedCategoryId) ?? string.Empty;
                if (_sortMode == SortMode.Category)
                {
                    ApplySort();
                }
            }
        }

        private async Task ClearCategoryAsync(Mod? mod)
        {
            if (mod == null) return;
            await _categoryAssignmentService.ClearCategoryAsync(mod);
            mod.CategoryId = string.Empty;
            mod.CategoryName = string.Empty;
            if (_sortMode == SortMode.Category)
            {
                ApplySort();
            }
        }

        private async Task ToggleSeparatorAsync(Mod? mod)
        {
            if (mod == null || mod.IsWorkshopMod) return;
            mod.HasSeparatorAfter = !mod.HasSeparatorAfter;
            await SaveCurrentProfileAsync();
        }

        /// <summary>
        /// Feature 3: Manuelle Versionsüberschreibung für einen Mod
        /// </summary>
        private async Task SetModVersionAsync(Mod? mod)
        {
            if (mod == null) return;
            if (!IsWorkshopActionAllowed(mod, "OverrideVersion"))
            {
                return;
            }

            // Dialog mit aktueller Version vorausgefüllt
            string? customVersion = _dialogService.ShowInputDialog(
                Resources.Strings.ResourceManager.GetString("EnterModVersion") ?? "Enter custom version (leave empty to use detected version):",
                Resources.Strings.ResourceManager.GetString("SetModVersionTitle") ?? "Set Mod Version",
                mod.Version);

            if (customVersion == null) return; // Benutzer hat abgebrochen

            // Speichere benutzerdefinierte Version
            await _userModDataService.SaveModDataAsync(
                mod.Id,
                customVersion: string.IsNullOrWhiteSpace(customVersion) ? null : customVersion,
                detectedVersion: mod.Version);

            // Aktualisiere Mod-Version in der UI
            if (!string.IsNullOrWhiteSpace(customVersion))
            {
                mod.Version = customVersion;
            }

            _logger.Info($"Benutzerdefinierte Version für Mod {mod.Name} gesetzt: {customVersion}");
            StatusText = $"Version für {mod.Name} auf {customVersion} gesetzt";
        }

        private async Task EndorseModAsync(Mod? mod)
        {
            if (mod == null || mod.ModNumber <= 0) return;
            if (!IsWorkshopActionAllowed(mod, "EndorseMod"))
            {
                return;
            }
            // WICHTIG: Verwende korrekte Game Domain basierend auf ausgewähltem Spiel
            string gameDomain = SelectedGame == Services.GameType.KCD1 
                ? "kingdomcomedeliverance" 
                : "kingdomcomedeliverance2";
            bool success = await _nexusService.EndorseModAsync(gameDomain, mod.ModNumber, _settings.NexusUserToken);
            // WICHTIG: Nur bei Fehlern einen Dialog anzeigen, bei Erfolg nichts anzeigen
            if (!success)
            {
                _dialogService.ShowMessageBox(Resources.Messages.ErrorEndorseFailed, Resources.Messages.DialogTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Bei Erfolg: Kein Dialog, stille Bestätigung
        }

        private async Task ChangeModNumberAsync(Mod? mod)
        {
            if (mod == null) return;
            if (!IsWorkshopActionAllowed(mod, "ChangeModNumber"))
            {
                return;
            }
            string? input = _dialogService.ShowInputDialog(Resources.Messages.PromptChangeModNumber, Resources.Messages.TitleChangeModNumber, mod.ModNumber > 0 ? mod.ModNumber.ToString() : "");
            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int newModNumber)) return;

            mod.ModNumber = newModNumber;
            // Mod-Version speichern...
            _logger.Info($"Mod-Nummer geändert: {mod.ModNumber}");
        }

        /// <summary>
        /// Schaltet Update-Checks für einen Mod um
        /// WICHTIG: Speichert die Änderung in mod_versions.json
        /// </summary>
        private async Task ToggleUpdateCheckAsync(Mod? mod)
        {
            if (mod == null) return;
            if (!IsWorkshopActionAllowed(mod, "ToggleUpdateCheck"))
            {
                return;
            }
            
            try
            {
                // Toggle UpdateChecksEnabled
                mod.UpdateChecksEnabled = !mod.UpdateChecksEnabled;
                
                // Aktualisiere Metadaten
                if (_modVersions.TryGetValue(mod.Id, out var versionInfo))
                {
                    versionInfo.UpdateChecksEnabled = mod.UpdateChecksEnabled;
                }
                else
                {
                    // Erstelle neuen Eintrag, falls nicht vorhanden
                    _modVersions[mod.Id] = new Models.ModVersionInfo
                    {
                        Version = mod.Version,
                        ModNumber = mod.ModNumber,
                        FileName = string.Empty,
                        UpdateChecksEnabled = mod.UpdateChecksEnabled
                    };
                }
                
                // Speichere Metadaten
                await SaveModVersionsDictionaryAsync();
                
                _logger.Info($"Update-Check für Mod {mod.Name} umgeschaltet: {mod.UpdateChecksEnabled}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Umschalten des Update-Checks für Mod {mod.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Speichert die gesamte ModVersions-Dictionary in mod_versions.json
        /// </summary>
        private async Task SaveModVersionsDictionaryAsync()
        {
            try
            {
                // Verwende ModInstallerService zum Speichern
                // Da SaveModVersionAsync nur einen Eintrag speichert, müssen wir alle durchgehen
                foreach (var kvp in _modVersions)
                {
                    // WICHTIG: Speichere UpdateChecksEnabled korrekt
                    await _modInstallerService.SaveModVersionAsync(
                        kvp.Key,
                        kvp.Value.Version,
                        kvp.Value.ModNumber,
                        kvp.Value.FileName,
                        _modFolder,
                        kvp.Value.UpdateChecksEnabled);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern der Mod-Versionen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Mod aktiviert/deaktiviert wird
        /// WICHTIG: Auto-Save des aktuellen Profils
        /// </summary>
        private async void ModCheckBoxChanged(Mod? mod)
        {
            if (mod == null) return;
            
            // Auto-Save: Speichere aktuelles Profil
            if (SelectedProfile != null)
            {
                await SaveCurrentProfileAsync();
            }
            
            UpdateModsEnabledCount();
        }

        private async Task CheckForUpdateAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== CheckForUpdateAsync GESTARTET ===");
            Console.WriteLine("=== CheckForUpdateAsync GESTARTET ===");
            _logger.Info("CheckForUpdateAsync gestartet");
            
            if (!_settings.EnableUpdateNotifications)
            {
                System.Diagnostics.Debug.WriteLine("Update-Benachrichtigungen sind DEAKTIVIERT");
                Console.WriteLine("Update-Benachrichtigungen sind DEAKTIVIERT");
                _logger.Info("Update-Benachrichtigungen sind deaktiviert - Update-Check übersprungen");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Aktuelle Manager-Version: {currentManagerVersion}");
                Console.WriteLine($"Aktuelle Manager-Version: {currentManagerVersion}");
                _logger.Info($"Aktuelle Manager-Version: {currentManagerVersion}");
                
                // GetLatestVersionAsync verwendet jetzt GitHub API (Parameter wird ignoriert)
                string? latestVersion = await _nexusService.GetLatestVersionAsync(string.Empty);
                
                if (latestVersion == null)
                {
                    System.Diagnostics.Debug.WriteLine("FEHLER: Keine Online-Version gefunden!");
                    Console.WriteLine("FEHLER: Keine Online-Version gefunden!");
                    _logger.Warning("Keine Online-Version gefunden - Update-Check abgebrochen");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Online-Version empfangen: '{latestVersion}'");
                Console.WriteLine($"Online-Version empfangen: '{latestVersion}'");
                _logger.Info($"Online-Version empfangen: '{latestVersion}'");
                
                if (!Version.TryParse(currentManagerVersion, out Version currentVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"FEHLER: Konnte aktuelle Version '{currentManagerVersion}' nicht parsen");
                    Console.WriteLine($"FEHLER: Konnte aktuelle Version '{currentManagerVersion}' nicht parsen");
                    _logger.Error($"Konnte aktuelle Version '{currentManagerVersion}' nicht parsen");
                    return;
                }
                
                if (!Version.TryParse(latestVersion, out Version onlineVersion))
                {
                    System.Diagnostics.Debug.WriteLine($"FEHLER: Konnte Online-Version '{latestVersion}' nicht parsen");
                    Console.WriteLine($"FEHLER: Konnte Online-Version '{latestVersion}' nicht parsen");
                    _logger.Error($"Konnte Online-Version '{latestVersion}' nicht parsen");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Version-Vergleich: Online={onlineVersion}, Aktuell={currentVersion}, Online > Aktuell = {onlineVersion > currentVersion}");
                Console.WriteLine($"Version-Vergleich: Online={onlineVersion}, Aktuell={currentVersion}, Online > Aktuell = {onlineVersion > currentVersion}");
                _logger.Info($"Version-Vergleich: Online={onlineVersion}, Aktuell={currentVersion}");
                
                if (onlineVersion > currentVersion)
                {
                    System.Diagnostics.Debug.WriteLine("*** NEUE VERSION VERFÜGBAR! Zeige Dialog... ***");
                    Console.WriteLine("*** NEUE VERSION VERFÜGBAR! Zeige Dialog... ***");
                    _logger.Info($"Neue Version verfügbar! Zeige Update-Dialog...");
                    
                    // Load localized strings
                    string? updateTitle = Resources.Strings.ResourceManager.GetString("UpdateAvailableTitle");
                    string? updateMessageTemplate = Resources.Strings.ResourceManager.GetString("UpdateAvailableMessage");
                    
                    if (string.IsNullOrEmpty(updateTitle))
                    {
                        updateTitle = "Update Available";
                        _logger.Warning("UpdateAvailableTitle nicht in Resources gefunden - verwende Fallback");
                    }
                    
                    if (string.IsNullOrEmpty(updateMessageTemplate))
                    {
                        updateMessageTemplate = "A new version ({0}) is available. Do you want to update?";
                        _logger.Warning("UpdateAvailableMessage nicht in Resources gefunden - verwende Fallback");
                    }
                    
                    string updateMessage = string.Format(updateMessageTemplate, latestVersion);
                    
                    _logger.Info($"Update-Dialog wird angezeigt: Title='{updateTitle}', Message='{updateMessage}'");
                    
                    // WICHTIG: MessageBox muss auf UI-Thread aufgerufen werden
                    bool? result = null;
                    if (Application.Current?.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                result = _dialogService.ShowMessageBox(
                                    updateMessage,
                                    updateTitle,
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Information);
                                _logger.Info($"Update-Dialog Ergebnis: {result}");
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Fehler beim Anzeigen des Update-Dialogs: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        // Fallback: Direkter Aufruf (sollte nicht passieren, aber sicherheitshalber)
                        _logger.Warning("Application.Current.Dispatcher ist null - verwende Fallback");
                        result = _dialogService.ShowMessageBox(
                            updateMessage,
                            updateTitle,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                    }
                    
                    if (result == true)
                    {
                        _logger.Info("Benutzer hat Update bestätigt - öffne NexusMods-Seite");
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "https://www.nexusmods.com/kingdomcomedeliverance2/mods/187",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Fehler beim Öffnen der NexusMods-Seite: {ex.Message}", ex);
                        }
                    }
                    else
                    {
                        _logger.Info("Benutzer hat Update-Dialog abgebrochen");
                    }
                }
                else
                {
                    _logger.Info($"Keine neue Version verfügbar. Online-Version ({onlineVersion}) ist nicht neuer als aktuelle Version ({currentVersion})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEHLER in CheckForUpdateAsync: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"FEHLER in CheckForUpdateAsync: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                _logger.Error($"Fehler beim Prüfen auf Updates: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Prüft auf Updates für einen einzelnen Mod (manuell)
        /// WICHTIG: Verwendet ModNumber aus Metadaten, um NexusMods API aufzurufen
        /// </summary>
        private async Task CheckForModUpdateAsync(Mod? mod)
        {
            if (mod == null || mod.ModNumber <= 0)
            {
                _dialogService.ShowMessageBox(
                    Resources.Strings.ResourceManager.GetString("UpdateCheckNoModNumber") ?? "Mod number not set. Cannot check for updates.",
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsWorkshopActionAllowed(mod, "CheckForUpdate"))
            {
                return;
            }

            try
            {
                StatusText = Resources.Strings.ResourceManager.GetString("UpdateCheckInProgress") ?? "Checking for updates...";
                
                // Bestimme Game Domain basierend auf ausgewähltem Spiel
                string gameDomain = SelectedGame == Services.GameType.KCD1 
                    ? "kingdomcomedeliverance" 
                    : "kingdomcomedeliverance2";
                
                string? apiKey = _settings.NexusUserToken;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _dialogService.ShowMessageBox(
                        "Nexus API Key not configured. Please log in to Nexus Mods first.",
                        Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    StatusText = Resources.Strings.StatusReady;
                    return;
                }

                // Hole Mod-Dateien von NexusMods API
                var filesResponse = await _nexusService.GetModFilesAsync(gameDomain, mod.ModNumber, apiKey);
                if (filesResponse == null)
                {
                    _dialogService.ShowMessageBox(
                        string.Format(
                            Resources.Strings.ResourceManager.GetString("UpdateCheckError") ?? "Error checking for updates: {0}",
                            "Failed to retrieve mod files from NexusMods"),
                        Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusText = Resources.Strings.StatusReady;
                    return;
                }

                // Finde die neueste Version
                string? latestVersion = null;
                if (filesResponse.files != null && filesResponse.files.Count > 0)
                {
                    // Sortiere nach uploaded_timestamp (neueste zuerst)
                    var latestFile = filesResponse.files
                        .OrderByDescending(f => f.uploaded_timestamp)
                        .FirstOrDefault();
                    
                    if (latestFile != null && !string.IsNullOrEmpty(latestFile.mod_version))
                    {
                        latestVersion = latestFile.mod_version;
                    }
                    else if (latestFile != null && !string.IsNullOrEmpty(latestFile.version))
                    {
                        latestVersion = latestFile.version;
                    }
                }

                // Vergleiche Versionen
                if (!string.IsNullOrEmpty(latestVersion) && !string.IsNullOrEmpty(mod.Version))
                {
                    if (System.Version.TryParse(latestVersion, out var onlineVersion) &&
                        System.Version.TryParse(mod.Version, out var currentVersion))
                    {
                        if (onlineVersion > currentVersion)
                        {
                            mod.HasUpdate = true;
                            mod.LatestVersion = latestVersion;
                            _logger.Info($"Update gefunden für Mod {mod.Name}: {mod.Version} -> {latestVersion}");
                            
                            // WICHTIG: Check Update zeigt nur die Information an - öffnet KEINE Web-Seite
                            string updateMessage = string.Format(
                                Resources.Strings.ResourceManager.GetString("UpdateAvailableMessage") ?? "A new version ({0}) is available for {1}.",
                                latestVersion, mod.Name);
                            _dialogService.ShowMessageBox(
                                updateMessage,
                                Resources.Strings.ResourceManager.GetString("UpdateAvailableTitle") ?? "Update Available",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            mod.HasUpdate = false;
                            mod.LatestVersion = string.Empty;
                            _dialogService.ShowMessageBox(
                                string.Format(
                                    Resources.Strings.ResourceManager.GetString("UpdateCheckNoUpdate") ?? "No update available. Current version: {0}",
                                    mod.Version),
                                Resources.Strings.ResourceManager.GetString("UpdateAvailableTitle") ?? "Update Available",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    _dialogService.ShowMessageBox(
                        Resources.Strings.ResourceManager.GetString("UpdateCheckNoUpdate") ?? "No update available. Current version: {0}",
                        Resources.Strings.ResourceManager.GetString("UpdateAvailableTitle") ?? "Update Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                StatusText = Resources.Strings.StatusReady;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim manuellen Prüfen von Updates für Mod {mod.Name} (ModNumber: {mod.ModNumber}): {ex.Message}", ex);
                _dialogService.ShowMessageBox(
                    string.Format(
                        Resources.Strings.ResourceManager.GetString("UpdateCheckError") ?? "Error checking for updates: {0}",
                        ex.Message),
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusText = Resources.Strings.StatusReady;
            }
        }

        /// <summary>
        /// Prüft auf Updates für alle Mods, die Update-Checks aktiviert haben
        /// WICHTIG: Verwendet ModNumber aus Metadaten, um NexusMods API aufzurufen
        /// </summary>
        private async Task CheckForModUpdatesAsync()
        {
            if (!_settings.EnableUpdateNotifications)
                return;

            try
            {
                _logger.Info("Starte Update-Prüfung für Mods...");
                
                // Lade aktuelle Metadaten
                _modVersions = await _modInstallerService.LoadModVersionsAsync(_modFolder);
                
                // Bestimme Game Domain basierend auf ausgewähltem Spiel
                string gameDomain = SelectedGame == Services.GameType.KCD1 
                    ? "kingdomcomedeliverance" 
                    : "kingdomcomedeliverance2";
                
                string? apiKey = _settings.NexusUserToken;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.Warning("Kein Nexus API Key vorhanden - Update-Prüfung übersprungen");
                    return;
                }

                int checkedCount = 0;
                int updateCount = 0;

                // Prüfe jeden Mod auf Updates
                foreach (var mod in Mods)
                {
                    // Überspringe Mods ohne ModNumber oder mit deaktivierten Update-Checks
                    if (mod.ModNumber <= 0 || !mod.UpdateChecksEnabled)
                        continue;

                    try
                    {
                        // Hole Mod-Dateien von NexusMods API
                        var filesResponse = await _nexusService.GetModFilesAsync(gameDomain, mod.ModNumber, apiKey);
                        if (filesResponse == null)
                            continue;

                        checkedCount++;

                        // Finde die neueste Version
                        string? latestVersion = null;
                        if (filesResponse.files != null && filesResponse.files.Count > 0)
                        {
                            // Sortiere nach uploaded_timestamp (neueste zuerst)
                            var latestFile = filesResponse.files
                                .OrderByDescending(f => f.uploaded_timestamp)
                                .FirstOrDefault();
                            
                            if (latestFile != null && !string.IsNullOrEmpty(latestFile.mod_version))
                            {
                                latestVersion = latestFile.mod_version;
                            }
                            else if (latestFile != null && !string.IsNullOrEmpty(latestFile.version))
                            {
                                latestVersion = latestFile.version;
                            }
                        }

                        // Vergleiche Versionen
                        if (!string.IsNullOrEmpty(latestVersion) && !string.IsNullOrEmpty(mod.Version))
                        {
                            if (System.Version.TryParse(latestVersion, out var onlineVersion) &&
                                System.Version.TryParse(mod.Version, out var currentVersion))
                            {
                                if (onlineVersion > currentVersion)
                                {
                                    mod.HasUpdate = true;
                                    mod.LatestVersion = latestVersion;
                                    updateCount++;
                                    _logger.Info($"Update gefunden für Mod {mod.Name}: {mod.Version} -> {latestVersion}");
                                }
                                else
                                {
                                    mod.HasUpdate = false;
                                    mod.LatestVersion = string.Empty;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Fehler beim Prüfen von Updates für Mod {mod.Name} (ModNumber: {mod.ModNumber}): {ex.Message}", ex);
                        // Weiter mit nächstem Mod
                    }
                }

                _logger.Info($"Update-Prüfung abgeschlossen: {checkedCount} Mods geprüft, {updateCount} Updates gefunden");
                
                if (updateCount > 0)
                {
                    StatusText = string.Format(
                        Resources.Strings.ResourceManager.GetString("ModUpdatesAvailable") ?? "{0} mod update(s) available",
                        updateCount);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Fehler beim Prüfen der Mod-Updates", ex);
            }
        }

        /// <summary>
        /// Feature 5: Alle Mods aktivieren
        /// </summary>
        private async Task EnableAllModsAsync()
        {
            foreach (var mod in Mods)
            {
                mod.IsEnabled = true;
            }
            await SaveModOrderAsync();
            UpdateModsEnabledCount();
            StatusText = "All mods enabled";
        }

        /// <summary>
        /// Feature 5: Alle Mods deaktivieren
        /// </summary>
        private async Task DisableAllModsAsync()
        {
            foreach (var mod in Mods)
            {
                mod.IsEnabled = false;
            }
            await SaveModOrderAsync();
            UpdateModsEnabledCount();
            StatusText = "All mods disabled";
        }

        /// <summary>
        /// Feature 4: Profile-Properties
        /// </summary>
        public ObservableCollection<ModProfile> Profiles
        {
            get => _profiles;
            set => SetProperty(ref _profiles, value);
        }

        private bool _isLoadingProfile = false;

        /// <summary>
        /// Aktuell ausgewähltes Profil
        /// WICHTIG: Beim Ändern wird automatisch das Profil geladen und angewendet
        /// Verhindert Rekursion durch _isLoadingProfile-Flag
        /// </summary>
        public ModProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                // Wenn bereits ein Profil geladen wird, nur die Property setzen (verhindert Rekursion)
                if (_isLoadingProfile)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                    return;
                }

                if (SetProperty(ref _selectedProfile, value) && value != null)
                {
                    // WICHTIG: Lade Profil automatisch, wenn es geändert wird
                    // Dies stellt sicher, dass die UI sofort aktualisiert wird
                    LoadProfileAsync(value.ProfileName);
                }
            }
        }

        /// <summary>
        /// Feature 11: Aktuell ausgewähltes Spiel
        /// </summary>
        public GameType SelectedGame
        {
            get => _selectedGame;
            set
            {
                if (SetProperty(ref _selectedGame, value))
                {
                    SelectedGameDisplay = value == GameType.KCD1 ? "Kingdom Come: Deliverance" : "Kingdom Come: Deliverance II";
                    _settings.LastSelectedGame = value.ToString();
                    _settings.Save();
                }
            }
        }

        public string SelectedGameDisplay
        {
            get => _selectedGameDisplay;
            set => SetProperty(ref _selectedGameDisplay, value);
        }

        /// <summary>
        /// Feature 11: Verfügbare Spiele
        /// </summary>
        public IEnumerable<GameType> AvailableGames
        {
            get
            {
                var games = new List<GameType>();
                if (_gameInstallService.InstallType == GameInstallType.KCD1 || 
                    _gameInstallService.InstallType == GameInstallType.Both)
                {
                    games.Add(GameType.KCD1);
                }
                if (_gameInstallService.InstallType == GameInstallType.KCD2 || 
                    _gameInstallService.InstallType == GameInstallType.Both)
                {
                    games.Add(GameType.KCD2);
                }
                return games;
            }
        }

        /// <summary>
        /// Feature 4: Lädt alle verfügbaren Profile für das aktuell ausgewählte Spiel
        /// WICHTIG: Profile sind pro Spiel getrennt (KCD1/KCD2)
        /// </summary>
        private async Task LoadProfilesAsync()
        {
            try
            {
                var profiles = await _profilesService.LoadProfilesAsync(SelectedGame);
                Profiles.Clear();
                foreach (var profile in profiles)
                {
                    Profiles.Add(profile);
                }
                
                // Wenn keine Profile vorhanden, erstelle Default-Profil
                if (Profiles.Count == 0)
                {
                    _logger.Info($"Keine Profile für {SelectedGame} gefunden, erstelle Default-Profil");
                    var defaultProfile = await _profilesService.EnsureDefaultProfileAsync(SelectedGame, Mods.ToList());
                    Profiles.Add(defaultProfile);
                    SelectedProfile = defaultProfile;
                    ApplyProfileLayout(defaultProfile);
                    
                    // Speichere als letztes verwendetes Profil
                    if (SelectedGame == GameType.KCD1)
                    {
                        _settings.LastUsedProfile_KCD1 = defaultProfile.ProfileName;
                    }
                    else
                    {
                        _settings.LastUsedProfile_KCD2 = defaultProfile.ProfileName;
                    }
                    _settings.Save();
                }
                else
                {
                    // Lade letztes verwendetes Profil
                    string? lastProfileName = SelectedGame == GameType.KCD1 
                        ? _settings.LastUsedProfile_KCD1 
                        : _settings.LastUsedProfile_KCD2;
                    
                    if (!string.IsNullOrEmpty(lastProfileName))
                    {
                        var lastProfile = Profiles.FirstOrDefault(p => p.ProfileName == lastProfileName);
                        if (lastProfile != null)
                        {
                            SelectedProfile = lastProfile;
                            // Lade Profil sofort
                            await LoadProfileAsync(lastProfileName);
                        }
                        else
                        {
                            // Fallback: Erstes Profil auswählen
                            SelectedProfile = Profiles.First();
                            await LoadProfileAsync(SelectedProfile.ProfileName);
                        }
                    }
                    else
                    {
                        // Fallback: Erstes Profil auswählen
                        SelectedProfile = Profiles.First();
                        await LoadProfileAsync(SelectedProfile.ProfileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der Profile: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Feature 4: Erstellt ein neues Profil aus dem aktuellen Zustand
        /// WICHTIG: Profil wird für das aktuell ausgewählte Spiel erstellt
        /// </summary>
        private async Task CreateProfileAsync()
        {
            string? profileName = _dialogService.ShowInputDialog(
                Resources.Strings.ResourceManager.GetString("EnterProfileName") ?? "Enter profile name:",
                Resources.Strings.ResourceManager.GetString("CreateProfileText") ?? "Create Profile",
                "");
            if (string.IsNullOrWhiteSpace(profileName)) return;

            try
            {
                var profile = await _profilesService.CreateProfileFromCurrentStateAsync(SelectedGame, profileName, Mods.ToList());
                Profiles.Add(profile);
                SelectedProfile = profile;
                
                // Speichere als letztes verwendetes Profil
                if (SelectedGame == GameType.KCD1)
                {
                    _settings.LastUsedProfile_KCD1 = profileName;
                }
                else
                {
                    _settings.LastUsedProfile_KCD2 = profileName;
                }
                _settings.Save();
                
                // Schreibe mod_order.txt in Spiel-Mods-Verzeichnis
                await _profilesService.WriteModOrderToGameFolderAsync(_modFolder, Mods.ToList());
                
                StatusText = $"Profile '{profileName}' created";
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Erstellen des Profils: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("ErrorCreatingProfile") ?? "Error creating profile: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Feature 4: Lädt ein Profil und wendet es an
        /// WICHTIG: 
        /// - Lädt mod_order.txt aus dem Profil-Verzeichnis
        /// - Wendet Profil auf Mod-Liste an (Enabled/Disabled, Load Order)
        /// - Schreibt mod_order.txt in das Spiel-Mods-Verzeichnis
        /// - Aktualisiert UI sofort (Mods-Collection wird neu sortiert)
        /// </summary>
        private async Task LoadProfileAsync(string? profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;
            
            // Verhindere mehrfaches gleichzeitiges Laden
            if (_isLoadingProfile) return;
            _isLoadingProfile = true;

            try
            {
                var profile = await _profilesService.LoadProfileAsync(SelectedGame, profileName);
                if (profile == null)
                {
                    _dialogService.ShowMessageBox(
                        string.Format(Resources.Messages.ErrorProfileNotFound, profileName), 
                        Resources.Messages.DialogTitleError, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                    return;
                }

                // Lade mod_order.txt für dieses Profil (aus Profil-Verzeichnis)
                await _profilesService.LoadProfileModOrderAsync(SelectedGame, profileName, Mods.ToList());
                
                // Wende Profil auf Mods an (Enabled/Disabled, Load Order)
                await _profilesService.ApplyProfileAsync(profile, Mods.ToList());

                ApplyProfileLayout(profile);
                
                // WICHTIG: Schreibe mod_order.txt in das Spiel-Mods-Verzeichnis
                // Dies ist die Datei, die das Spiel tatsächlich liest
                await _profilesService.WriteModOrderToGameFolderAsync(_modFolder, Mods.ToList());
                
                // UI aktualisieren: Mods neu sortieren und Enabled-Count aktualisieren
                UpdateModsEnabledCount();
                
                // WICHTIG: Collection neu sortieren, damit UI sofort aktualisiert wird
                var sortedMods = Mods.OrderBy(m => m.Number).ToList();
                Mods.Clear();
                foreach (var mod in sortedMods)
                {
                    Mods.Add(mod);
                }
                
                // WICHTIG: Setze SelectedProfile OHNE Rekursion (verwende _isLoadingProfile-Flag)
                _selectedProfile = profile;
                OnPropertyChanged(nameof(SelectedProfile));
                
                // Speichere als letztes verwendetes Profil
                if (SelectedGame == GameType.KCD1)
                {
                    _settings.LastUsedProfile_KCD1 = profileName;
                }
                else
                {
                    _settings.LastUsedProfile_KCD2 = profileName;
                }
                _settings.Save();
                
                StatusText = $"Profile '{profileName}' loaded";
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden des Profils: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("ErrorLoadingProfile") ?? "Error loading profile: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingProfile = false;
            }
        }

        private void ApplyProfileLayout(ModProfile profile)
        {
            var separators = profile.SeparatorsAfterModIds != null
                ? new HashSet<string>(profile.SeparatorsAfterModIds)
                : new HashSet<string>();

            foreach (var mod in Mods)
            {
                if (mod.IsWorkshopMod)
                {
                    mod.HasSeparatorAfter = false;
                    continue;
                }

                mod.HasSeparatorAfter = separators.Contains(mod.Id);
            }
        }

        /// <summary>
        /// Feature 4: Löscht ein Profil
        /// WICHTIG: Profil wird für das aktuell ausgewählte Spiel gelöscht
        /// </summary>
        private async Task DeleteProfileAsync(string? profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;

            string deleteMessage = string.Format(
                Resources.Strings.ResourceManager.GetString("DeleteProfileConfirm") ?? "Delete profile '{0}'?",
                profileName);
            bool? result = _dialogService.ShowMessageBox(
                deleteMessage,
                Resources.Strings.ResourceManager.GetString("ConfirmDeleteTitle") ?? "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != true) return;

            try
            {
                await _profilesService.DeleteProfileAsync(SelectedGame, profileName);
                var profileToRemove = Profiles.FirstOrDefault(p => p.ProfileName == profileName);
                if (profileToRemove != null)
                {
                    Profiles.Remove(profileToRemove);
                }
                if (SelectedProfile?.ProfileName == profileName)
                {
                    SelectedProfile = null;
                    // Lade Default-Profil, falls vorhanden
                    var defaultProfile = Profiles.FirstOrDefault(p => p.ProfileName == "Default");
                    if (defaultProfile != null)
                    {
                        await LoadProfileAsync(defaultProfile.ProfileName);
                    }
                    else if (Profiles.Count > 0)
                    {
                        await LoadProfileAsync(Profiles.First().ProfileName);
                    }
                }
                StatusText = $"Profile '{profileName}' deleted";
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Löschen des Profils: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("ErrorDeletingProfile") ?? "Error deleting profile: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Feature 4: Speichert den aktuellen Zustand als Profil
        /// WICHTIG: 
        /// - Speichert Profil-Metadaten (profile.json)
        /// - Speichert mod_order.txt im Profil-Verzeichnis
        /// - Schreibt mod_order.txt in das Spiel-Mods-Verzeichnis
        /// - Wird automatisch bei jeder Änderung aufgerufen (Auto-Save)
        /// </summary>
        private async Task SaveCurrentProfileAsync()
        {
            if (SelectedProfile == null)
            {
                await CreateProfileAsync();
                return;
            }

            try
            {
                // WICHTIG: Number-Eigenschaften aktualisieren, damit die Reihenfolge korrekt ist
                UpdateModNumbersFromCollectionOrder();

                // Speichere Profil mit aktuellem Zustand (im Profil-Verzeichnis)
                // CreateProfileFromCurrentStateAsync verwendet mods.OrderBy(m => m.Number)
                await _profilesService.CreateProfileFromCurrentStateAsync(SelectedGame, SelectedProfile.ProfileName, Mods.ToList());
                
                // WICHTIG: Schreibe mod_order.txt in das Spiel-Mods-Verzeichnis
                // Dies ist die Datei, die das Spiel tatsächlich liest
                // Format: enabled als "modfolder", disabled als "# modfolder"
                await _profilesService.WriteModOrderToGameFolderAsync(_modFolder, Mods.ToList());
                
                StatusText = $"Profile '{SelectedProfile.ProfileName}' saved";
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern des Profils: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("ErrorSavingProfile") ?? "Error saving profile: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Feature 11: Zeigt den Spiel-Auswahl-Dialog
        /// </summary>
        private async Task ShowGameSelectionDialogAsync()
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = _serviceProvider.GetRequiredService<Views.Dialogs.GameSelectionDialog>();
                    if (dialog.ShowDialog() == true && dialog.SelectedGame.HasValue)
                    {
                        SelectedGame = dialog.SelectedGame.Value;
                        _gameInstallService.SelectedGame = dialog.SelectedGame.Value;
                        
                        // SelectedGameInstall aktualisieren
                        var selectedInstall = SelectedGame == GameType.KCD1 
                            ? _gameInstallService.KCD1Install 
                            : _gameInstallService.KCD2Install;
                        
                        if (selectedInstall != null)
                        {
                            SelectedGameInstall = selectedInstall;
                            GamePath = selectedInstall.ExecutablePath;
                            ModFolder = selectedInstall.ModsPath;
                            _settings.GamePath = GamePath;
                            _settings.LastSelectedGame = SelectedGame.ToString();
                            _settings.Save();
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Feature 11: Wechselt das ausgewählte Spiel
        /// WICHTIG: 
        /// - Speichert aktuelles Profil des alten Spiels
        /// - Lädt Profile des neuen Spiels
        /// - Lädt letztes verwendetes Profil des neuen Spiels
        /// - Schreibt mod_order.txt für das neue Profil in das Spiel-Mods-Verzeichnis
        /// </summary>
        private async Task SwitchGameAsync()
        {
            // WICHTIG: Speichere aktuelles Profil des alten Spiels vor dem Wechsel
            if (SelectedProfile != null)
            {
                await SaveCurrentProfileAsync();
            }
            
            await ShowGameSelectionDialogAsync();
            
            // Spiel gewechselt - SelectedGameInstall aktualisieren
            var newInstall = SelectedGame == GameType.KCD1 
                ? _gameInstallService.KCD1Install 
                : _gameInstallService.KCD2Install;
            
            if (newInstall == null)
            {
                _dialogService.ShowMessageBox(
                    Resources.Messages.ErrorGameInstallationNotFound, 
                    Resources.Messages.DialogTitleError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }
            
            SelectedGameInstall = newInstall;
            GamePath = newInstall.ExecutablePath;
            ModFolder = newInstall.ModsPath;
            
            // Einstellungen speichern
            _settings.GamePath = GamePath;
            _settings.LastSelectedGame = SelectedGame.ToString();
            _settings.Save();
            
            // Mods neu laden für neues Spiel
            IsLoadingMods = true;
            try
            {
                await LoadModsAsync();
                
                // WICHTIG: Profile für das neue Spiel laden
                // LoadProfilesAsync erstellt automatisch Default-Profil, falls keines existiert
                // und lädt das letzte verwendete Profil
                await LoadProfilesAsync();
            }
            finally
            {
                IsLoadingMods = false;
            }
            
            StatusText = $"Switched to {SelectedGameDisplay}";
        }

        /// <summary>
        /// Feature 6: Aktualisiert gameVersion im Manifest eines Mods
        /// </summary>
        private async Task UpdateManifestAsync(Mod? mod)
        {
            if (mod == null) return;

            string? gameVersion = await _gameInstallService.GetGameVersionAsync(SelectedGame);
            if (string.IsNullOrEmpty(gameVersion))
            {
                _dialogService.ShowMessageBox(
                    Resources.Messages.ErrorGameVersion, 
                    Resources.Messages.DialogTitleError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            string manifestPath = _fileService.Combine(mod.Path, "mod.manifest");
            bool success = await _manifestUpdateService.UpdateManifestGameVersionAsync(manifestPath, gameVersion);
            
            if (success)
            {
                StatusText = $"Updated manifest for {mod.Name}";
                mod.GameVersion = gameVersion;
                mod.IsCompatible = true; // Nach Update sollte kompatibel sein
            }
            else
            {
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("FailedToUpdateManifest") ?? "Failed to update manifest for {0}",
                    mod.Name);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Feature 6: Aktualisiert gameVersion in allen Manifesten
        /// </summary>
        private async Task UpdateAllManifestsAsync()
        {
            string? gameVersion = await _gameInstallService.GetGameVersionAsync(SelectedGame);
            if (string.IsNullOrEmpty(gameVersion))
            {
                _dialogService.ShowMessageBox(
                    Resources.Messages.ErrorGameVersion, 
                    Resources.Messages.DialogTitleError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            string updateAllMessage = string.Format(
                Resources.Strings.ResourceManager.GetString("UpdateAllManifestsMessage") ?? "Update all manifests to game version {0}?",
                gameVersion);
            bool? result = _dialogService.ShowMessageBox(
                updateAllMessage,
                Resources.Strings.ResourceManager.GetString("ConfirmUpdateTitle") ?? "Confirm Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != true) return;

            StatusText = "Updating manifests...";
            int updatedCount = await _manifestUpdateService.UpdateAllManifestsGameVersionAsync(_modFolder, gameVersion);
            
            // Mods neu laden um Kompatibilität zu aktualisieren
            await LoadModsAsync();
            
            StatusText = $"Updated {updatedCount} manifests";
            string successMsg = string.Format(
                Resources.Strings.ResourceManager.GetString("UpdatedManifestsCount") ?? "Updated {0} manifests",
                updatedCount);
            _dialogService.ShowMessageBox(successMsg, 
                Resources.Strings.ResourceManager.GetString("SuccessTitle") ?? "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Feature 4: Dupliziert ein Profil
        /// </summary>
        /// <summary>
        /// Dupliziert ein Profil
        /// WICHTIG: Profil wird für das aktuell ausgewählte Spiel dupliziert
        /// </summary>
        private async Task DuplicateProfileAsync(string? profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;

            string? newName = _dialogService.ShowInputDialog(
                Resources.Strings.ResourceManager.GetString("EnterDuplicateProfileName") ?? "Enter name for duplicated profile:",
                Resources.Strings.ResourceManager.GetString("DuplicateProfileTitle") ?? "Duplicate Profile",
                $"{profileName}_copy");
            if (string.IsNullOrWhiteSpace(newName)) return;

            try
            {
                var originalProfile = await _profilesService.LoadProfileAsync(SelectedGame, profileName);
                if (originalProfile == null)
                {
                    _dialogService.ShowMessageBox(
                        string.Format(Resources.Messages.ErrorProfileNotFound, profileName), 
                        Resources.Messages.DialogTitleError, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                    return;
                }

                var newProfile = new ModProfile
                {
                    ProfileName = newName,
                    ActiveMods = new List<string>(originalProfile.ActiveMods),
                    LoadOrder = new List<string>(originalProfile.LoadOrder)
                };

                await _profilesService.SaveProfileAsync(SelectedGame, newProfile);
                
                // Kopiere auch mod_order.txt
                await _profilesService.LoadProfileModOrderAsync(SelectedGame, profileName, Mods.ToList());
                await _profilesService.SaveProfileModOrderAsync(SelectedGame, newName, Mods.ToList());
                
                await LoadProfilesAsync();
                SelectedProfile = newProfile;
                StatusText = $"Profile '{newName}' created";
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Duplizieren des Profils: {ex.Message}", ex);
                string errorMsg = string.Format(
                    Resources.Strings.ResourceManager.GetString("ErrorDuplicatingProfile") ?? "Error duplicating profile: {0}",
                    ex.Message);
                _dialogService.ShowMessageBox(errorMsg, 
                    Resources.Strings.ResourceManager.GetString("ErrorTitle") ?? "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

