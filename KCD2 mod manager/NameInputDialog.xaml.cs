using System.Windows;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Dialog für Texteingabe (z.B. Profilname, Mod-Name, Notizen)
    /// WICHTIG: Vollständig lokalisiert, reagiert auf Sprachänderungen, unterstützt Dark/Light Mode
    /// </summary>
    public partial class NameInputDialog : Window
    {
        private readonly NameInputDialogViewModel _viewModel;
        private readonly IThemeService? _themeService;
        public string EnteredText { get; private set; } = string.Empty;

        /// <summary>
        /// DI-kompatibler Constructor
        /// </summary>
        public NameInputDialog(NameInputDialogViewModel viewModel, IThemeService? themeService = null)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _themeService = themeService;
            DataContext = _viewModel;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
            
            // WICHTIG: Höhe basierend auf IsMultiline setzen
            UpdateWindowHeight();
            
            // WICHTIG: TextBox-Text explizit setzen, um sicherzustellen, dass DefaultValue angezeigt wird
            ModNameTextBox.Text = _viewModel.DefaultValue;
            
            // WICHTIG: Auf Änderungen von IsMultiline reagieren
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NameInputDialogViewModel.IsMultiline))
                {
                    UpdateWindowHeight();
                }
                else if (e.PropertyName == nameof(NameInputDialogViewModel.DefaultValue))
                {
                    // WICHTIG: TextBox aktualisieren, wenn DefaultValue geändert wird
                    ModNameTextBox.Text = _viewModel.DefaultValue;
                }
            };
        }

        /// <summary>
        /// Legacy Constructor für Kompatibilität
        /// </summary>
        public NameInputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            
            // ViewModel über DI erstellen
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            if (serviceProvider != null)
            {
                _viewModel = serviceProvider.GetRequiredService<NameInputDialogViewModel>();
                _themeService = serviceProvider.GetService<IThemeService>();
            }
            else
            {
                // Fallback: Direkt erstellen (nicht ideal, aber für Kompatibilität)
                var localizationService = new Services.LocalizationService(new Services.AppSettings());
                _viewModel = new NameInputDialogViewModel(localizationService);
            }
            
            _viewModel.Prompt = prompt;
            _viewModel.Title = title;
            _viewModel.DefaultValue = defaultValue;
            DataContext = _viewModel;
            
            // WICHTIG: Theme anwenden
            ApplyTheme();
            
            // WICHTIG: Höhe basierend auf IsMultiline setzen
            UpdateWindowHeight();
            
            // WICHTIG: TextBox-Text explizit setzen, um sicherzustellen, dass DefaultValue angezeigt wird
            ModNameTextBox.Text = _viewModel.DefaultValue;
            
            // WICHTIG: Auf Änderungen von IsMultiline reagieren
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NameInputDialogViewModel.IsMultiline))
                {
                    UpdateWindowHeight();
                }
                else if (e.PropertyName == nameof(NameInputDialogViewModel.DefaultValue))
                {
                    // WICHTIG: TextBox aktualisieren, wenn DefaultValue geändert wird
                    ModNameTextBox.Text = _viewModel.DefaultValue;
                }
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

        /// <summary>
        /// Aktualisiert die Fensterhöhe basierend auf IsMultiline
        /// </summary>
        private void UpdateWindowHeight()
        {
            if (_viewModel != null)
            {
                this.Height = _viewModel.IsMultiline ? 320.0 : 220.0;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // WICHTIG: Bei multiline Text nicht trimmen (Zeilenumbrüche behalten)
            // Nur führende/abschließende Leerzeilen entfernen
            if (_viewModel.IsMultiline)
            {
                EnteredText = ModNameTextBox.Text.TrimEnd('\r', '\n').Trim();
            }
            else
            {
                EnteredText = ModNameTextBox.Text.Trim();
            }
            DialogResult = true;
            Close();
        }
    }
}
