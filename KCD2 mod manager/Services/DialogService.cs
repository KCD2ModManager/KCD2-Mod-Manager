using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using KCD2_mod_manager.ViewModels;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von IDialogService für WPF-Dialoge
    /// WICHTIG: Alle Dialoge verwenden jetzt lokalisierte ViewModels und ThemeService
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly IThemeService? _themeService;

        public DialogService(IThemeService? themeService = null)
        {
            // Fallback: ServiceProvider über App bekommen
            var app = Application.Current as App;
            _serviceProvider = app?.GetServiceProvider();
            _themeService = themeService ?? _serviceProvider?.GetService<IThemeService>();
        }

        public bool? ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            var result = MessageBox.Show(message, title, buttons, icon);
            return result switch
            {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No => false,
                MessageBoxResult.OK => true,
                MessageBoxResult.Cancel => null,
                _ => null
            };
        }

        public string? ShowOpenFileDialog(string filter, string title, bool multiselect = false)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = multiselect
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string filter, string title)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                Title = title
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// Zeigt einen lokalisierten Eingabe-Dialog
        /// WICHTIG: Verwendet NameInputDialog mit ViewModel für Lokalisierung und ThemeService
        /// Der Prompt wird gesetzt, aber Title und Button-Text kommen aus den Resources
        /// </summary>
        public string? ShowInputDialog(string prompt, string title, string defaultValue = "")
        {
            if (_serviceProvider != null)
            {
                // WICHTIG: Verwende lokalisierten Dialog über DI
                var viewModel = _serviceProvider.GetRequiredService<NameInputDialogViewModel>();
                // WICHTIG: Nur Prompt setzen - Title und Button-Text kommen aus Resources (werden automatisch aktualisiert)
                viewModel.Prompt = prompt;
                // Title wird vom ViewModel aus Resources geladen, aber wir können es überschreiben wenn nötig
                if (!string.IsNullOrEmpty(title))
                {
                    viewModel.Title = title;
                }
                viewModel.DefaultValue = defaultValue;
                
                var dialog = _serviceProvider.GetRequiredService<NameInputDialog>();
                // WICHTIG: ThemeService wird über DI injiziert
                if (dialog.ShowDialog() == true)
                {
                    return dialog.EnteredText;
                }
                return null;
            }
            else
            {
                // Fallback: Legacy-Verhalten (nicht lokalisiert)
                string input = Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultValue);
                return string.IsNullOrWhiteSpace(input) ? null : input;
            }
        }

        /// <summary>
        /// Zeigt einen lokalisierten Lösch-Bestätigungs-Dialog
        /// WICHTIG: Verwendet DeleteConfirmationWindow mit ViewModel für Lokalisierung und ThemeService
        /// </summary>
        public bool? ShowDeleteConfirmation(out bool dontAskAgain)
        {
            dontAskAgain = false;
            
            if (_serviceProvider != null)
            {
                // WICHTIG: Verwende lokalisierten Dialog über DI
                var viewModel = _serviceProvider.GetRequiredService<DeleteConfirmationDialogViewModel>();
                var window = _serviceProvider.GetRequiredService<DeleteConfirmationWindow>();
                // WICHTIG: ThemeService wird über DI injiziert
                
                var result = window.ShowDialog();
                if (result == true)
                {
                    dontAskAgain = window.DontAskAgain;
                    return window.UserConfirmed;
                }
                return null;
            }
            else
            {
                // Fallback: Legacy-Verhalten
                var window = new DeleteConfirmationWindow();
                var result = window.ShowDialog();
                if (result == true)
                {
                    dontAskAgain = window.DontAskAgain;
                    return window.UserConfirmed;
                }
                return null;
            }
        }
    }
}

