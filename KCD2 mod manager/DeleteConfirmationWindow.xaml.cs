using System.Windows;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Bestätigungs-Dialog für das Löschen von Mods
    /// WICHTIG: Vollständig lokalisiert, reagiert auf Sprachänderungen, unterstützt Dark/Light Mode
    /// </summary>
    public partial class DeleteConfirmationWindow : Window
    {
        private readonly DeleteConfirmationDialogViewModel _viewModel;
        private readonly IThemeService? _themeService;
        public bool DontAskAgain { get; private set; } = false;
        public bool UserConfirmed { get; private set; } = false;

        /// <summary>
        /// DI-kompatibler Constructor
        /// </summary>
        public DeleteConfirmationWindow(DeleteConfirmationDialogViewModel viewModel, IThemeService? themeService = null)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _themeService = themeService;
            DataContext = _viewModel;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
        }

        /// <summary>
        /// Legacy Constructor für Kompatibilität
        /// </summary>
        public DeleteConfirmationWindow()
        {
            InitializeComponent();
            
            // ViewModel über DI erstellen
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            if (serviceProvider != null)
            {
                _viewModel = serviceProvider.GetRequiredService<DeleteConfirmationDialogViewModel>();
                _themeService = serviceProvider.GetService<IThemeService>();
            }
            else
            {
                // Fallback: Direkt erstellen (nicht ideal, aber für Kompatibilität)
                var localizationService = new Services.LocalizationService(new Services.AppSettings());
                _viewModel = new DeleteConfirmationDialogViewModel(localizationService);
            }
            
            DataContext = _viewModel;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
        }

        /// <summary>
        /// Wendet das aktuelle Theme (Dark/Light Mode) auf den Dialog an
        /// WICHTIG: Verwendet zentralen ThemeService für Konsistenz
        /// </summary>
        private void ApplyTheme()
        {
            if (_themeService != null)
            {
                _themeService.ApplyTheme(this.Resources, _themeService.IsDarkMode);
                this.Background = (System.Windows.Media.Brush)this.Resources["WindowBackgroundBrush"];
            }
            else
            {
                // Fallback: Direkt aus Settings lesen
                var app = Application.Current as App;
                var serviceProvider = app?.GetServiceProvider();
                if (serviceProvider != null)
                {
                    var settings = serviceProvider.GetService<IAppSettings>();
                    if (settings != null)
                    {
                        var tempThemeService = new ThemeService(settings);
                        tempThemeService.ApplyTheme(this.Resources, settings.IsDarkMode);
                        this.Background = (System.Windows.Media.Brush)this.Resources["WindowBackgroundBrush"];
                    }
                }
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            DontAskAgain = DontAskAgainCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
