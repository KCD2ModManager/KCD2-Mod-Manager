using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Resources;
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
        private readonly IDialogService? _dialogService;

        /// <summary>
        /// DI-kompatibler Constructor - ViewModel wird über Dependency Injection injiziert
        /// </summary>
        public GameSelectionDialog(GameSelectionDialogViewModel viewModel, IThemeService? themeService = null, IDialogService? dialogService = null)
        {
            // WICHTIG: InitializeComponent ZUERST, damit XAML-Ressourcen geladen sind
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            _themeService = themeService;
            _dialogService = dialogService;
            
            // Fallback: DialogService über ServiceProvider holen, falls nicht injiziert
            if (_dialogService == null)
            {
                var app = Application.Current as App;
                var serviceProvider = app?.GetServiceProvider();
                _dialogService = serviceProvider?.GetService<IDialogService>();
            }
            
            DataContext = _viewModel;
            
            // Fenster-Eigenschaften setzen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = true;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
            
            // WICHTIG: Closing-Event-Handler für Beendigungs-Dialog
            this.Closing += GameSelectionDialog_Closing;
            
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
            
            // Event-Handler für Spiel-Auswahl
            _viewModel.GameSelected += (s, gameType) =>
            {
                SelectedGame = gameType;
                DialogResult = true;
                Close();
            };
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

        // OK-Button wurde entfernt - Karten sind jetzt klickbar
        // Beim Klick auf eine Karte wird SelectGameCommand aufgerufen
        // Der Command setzt SelectedGame und schließt den Dialog automatisch

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // WICHTIG: Beim App-Start (wenn MainWindow noch nicht existiert) soll der Beendigungs-Dialog kommen
            // Beim Wechseln (wenn MainWindow existiert) soll einfach geschlossen werden
            if (Application.Current?.MainWindow == null)
            {
                // App-Start: Zeige Beendigungs-Dialog
                // WICHTIG: Entferne Closing-Handler temporär, um mehrfache Dialoge zu vermeiden
                this.Closing -= GameSelectionDialog_Closing;
                
                // Zeige DialogService-Dialog (Dark/Light Mode kompatibel)
                if (_dialogService != null)
                {
                    var message = Messages.ExitApplicationMessage ?? "Closing this window will exit the application. Do you want to continue?";
                    var title = Messages.DialogTitleWarning ?? "Exit Application";
                    
                    var result = _dialogService.ShowMessageBox(
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == true)
                    {
                        // Benutzer hat "Yes" gewählt - Anwendung beenden
                        DialogResult = false;
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        // Benutzer hat "No" gewählt - Fenster bleibt offen
                        // WICHTIG: Füge Closing-Handler wieder hinzu
                        this.Closing += GameSelectionDialog_Closing;
                    }
                }
                else
                {
                    // Fallback: Standard MessageBox (nicht Dark Mode kompatibel)
                    var message = Messages.ExitApplicationMessage ?? "Closing this window will exit the application. Do you want to continue?";
                    var title = Messages.DialogTitleWarning ?? "Exit Application";
                    
                    var result = MessageBox.Show(
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        DialogResult = false;
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        // Fenster bleibt offen
                        this.Closing += GameSelectionDialog_Closing;
                    }
                }
            }
            else
            {
                // Wechseln: Einfach schließen ohne Dialog
                DialogResult = false;
                // WICHTIG: Entferne Closing-Handler temporär, um mehrfache Dialoge zu vermeiden
                this.Closing -= GameSelectionDialog_Closing;
                Close();
            }
        }

        private void SelectFolderButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Command != null)
            {
                var parameter = button.CommandParameter;
                if (button.Command.CanExecute(parameter))
                {
                    button.Command.Execute(parameter);
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event-Handler für das Schließen des Fensters
        /// Zeigt einen DialogService-Dialog, der bestätigt, dass die Anwendung beendet wird
        /// WICHTIG: Verwendet DialogService für Dark/Light Mode Kompatibilität
        /// </summary>
        private void GameSelectionDialog_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // WICHTIG: Wenn DialogResult bereits gesetzt wurde (OK oder Cancel), kein Dialog nötig
            // Dies verhindert mehrfache Dialoge, wenn Cancel-Button geklickt wurde
            if (DialogResult.HasValue || SelectedGame != null)
            {
                return;
            }
            
            // Verhindere das Schließen, bis der Dialog beantwortet wurde
            e.Cancel = true;
            
            // Zeige DialogService-Dialog (Dark/Light Mode kompatibel)
            if (_dialogService != null)
            {
                var message = Messages.ExitApplicationMessage ?? "Closing this window will exit the application. Do you want to continue?";
                var title = Messages.DialogTitleWarning ?? "Exit Application";
                
                var result = _dialogService.ShowMessageBox(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == true)
                {
                    // Benutzer hat "Yes" gewählt - Anwendung beenden
                    e.Cancel = false;
                    Application.Current.Shutdown();
                }
                // Wenn "No", bleibt e.Cancel = true, Fenster bleibt offen
            }
            else
            {
                // Fallback: Standard MessageBox (nicht Dark Mode kompatibel)
                var message = Messages.ExitApplicationMessage ?? "Closing this window will exit the application. Do you want to continue?";
                var title = Messages.DialogTitleWarning ?? "Exit Application";
                
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = false;
                    Application.Current.Shutdown();
                }
            }
        }

    }
}
