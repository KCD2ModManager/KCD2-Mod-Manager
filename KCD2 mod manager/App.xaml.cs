using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public ServiceProvider? GetServiceProvider() => _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // WICHTIG: ShutdownMode auf OnExplicitShutdown setzen, damit die Anwendung nicht automatisch beendet wird
            // wenn alle Fenster geschlossen werden, bevor das MainWindow angezeigt wird
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // WICHTIG: Sprache ZUERST setzen, BEVOR Services erstellt werden
                // Dies stellt sicher, dass alle ViewModels und Dialoge die korrekte Sprache verwenden
                InitializeLanguageEarly();

                // Dependency Injection Container einrichten
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                _serviceProvider = serviceCollection.BuildServiceProvider();

                // WICHTIG: Game Selection Dialog ZUERST anzeigen, BEVOR MainWindow erstellt wird
                // Verwende Dispatcher.InvokeAsync, um async Operationen im UI-Thread auszuführen
                Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await StartApplicationAsync();
                    }
                    catch (Exception ex)
                    {
                        var logger = _serviceProvider?.GetService<ILog>();
                        logger?.Error("Fehler beim Starten der Anwendung", ex);
                        MessageBox.Show($"Fehler beim Starten der Anwendung:\n{ex.Message}", 
                            "Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritischer Fehler beim Starten:\n{ex.Message}\n\n{ex.StackTrace}", 
                    "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Asynchroner Start der Anwendung
        /// WICHTIG: Verhindert Deadlocks durch korrektes awaiten von async Methoden
        /// </summary>
        private async System.Threading.Tasks.Task StartApplicationAsync()
        {
            var gameInstallService = _serviceProvider!.GetRequiredService<IGameInstallService>();
            var logger = _serviceProvider.GetRequiredService<ILog>();
            
            // Spiele erkennen - WICHTIG: Korrekt awaiten, nicht .GetAwaiter().GetResult()
            logger.Info("Erkenne installierte Spiele...");
            await gameInstallService.DetectInstalledGamesAsync();
            logger.Info($"Spiele erkannt: {gameInstallService.InstallType}");

            GameType? selectedGame = null;

            // Prüfe gespeicherte Auswahl
            var settings = _serviceProvider.GetRequiredService<IAppSettings>();
            string? lastSelected = settings.LastSelectedGame;
            
            if (!string.IsNullOrEmpty(lastSelected) && Enum.TryParse<GameType>(lastSelected, out var savedGame))
            {
                // Prüfe ob gespeichertes Spiel noch installiert ist
                var savedInstall = savedGame == GameType.KCD1 ? gameInstallService.KCD1Install : gameInstallService.KCD2Install;
                if (savedInstall != null)
                {
                    selectedGame = savedGame;
                    gameInstallService.SelectedGame = savedGame;
                    logger.Info($"Gespeicherte Auswahl gefunden: {savedGame}");
                }
                else
                {
                    logger.Warning($"Gespeichertes Spiel {savedGame} nicht mehr installiert");
                }
            }

            // Wenn keine gespeicherte Auswahl oder beide Spiele installiert, Dialog zeigen
            if (!selectedGame.HasValue)
            {
                if (gameInstallService.InstallType == GameInstallType.Both)
                {
                    // Beide Spiele installiert - Dialog zeigen
                    logger.Info("Beide Spiele installiert - zeige Auswahl-Dialog");
                    selectedGame = ShowGameSelectionDialog(gameInstallService, logger);
                    
                    if (!selectedGame.HasValue)
                    {
                        // Dialog abgebrochen oder Fehler - Anwendung beenden
                        logger.Warning("Spiel-Auswahl abgebrochen - Anwendung wird beendet");
                        
                        // WICHTIG: Verwende DialogService für Dark/Light Mode Kompatibilität
                        var dialogService = _serviceProvider?.GetService<IDialogService>();
                        if (dialogService != null)
                        {
                            var message = Messages.NoGameSelectedMessage ?? "No game was selected. The application will be closed.";
                            var title = Messages.DialogTitleInformation ?? "No Selection";
                            dialogService.ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // Fallback: Standard MessageBox
                            MessageBox.Show(Messages.NoGameSelectedMessage ?? "No game was selected. The application will be closed.", 
                                Messages.DialogTitleInformation ?? "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        
                        Shutdown();
                        return;
                    }
                }
                else if (gameInstallService.InstallType == GameInstallType.KCD1 && gameInstallService.KCD1Install != null)
                {
                    selectedGame = GameType.KCD1;
                    gameInstallService.SelectedGame = GameType.KCD1;
                    logger.Info("Nur KCD1 installiert - automatisch ausgewählt");
                }
                else if (gameInstallService.InstallType == GameInstallType.KCD2 && gameInstallService.KCD2Install != null)
                {
                    selectedGame = GameType.KCD2;
                    gameInstallService.SelectedGame = GameType.KCD2;
                    logger.Info("Nur KCD2 installiert - automatisch ausgewählt");
                }
                else
                {
                    // Kein Spiel installiert - Dialog zeigen
                    logger.Warning("Kein Spiel installiert - zeige Auswahl-Dialog");
                    selectedGame = ShowGameSelectionDialog(gameInstallService, logger);
                    
                    if (!selectedGame.HasValue)
                    {
                        logger.Warning("Spiel-Auswahl abgebrochen - Anwendung wird beendet");
                        
                        // WICHTIG: Verwende DialogService für Dark/Light Mode Kompatibilität
                        var dialogService = _serviceProvider?.GetService<IDialogService>();
                        if (dialogService != null)
                        {
                            var message = Messages.NoGameSelectedMessage ?? "No game was selected. The application will be closed.";
                            var title = Messages.DialogTitleInformation ?? "No Selection";
                            dialogService.ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // Fallback: Standard MessageBox
                            MessageBox.Show(Messages.NoGameSelectedMessage ?? "No game was selected. The application will be closed.", 
                                Messages.DialogTitleInformation ?? "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        
                        Shutdown();
                        return;
                    }
                }
            }

            // WICHTIG: SelectedGame MUSS gesetzt sein, bevor MainWindow erstellt wird
            if (!selectedGame.HasValue)
            {
                logger.Error("Kritischer Fehler: SelectedGame ist null nach Auswahl-Logik");
                
                // WICHTIG: Verwende DialogService für Dark/Light Mode Kompatibilität
                var dialogService = _serviceProvider?.GetService<IDialogService>();
                if (dialogService != null)
                {
                    var message = Messages.ErrorNoGameSelected ?? "Critical error: No game could be selected.";
                    var title = Messages.DialogTitleError ?? "Critical Error";
                    dialogService.ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Fallback: Standard MessageBox
                    MessageBox.Show(Messages.ErrorNoGameSelected ?? "Critical error: No game could be selected.", 
                        Messages.DialogTitleError ?? "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                Shutdown();
                return;
            }

            // Ausgewähltes Spiel speichern
            settings.LastSelectedGame = selectedGame.Value.ToString();
            settings.Save();
            logger.Info($"Spiel ausgewählt und gespeichert: {selectedGame.Value}");

            // MainWindow erstellen und anzeigen (nach Spiel-Auswahl)
            // WICHTIG: MainWindow wird erst NACH erfolgreicher Spiel-Auswahl erstellt
            // WICHTIG: Da StartApplicationAsync bereits im UI-Thread läuft (via Dispatcher.InvokeAsync),
            // kann MainWindow direkt erstellt und angezeigt werden
            try
            {
                logger.Info("Erstelle MainWindow...");
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                logger.Info("MainWindow erstellt, zeige an...");
                
                // WICHTIG: MainWindow als Hauptfenster setzen, damit die Anwendung nicht beendet wird
                MainWindow = mainWindow;
                
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Focus();
                logger.Info("MainWindow erfolgreich angezeigt");
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erstellen/Anzeigen des MainWindow", ex);
                
                // WICHTIG: Verwende DialogService für Dark/Light Mode Kompatibilität
                var dialogService = _serviceProvider?.GetService<IDialogService>();
                var errorMessage = string.Format(Messages.ErrorMainWindowCreation ?? "Error creating main window:\n{0}\n\n{1}", ex.Message, ex.StackTrace);
                var title = Messages.DialogTitleError ?? "Critical Error";
                
                if (dialogService != null)
                {
                    dialogService.ShowMessageBox(errorMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Fallback: Standard MessageBox
                    MessageBox.Show(errorMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                Shutdown();
            }
        }

        /// <summary>
        /// Zeigt den Game Selection Dialog und gibt das ausgewählte Spiel zurück
        /// WICHTIG: Wird bereits im UI-Thread aufgerufen (via Dispatcher.InvokeAsync in StartApplicationAsync)
        /// Daher kann ShowDialog() direkt aufgerufen werden
        /// </summary>
        private GameType? ShowGameSelectionDialog(IGameInstallService gameInstallService, ILog logger)
        {
            GameType? result = null;
            
            try
            {
                // Dialog über DI erstellen (ViewModel wird automatisch injiziert)
                var dialog = _serviceProvider!.GetRequiredService<Views.Dialogs.GameSelectionDialog>();
                
                // Stelle sicher, dass Dialog-Eigenschaften korrekt gesetzt sind
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.Topmost = true;
                dialog.ShowInTaskbar = true;
                
                logger.Info("GameSelectionDialog wird angezeigt");
                bool? dialogResult = dialog.ShowDialog();
                logger.Info($"GameSelectionDialog Ergebnis: {dialogResult}");

                if (dialogResult == true && dialog.SelectedGame.HasValue)
                {
                    result = dialog.SelectedGame.Value;
                    gameInstallService.SelectedGame = result.Value;
                    logger.Info($"Spiel ausgewählt: {result.Value}");
                }
                else
                {
                    logger.Info("GameSelectionDialog wurde abgebrochen");
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Anzeigen des GameSelectionDialog", ex);
                
                // WICHTIG: Verwende DialogService für Dark/Light Mode Kompatibilität
                var dialogService = _serviceProvider?.GetService<IDialogService>();
                var errorMessage = string.Format(Messages.ErrorGameSelectionDialog ?? "Error displaying game selection dialog:\n{0}", ex.Message);
                var title = Messages.DialogTitleError ?? "Error";
                
                if (dialogService != null)
                {
                    dialogService.ShowMessageBox(errorMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Fallback: Standard MessageBox
                    MessageBox.Show(errorMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Initialisiert die Sprache sehr früh, BEVOR Services erstellt werden
        /// WICHTIG: Dies stellt sicher, dass alle ViewModels und Dialoge die korrekte Sprache beim Start verwenden
        /// </summary>
        private void InitializeLanguageEarly()
        {
            try
            {
                // Lade gespeicherte Sprache aus Settings
                var settings = new AppSettings();
                string languageCode = settings.Language ?? "en";
                
                // Setze Culture auf UI-Thread
                var culture = new System.Globalization.CultureInfo(languageCode);
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch
            {
                // Fallback auf Englisch bei Fehler
                var culture = new System.Globalization.CultureInfo("en");
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services registrieren
            services.AddSingleton<ILog, Logger>();
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IModManifestService, ModManifestService>();
            services.AddHttpClient(); // Für HttpClientFactory
            services.AddSingleton<INexusService, NexusService>();
            services.AddSingleton<IModInstallerService, ModInstallerService>();
            services.AddSingleton<IUserModDataService, UserModDataService>();
            services.AddSingleton<IProfilesService, ProfilesService>();
            services.AddSingleton<IModOrderFileManager, ModOrderFileManager>();
            services.AddSingleton<IGameInstallService, GameInstallService>();
            services.AddSingleton<IManifestUpdateService, ManifestUpdateService>();

            // ViewModels registrieren (MUSS vor Views registriert werden)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SettingsWindowViewModel>();
            services.AddTransient<GameSelectionDialogViewModel>();
            services.AddTransient<ViewModels.NameInputDialogViewModel>();
            services.AddTransient<ViewModels.DeleteConfirmationDialogViewModel>();
            services.AddTransient<ViewModels.CustomMessageBoxViewModel>();

            // Views registrieren (NACH ViewModels)
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<Views.Dialogs.GameSelectionDialog>();
            services.AddTransient<NameInputDialog>();
            services.AddTransient<DeleteConfirmationWindow>();
            services.AddTransient<CustomMessageBoxWindow>();

            // Logs-Verzeichnis erstellen
            Directory.CreateDirectory("logs");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
