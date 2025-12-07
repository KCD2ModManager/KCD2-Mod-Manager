using System.Windows;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Custom MessageBox-Dialog mit Dark Mode Support und Lokalisierung
    /// WICHTIG: Vollständig lokalisiert, reagiert auf Sprachänderungen, unterstützt Dark/Light Mode
    /// </summary>
    public partial class CustomMessageBoxWindow : Window
    {
        private readonly CustomMessageBoxViewModel _viewModel;
        private readonly IThemeService? _themeService;
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        /// <summary>
        /// DI-kompatibler Constructor
        /// </summary>
        public CustomMessageBoxWindow(CustomMessageBoxViewModel viewModel, IThemeService? themeService = null)
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
        public CustomMessageBoxWindow()
        {
            InitializeComponent();
            
            // Fallback: ViewModel ohne DI erstellen
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            if (serviceProvider != null)
            {
                _viewModel = serviceProvider.GetRequiredService<CustomMessageBoxViewModel>();
                _themeService = serviceProvider.GetService<IThemeService>();
                DataContext = _viewModel;
            }
            else
            {
                // Fallback: Direkt erstellen (sollte nicht passieren)
                var settings = new Services.AppSettings();
                var localizationService = new Services.LocalizationService(settings);
                _viewModel = new CustomMessageBoxViewModel(localizationService);
                DataContext = _viewModel;
            }
            
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}

