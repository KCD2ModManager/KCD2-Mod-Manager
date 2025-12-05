using System.Windows;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KCD2_mod_manager.Views.Dialogs
{
    /// <summary>
    /// Dialog für die Auswahl zwischen KCD1 und KCD2 (Feature 11)
    /// WICHTIG: Vollständig DI-kompatibel - ViewModel wird über Dependency Injection injiziert
    /// Unterstützt Dark/Light Mode über zentralen ThemeService
    /// </summary>
    public partial class GameSelectionDialog : Window
    {
        public GameType? SelectedGame { get; private set; }
        private readonly GameSelectionDialogViewModel _viewModel;
        private readonly IThemeService? _themeService;

        /// <summary>
        /// DI-kompatibler Constructor - ViewModel wird über Dependency Injection injiziert
        /// </summary>
        public GameSelectionDialog(GameSelectionDialogViewModel viewModel, IThemeService? themeService = null)
        {
            // WICHTIG: InitializeComponent ZUERST, damit XAML-Ressourcen geladen sind
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            _themeService = themeService;
            DataContext = _viewModel;
            
            // Fenster-Eigenschaften setzen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = true;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
            
            // Setze Standard-Auswahl, falls noch keine gesetzt ist
            // WICHTIG: Prüfe auf default(GameType), nicht auf null, da es ein Enum ist
            if (_viewModel.SelectedGame == default(GameType))
            {
                // Wähle automatisch das erste verfügbare Spiel
                if (_viewModel.HasKCD1)
                {
                    _viewModel.SelectedGame = GameType.KCD1;
                }
                else if (_viewModel.HasKCD2)
                {
                    _viewModel.SelectedGame = GameType.KCD2;
                }
            }
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedGame = _viewModel.SelectedGame;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
