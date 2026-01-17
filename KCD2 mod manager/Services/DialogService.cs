using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using KCD2_mod_manager.ViewModels;
using System.Threading;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// Zeigt einen lokalisierten Custom MessageBox-Dialog
        /// WICHTIG: Verwendet CustomMessageBoxWindow mit ViewModel für Lokalisierung und ThemeService
        /// </summary>
        public bool? ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            if (_serviceProvider != null)
            {
                // WICHTIG: Verwende lokalisierten Dialog über DI
                var viewModel = _serviceProvider.GetRequiredService<ViewModels.CustomMessageBoxViewModel>();
                
                // Setze Inhalt BEVOR das Window erstellt wird
                viewModel.SetContent(message, title, buttons, icon);
                
                // WICHTIG: Erstelle Window mit dem bereits konfigurierten ViewModel
                var themeService = _serviceProvider.GetService<IThemeService>();
                var window = new CustomMessageBoxWindow(viewModel, themeService);
                
                // WICHTIG: Setze Owner für korrektes Zentrieren auf der MainWindow
                var owner = GetDialogOwner(window);
                if (owner != null)
                {
                    window.Owner = owner;
                }
                
                // WICHTIG: ThemeService wird über DI injiziert
                var result = window.ShowDialog();
                if (result == true)
                {
                    return window.Result switch
                    {
                        MessageBoxResult.Yes => true,
                        MessageBoxResult.No => false,
                        MessageBoxResult.OK => true,
                        MessageBoxResult.Cancel => null,
                        _ => null
                    };
                }
                return null;
            }
            else
            {
                // Fallback: Legacy MessageBox (nicht lokalisiert, kein Dark Mode)
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
        public string? ShowInputDialog(string prompt, string title, string defaultValue = "", bool isMultiline = false)
        {
            if (_serviceProvider != null)
            {
                // WICHTIG: Verwende lokalisierten Dialog über DI
                var viewModel = _serviceProvider.GetRequiredService<NameInputDialogViewModel>();
                var themeService = _serviceProvider.GetService<IThemeService>();
                
                // WICHTIG: Konfiguriere ViewModel BEVOR der Dialog erstellt wird
                viewModel.Prompt = prompt;
                // Title wird vom ViewModel aus Resources geladen, aber wir können es überschreiben wenn nötig
                if (!string.IsNullOrEmpty(title))
                {
                    viewModel.Title = title;
                }
                viewModel.DefaultValue = defaultValue;
                viewModel.IsMultiline = isMultiline;
                
                // WICHTIG: Erstelle Dialog mit dem konfigurierten ViewModel (nicht über DI, da DI ein neues ViewModel erstellen würde)
                var dialog = new NameInputDialog(viewModel, themeService);
                // WICHTIG: Owner setzen für korrektes Zentrieren
                var owner = GetDialogOwner(dialog);
                if (owner != null)
                {
                    dialog.Owner = owner;
                }
                
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
                var owner = GetDialogOwner(window);
                if (owner != null)
                {
                    window.Owner = owner;
                }
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

        /// <summary>
        /// Zeigt einen Folder-Picker-Dialog
        /// WICHTIG: Verwendet OpenFileDialog mit FolderSelection-Option (WPF-kompatibel)
        /// </summary>
        public string? ShowFolderPicker(string description, string? initialPath = null)
        {
            // WICHTIG: OpenFileDialog mit ValidateNames=false und CheckFileExists=false simuliert Folder-Picker
            var dialog = new OpenFileDialog
            {
                Title = description,
                ValidateNames = false,
                CheckFileExists = false,
                FileName = "Folder Selection."
            };
            
            if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            if (dialog.ShowDialog() == true)
            {
                string? selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                {
                    return selectedPath;
                }
            }
            return null;
        }

        private Window? GetDialogOwner(Window dialog)
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null && mainWindow != dialog)
            {
                return mainWindow;
            }

            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w != dialog);
        }

        /// <summary>
        /// Zeigt einen Progress-Dialog (vereinfacht: nur Progress-Reporting)
        /// WICHTIG: Für längere Operationen, die cancellable sein sollen
        /// Die eigentliche UI wird vom Aufrufer verwaltet (z.B. via MessageBox oder Custom Window)
        /// </summary>
        public void ShowProgressDialog(string title, string message, CancellationToken cancellationToken, IProgress<string>? progress = null)
        {
            // WICHTIG: Vereinfachte Implementierung - nur Progress-Reporting
            // Der Aufrufer kann einen eigenen Progress-Dialog zeigen
            if (progress != null)
            {
                progress.Report(message);
            }
        }
    }
}

