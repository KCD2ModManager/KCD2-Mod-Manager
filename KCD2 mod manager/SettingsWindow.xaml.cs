using System.Windows;
using System.Windows.Media;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsWindowViewModel? _viewModel;

        public SettingsWindow(SettingsWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.ThemeChanged += (s, e) => UpdateTheme();
            _viewModel.LanguageChanged += (s, e) => OnLanguageChanged();
            UpdateTheme();
            CheckAndLoadGamePath();
        }

        private void OnLanguageChanged()
        {
            // Sprache wurde geändert - UI wird dynamisch aktualisiert
            // Kein Neustart mehr nötig, da alle Texte über Bindings aktualisiert werden
        }

        private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Ignoriere Initialisierung - nur echte Änderungen behandeln
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0) return;
            
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedValue is string languageCode)
            {
                // Prüfe, ob sich die Sprache wirklich geändert hat
                string currentLanguage = _viewModel?.Settings.Language ?? "en";
                if (currentLanguage != languageCode)
                {
                    _viewModel?.ChangeLanguageCommand.Execute(languageCode);
                }
            }
        }

        public event EventHandler? ThemeChanged;

        /// <summary>
        /// Aktualisiert das Theme für SettingsWindow
        /// WICHTIG: Verwendet ThemeService zum Laden der Theme-Dictionaries
        /// </summary>
        private void UpdateTheme()
        {
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            if (serviceProvider != null)
            {
                var themeService = serviceProvider.GetService(typeof(IThemeService)) as IThemeService;
                if (themeService != null)
                {
                    themeService.ApplyTheme(this.Resources, themeService.IsDarkMode);
                    this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                }
            }
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CheckAndLoadGamePath()
        {
            if (_viewModel == null) return;

            string gamePath = _viewModel.Settings.GamePath;

            if (!string.IsNullOrWhiteSpace(gamePath))
            {
                if (!System.IO.Path.GetExtension(gamePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Invalid file type in settings. Only .exe files are allowed.", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _viewModel.Settings.GamePath = "";
                    _viewModel.Settings.Save();
                }
            }

            // WICHTIG: Game Path wird jetzt automatisch über GameInstallService ermittelt
            // Kein manuelles Setting mehr nötig

            if (!string.IsNullOrWhiteSpace(_viewModel.Settings.GamePath) && !System.IO.File.Exists(_viewModel.Settings.GamePath))
            {
                MessageBox.Show(KCD2_mod_manager.Resources.Messages.ErrorInvalidPath, KCD2_mod_manager.Resources.Messages.DialogTitleInvalidPath, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetMaxBackups_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.SetMaxBackupsCommand.Execute(null);
        }

        private void ToggleDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDarkModeCommand.Execute(true);
            }
        }

        private void ToggleDarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDarkModeCommand.Execute(false);
            }
        }

        private void ToggleDevMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDevModeCommand.Execute(true);
            }
        }

        private void ToggleDevMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDevModeCommand.Execute(false);
            }
        }

        private void ToggleDeleteConfirmation_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDeleteConfirmationCommand.Execute(true);
            }
        }

        private void ToggleDeleteConfirmation_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleDeleteConfirmationCommand.Execute(false);
            }
        }

        private void ToggleUpdateNotifications_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleUpdateNotificationsCommand.Execute(true);
            }
        }

        private void ToggleUpdateNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleUpdateNotificationsCommand.Execute(false);
            }
        }

        private void ToggleModOrderCreation_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleModOrderCreationCommand.Execute(true);
            }
        }

        private void ToggleModOrderCreation_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleModOrderCreationCommand.Execute(false);
            }
        }

        private void ToggleFileRenaming_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleFileRenamingCommand.Execute(true);
            }
        }

        private void ToggleFileRenaming_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleFileRenamingCommand.Execute(false);
            }
        }

        private void ToggleWorkshopActions_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleWorkshopActionsCommand.Execute(true);
            }
        }

        private void ToggleWorkshopActions_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleWorkshopActionsCommand.Execute(false);
            }
        }

        private void ToggleBackupCreation_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleBackupCreationCommand.Execute(true);
            }
        }

        private void ToggleBackupCreation_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleBackupCreationCommand.Execute(false);
            }
        }

        private void ToggleBackupOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleBackupOnStartupCommand.Execute(true);
            }
        }

        private void ToggleBackupOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && _viewModel != null)
            {
                _viewModel.ToggleBackupOnStartupCommand.Execute(false);
            }
        }
    }
}
