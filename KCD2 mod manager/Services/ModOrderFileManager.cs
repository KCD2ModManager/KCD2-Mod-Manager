using System.IO;
using System.Windows;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für die atomare Verwaltung von mod_order.txt und mod_order_backup.txt
    /// WICHTIG: Verwaltet nur die MAIN/Global-Mod-Order-Dateien, nicht Profile-spezifische
    /// </summary>
    public class ModOrderFileManager : IModOrderFileManager
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly IDialogService? _dialogService;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        private const string ModOrderFileName = "mod_order.txt";
        private const string ModOrderBackupFileName = "mod_order_backup.txt";

        public ModOrderFileManager(IFileService fileService, ILog logger, IDialogService? dialogService = null)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService;
        }

        /// <summary>
        /// Wendet das ModOrderEnabled-Setting an:
        /// - Wenn enabled=true: Verschiebt mod_order_backup.txt -> mod_order.txt (falls Backup existiert)
        /// - Wenn enabled=false: Verschiebt mod_order.txt -> mod_order_backup.txt und löscht mod_order.txt
        /// </summary>
        public async Task ApplyModOrderSettingAsync(bool enabled, string modFolder, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                string modOrderPath = _fileService.Combine(modFolder, ModOrderFileName);
                string modOrderBackupPath = _fileService.Combine(modFolder, ModOrderBackupFileName);

                if (enabled)
                {
                    // ModOrderEnabled = true: Stelle mod_order.txt wieder her
                    if (_fileService.FileExists(modOrderBackupPath))
                    {
                        _logger.Info($"ModOrderEnabled=true: Stelle mod_order.txt aus Backup wieder her");
                        
                        // Atomisches Verschieben: Backup -> mod_order.txt
                        await AtomicMoveFileAsync(modOrderBackupPath, modOrderPath, cancellationToken);
                        
                        _logger.Info($"mod_order.txt aus Backup wiederhergestellt: {modOrderPath}");
                    }
                    else if (!_fileService.FileExists(modOrderPath))
                    {
                        // Kein Backup vorhanden, erstelle leere mod_order.txt
                        _logger.Info($"ModOrderEnabled=true: Erstelle neue mod_order.txt (kein Backup vorhanden)");
                        await _fileService.WriteAllLinesAsync(modOrderPath, Array.Empty<string>(), cancellationToken);
                    }
                    else
                    {
                        _logger.Info($"ModOrderEnabled=true: mod_order.txt existiert bereits, keine Aktion erforderlich");
                    }
                }
                else
                {
                    // ModOrderEnabled = false: Verschiebe mod_order.txt -> mod_order_backup.txt und lösche mod_order.txt
                    if (_fileService.FileExists(modOrderPath))
                    {
                        _logger.Info($"ModOrderEnabled=false: Verschiebe mod_order.txt -> mod_order_backup.txt");
                        
                        // Wenn Backup bereits existiert, überschreibe es (keine Daten verlieren)
                        if (_fileService.FileExists(modOrderBackupPath))
                        {
                            _logger.Info($"Backup existiert bereits, überschreibe mit aktuellem mod_order.txt");
                            _fileService.DeleteFile(modOrderBackupPath);
                        }
                        
                        // Atomisches Verschieben: mod_order.txt -> mod_order_backup.txt
                        await AtomicMoveFileAsync(modOrderPath, modOrderBackupPath, cancellationToken);
                        
                        // Stelle sicher, dass mod_order.txt nicht existiert
                        if (_fileService.FileExists(modOrderPath))
                        {
                            _fileService.DeleteFile(modOrderPath);
                        }
                        
                        _logger.Info($"mod_order.txt nach mod_order_backup.txt verschoben und mod_order.txt gelöscht");
                    }
                    else
                    {
                        _logger.Info($"ModOrderEnabled=false: mod_order.txt existiert nicht, keine Aktion erforderlich");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                string errorMsg = $"Keine Berechtigung zum Verschieben der Mod-Order-Dateien: {ex.Message}";
                _logger.Error(errorMsg, ex);
                if (_dialogService != null)
                {
                    _dialogService.ShowMessageBox(
                        errorMsg,
                        Resources.Messages.DialogTitleError,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                throw;
            }
            catch (IOException ex)
            {
                string errorMsg = $"IO-Fehler beim Verschieben der Mod-Order-Dateien: {ex.Message}";
                _logger.Error(errorMsg, ex);
                if (_dialogService != null)
                {
                    _dialogService.ShowMessageBox(
                        errorMsg,
                        Resources.Messages.DialogTitleError,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                throw;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Unerwarteter Fehler beim Anwenden des ModOrderEnabled-Settings: {ex.Message}";
                _logger.Error(errorMsg, ex);
                if (_dialogService != null)
                {
                    _dialogService.ShowMessageBox(
                        errorMsg,
                        Resources.Messages.DialogTitleError,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Konsolidiert die Mod-Order-Dateien beim App-Start basierend auf dem aktuellen Setting
        /// </summary>
        public async Task ConsolidateModOrderFilesAsync(bool modOrderEnabled, string modFolder, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                string modOrderPath = _fileService.Combine(modFolder, ModOrderFileName);
                string modOrderBackupPath = _fileService.Combine(modFolder, ModOrderBackupFileName);

                bool modOrderExists = _fileService.FileExists(modOrderPath);
                bool modOrderBackupExists = _fileService.FileExists(modOrderBackupPath);

                _logger.Info($"Konsolidierung Mod-Order-Dateien: ModOrderEnabled={modOrderEnabled}, mod_order.txt={modOrderExists}, mod_order_backup.txt={modOrderBackupExists}");

                if (modOrderEnabled)
                {
                    // Setting = true: mod_order.txt sollte existieren
                    if (!modOrderExists && modOrderBackupExists)
                    {
                        // mod_order.txt fehlt, aber Backup existiert -> wiederherstellen
                        _logger.Info($"Konsolidierung: Stelle mod_order.txt aus Backup wieder her");
                        await AtomicMoveFileAsync(modOrderBackupPath, modOrderPath, cancellationToken);
                    }
                    else if (modOrderExists && modOrderBackupExists)
                    {
                        // Beide existieren -> Backup ist veraltet, kann gelöscht werden (optional)
                        _logger.Info($"Konsolidierung: Beide Dateien existieren, Backup wird beibehalten");
                    }
                }
                else
                {
                    // Setting = false: mod_order.txt sollte NICHT existieren
                    if (modOrderExists && !modOrderBackupExists)
                    {
                        // mod_order.txt existiert, aber kein Backup -> verschiebe zu Backup
                        _logger.Info($"Konsolidierung: Verschiebe mod_order.txt -> mod_order_backup.txt");
                        await AtomicMoveFileAsync(modOrderPath, modOrderBackupPath, cancellationToken);
                        
                        // Stelle sicher, dass mod_order.txt gelöscht ist
                        if (_fileService.FileExists(modOrderPath))
                        {
                            _fileService.DeleteFile(modOrderPath);
                        }
                    }
                    else if (modOrderExists && modOrderBackupExists)
                    {
                        // Beide existieren -> mod_order.txt sollte gelöscht werden, Backup behalten
                        _logger.Info($"Konsolidierung: Lösche mod_order.txt (Backup bleibt erhalten)");
                        _fileService.DeleteFile(modOrderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei der Konsolidierung der Mod-Order-Dateien: {ex.Message}", ex);
                // Nicht weiterwerfen - Konsolidierung ist nicht kritisch
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Atomisches Verschieben einer Datei mit Retry-Mechanismus
        /// </summary>
        private async Task AtomicMoveFileAsync(string sourcePath, string destPath, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            const int delayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_fileService.FileExists(sourcePath))
                {
                    string missingMsg = string.Format(Resources.Messages.FileMoveSourceMissing, sourcePath);
                    _logger.Error(missingMsg);
                    _dialogService?.ShowMessageBox(
                        missingMsg,
                        Resources.Messages.DialogTitleError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new FileNotFoundException(missingMsg, sourcePath);
                }

                try
                {
                    // Verwende File.Move für atomare Operation
                    // File.Move ist atomar auf Windows (wenn auf derselben Partition)
                    if (_fileService.FileExists(destPath))
                    {
                        // Ziel existiert -> lösche zuerst, dann verschiebe
                        _fileService.DeleteFile(destPath);
                    }
                    
                    // Atomisches Verschieben
                    _fileService.MoveFile(sourcePath, destPath);
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    if (!_fileService.FileExists(sourcePath))
                    {
                        string missingMsg = string.Format(Resources.Messages.FileMoveSourceMissing, sourcePath);
                        _logger.Error(missingMsg, ex);
                        _dialogService?.ShowMessageBox(
                            missingMsg,
                            Resources.Messages.DialogTitleError,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        throw new FileNotFoundException(missingMsg, sourcePath, ex);
                    }

                    _logger.Warning($"Versuch {attempt}/{maxRetries} fehlgeschlagen beim Verschieben {sourcePath} -> {destPath}: {ex.Message}");
                    if (_dialogService != null)
                    {
                        bool? retry = _dialogService.ShowMessageBox(
                            string.Format(Resources.Messages.FileMoveFailedRetryPrompt, sourcePath, destPath, ex.Message),
                            Resources.Messages.DialogTitleError,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Error);
                        if (retry != true)
                        {
                            throw;
                        }
                    }
                    await Task.Delay(delayMs * attempt, cancellationToken);
                }
                catch (UnauthorizedAccessException ex) when (attempt < maxRetries)
                {
                    _logger.Warning($"Versuch {attempt}/{maxRetries} fehlgeschlagen (Berechtigung): {ex.Message}");
                    if (_dialogService != null)
                    {
                        bool? retry = _dialogService.ShowMessageBox(
                            string.Format(Resources.Messages.FileMoveFailedRetryPrompt, sourcePath, destPath, ex.Message),
                            Resources.Messages.DialogTitleError,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Error);
                        if (retry != true)
                        {
                            throw;
                        }
                    }
                    await Task.Delay(delayMs * attempt, cancellationToken);
                }
            }

            // Letzter Versuch ohne Retry
            if (!_fileService.FileExists(sourcePath))
            {
                string missingMsg = string.Format(Resources.Messages.FileMoveSourceMissing, sourcePath);
                _logger.Error(missingMsg);
                _dialogService?.ShowMessageBox(
                    missingMsg,
                    Resources.Messages.DialogTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw new FileNotFoundException(missingMsg, sourcePath);
            }
            if (_fileService.FileExists(destPath))
            {
                _fileService.DeleteFile(destPath);
            }
            _fileService.MoveFile(sourcePath, destPath);
        }
    }
}

