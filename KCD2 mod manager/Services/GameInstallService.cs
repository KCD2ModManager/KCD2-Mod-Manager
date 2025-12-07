using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Resources;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für die Erkennung von KCD1 und KCD2 Installationen mit korrekten Pfadstrukturen
    /// 
    /// WICHTIG: Die Mods-Ordner befinden sich NICHT in Bin\Mods, sondern direkt im Root-Verzeichnis:
    /// - KCD1: ...\KingdomComeDeliverance\Mods
    /// - KCD2: ...\KingdomComeDeliverance2\Mods
    /// 
    /// ERWEITERT: Robuste Erkennung über Registry, Steam libraryfolders.vdf, alle Laufwerke, und manuelle Auswahl
    /// </summary>
    public class GameInstallService : IGameInstallService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly IAppSettings _settings;
        private readonly IDialogService _dialogService;

        public GameInstallType InstallType { get; private set; }
        public GameInstallDescriptor? KCD1Install { get; private set; }
        public GameInstallDescriptor? KCD2Install { get; private set; }
        public GameType SelectedGame { get; set; } = GameType.KCD2;

        public GameInstallDescriptor? SelectedInstall
        {
            get
            {
                return SelectedGame == GameType.KCD1 ? KCD1Install : KCD2Install;
            }
        }

        public GameInstallService(IFileService fileService, ILog logger, IAppSettings settings, IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        /// <summary>
        /// Erkennt installierte Spiele automatisch mit erweiterter Suche
        /// </summary>
        public async Task DetectInstalledGamesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(async () =>
            {
                // KCD1: Prüfe gespeicherten Pfad oder suche
                var kcd1Path = await ApplyCachedOrFindAsync(GameType.KCD1, cancellationToken);
                if (!string.IsNullOrEmpty(kcd1Path))
                {
                    var exePath = FindExecutableInPath(kcd1Path, GameType.KCD1);
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        KCD1Install = CreateInstallDescriptor(GameType.KCD1, kcd1Path, exePath);
                        _logger.Info($"KCD1 Installation gefunden: {kcd1Path} (Quelle: {GetPathSource(GameType.KCD1)})");
                    }
                }

                // KCD2: Prüfe gespeicherten Pfad oder suche
                var kcd2Path = await ApplyCachedOrFindAsync(GameType.KCD2, cancellationToken);
                if (!string.IsNullOrEmpty(kcd2Path))
                {
                    var exePath = FindExecutableInPath(kcd2Path, GameType.KCD2);
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        KCD2Install = CreateInstallDescriptor(GameType.KCD2, kcd2Path, exePath);
                        _logger.Info($"KCD2 Installation gefunden: {kcd2Path} (Quelle: {GetPathSource(GameType.KCD2)})");
                    }
                }

                // InstallType setzen
                if (KCD1Install != null && KCD2Install != null)
                {
                    InstallType = GameInstallType.Both;
                }
                else if (KCD1Install != null)
                {
                    InstallType = GameInstallType.KCD1;
                    SelectedGame = GameType.KCD1;
                }
                else if (KCD2Install != null)
                {
                    InstallType = GameInstallType.KCD2;
                    SelectedGame = GameType.KCD2;
                }
                else
                {
                    InstallType = GameInstallType.Unknown;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Prüft gespeicherten Pfad oder startet Suche
        /// </summary>
        private async Task<string?> ApplyCachedOrFindAsync(GameType gameType, CancellationToken cancellationToken)
        {
            // 1. Prüfe gespeicherten Pfad
            string? cachedPath = gameType == GameType.KCD1 ? _settings.GamePath_KCD1 : _settings.GamePath_KCD2;
            if (!string.IsNullOrEmpty(cachedPath))
            {
                if (await ValidateGameFolderAsync(gameType, cachedPath, cancellationToken))
                {
                    return cachedPath;
                }
                else
                {
                    _logger.Warning($"Gespeicherter Pfad für {gameType} ist ungültig: {cachedPath}");
                    // Lösche ungültigen Pfad
                    if (gameType == GameType.KCD1)
                        _settings.GamePath_KCD1 = string.Empty;
                    else
                        _settings.GamePath_KCD2 = string.Empty;
                    _settings.Save();
                }
            }

            // 2. Starte automatische Suche (ohne Folder-Picker am Ende)
            return await FindGameFolderAsync(gameType, cancellationToken);
        }

        /// <summary>
        /// Validiert einen Game-Folder-Pfad
        /// </summary>
        private async Task<bool> ValidateGameFolderAsync(GameType gameType, string rootPath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(rootPath) || !_fileService.DirectoryExists(rootPath))
                        return false;

                    // Prüfe ob Executable existiert
                    var exePath = FindExecutableInPath(rootPath, gameType);
                    if (string.IsNullOrEmpty(exePath) || !_fileService.FileExists(exePath))
                        return false;

                    // Optional: Prüfe ob Data-Ordner existiert (Marker für gültige Installation)
                    var dataPath = Path.Combine(rootPath, "Data");
                    if (!_fileService.DirectoryExists(dataPath))
                    {
                        _logger.Warning($"Data-Ordner nicht gefunden in {rootPath}, aber Executable vorhanden");
                        // Nicht kritisch, aber Warnung
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler beim Validieren von {rootPath}: {ex.Message}", ex);
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Findet Executable in einem Root-Pfad
        /// WICHTIG: Unterstützt alle möglichen Pfadstrukturen (Steam, GOG, etc.)
        /// </summary>
        private string? FindExecutableInPath(string rootPath, GameType gameType)
        {
            string exeName = "KingdomCome.exe";
            
            // Strategie 1: Standard-Pfade (Steam)
            if (gameType == GameType.KCD1)
            {
                string exePath = Path.Combine(rootPath, "Bin", "Win64", exeName);
                if (_fileService.FileExists(exePath))
                    return exePath;
            }
            else // KCD2
            {
                // KCD2 kann verschiedene Bin-Pfade haben
                string[] possibleBinPaths = new[]
                {
                    Path.Combine(rootPath, "Bin", "Win64MasterMasterSteamPGO", exeName),
                    Path.Combine(rootPath, "Bin", "Win64", exeName)
                };

                foreach (var exePath in possibleBinPaths)
                {
                    if (_fileService.FileExists(exePath))
                        return exePath;
                }
            }
            
            // Strategie 2: Rekursive Suche im Bin-Ordner (für GOG und andere Strukturen)
            string binPath = Path.Combine(rootPath, "Bin");
            if (_fileService.DirectoryExists(binPath))
            {
                var foundExe = FindExecutableRecursive(binPath, exeName);
                if (!string.IsNullOrEmpty(foundExe) && _fileService.FileExists(foundExe))
                    return foundExe;
            }
            
            // Strategie 3: Prüfe ob EXE direkt im Root liegt (selten, aber möglich)
            string rootExe = Path.Combine(rootPath, exeName);
            if (_fileService.FileExists(rootExe))
                return rootExe;
            
            return null;
        }
        
        /// <summary>
        /// Sucht rekursiv nach einer EXE-Datei in einem Verzeichnis
        /// </summary>
        private string? FindExecutableRecursive(string directory, string exeName)
        {
            try
            {
                if (!_fileService.DirectoryExists(directory))
                    return null;
                
                // Prüfe direkt in diesem Verzeichnis
                string directPath = Path.Combine(directory, exeName);
                if (_fileService.FileExists(directPath))
                    return directPath;
                
                // Suche rekursiv in Unterordnern (max. 3 Ebenen tief für Performance)
                return FindExecutableRecursive(directory, exeName, 0, 3);
            }
            catch
            {
                return null;
            }
        }
        
        private string? FindExecutableRecursive(string directory, string exeName, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return null;
            
            try
            {
                if (!_fileService.DirectoryExists(directory))
                    return null;
                
                // Prüfe direkt in diesem Verzeichnis
                string directPath = Path.Combine(directory, exeName);
                if (_fileService.FileExists(directPath))
                    return directPath;
                
                // Suche in Unterordnern
                var subdirs = Directory.GetDirectories(directory);
                foreach (var subdir in subdirs)
                {
                    var found = FindExecutableRecursive(subdir, exeName, currentDepth + 1, maxDepth);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }
            catch
            {
                // Ignoriere Fehler (z.B. Zugriffsrechte)
            }
            
            return null;
        }

        /// <summary>
        /// Erweiterte Suche nach Game-Folder
        /// </summary>
        private async Task<string?> FindGameFolderAsync(GameType gameType, CancellationToken cancellationToken)
        {
            IProgress<string>? progress = new Progress<string>(message => _logger.Info($"Game-Suche ({gameType}): {message}"));

            try
            {
                // 1. Steam Registry
                var steamInstallPath = await FindSteamInstallPathAsync(cancellationToken);
                if (!string.IsNullOrEmpty(steamInstallPath))
                {
                    progress.Report($"Prüfe Steam-Installation: {steamInstallPath}");
                    // Steam InstallPath enthält bereits "steamapps\common" nicht - füge es hinzu
                    string steamCommonPath = Path.Combine(steamInstallPath, "steamapps", "common");
                    var gamePath = await CheckSteamPathAsync(steamCommonPath, gameType, cancellationToken);
                    if (!string.IsNullOrEmpty(gamePath))
                    {
                        await SaveGamePathAsync(gameType, gamePath, "Steam Registry");
                        return gamePath;
                    }
                }

                // 2. Steam libraryfolders.vdf
                if (!string.IsNullOrEmpty(steamInstallPath))
                {
                    progress.Report("Prüfe Steam Library Folders...");
                    var libraryPaths = await ParseSteamLibraryFoldersAsync(steamInstallPath, cancellationToken);
                    foreach (var libraryPath in libraryPaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress.Report($"Prüfe Steam Library: {libraryPath}");
                        var gamePath = await CheckSteamPathAsync(libraryPath, gameType, cancellationToken);
                        if (!string.IsNullOrEmpty(gamePath))
                        {
                            await SaveGamePathAsync(gameType, gamePath, "Steam Library");
                            return gamePath;
                        }
                    }
                }

                // 3. GOG Registry
                progress.Report("Prüfe GOG-Installation...");
                var gogPath = await FindGogInstallPathAsync(cancellationToken);
                if (!string.IsNullOrEmpty(gogPath))
                {
                    var gamePath = await CheckGogPathAsync(gogPath, gameType, cancellationToken);
                    if (!string.IsNullOrEmpty(gamePath))
                    {
                        await SaveGamePathAsync(gameType, gamePath, "GOG Registry");
                        return gamePath;
                    }
                }

                // 4. Standard-Pfade auf allen Fixed Drives
                progress.Report("Prüfe Standard-Pfade auf allen Laufwerken...");
                var standardPaths = GetStandardSearchPaths(gameType);
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var standardPath in standardPaths)
                    {
                        var fullPath = Path.Combine(drive.RootDirectory.FullName, standardPath.TrimStart('\\'));
                        progress.Report($"Prüfe: {fullPath}");
                        if (await ValidateGameFolderAsync(gameType, fullPath, cancellationToken))
                        {
                            await SaveGamePathAsync(gameType, fullPath, $"Standard-Pfad ({drive.Name})");
                            return fullPath;
                        }
                    }
                }

                // 5. Keine automatische manuelle Auswahl - User kann später über Dialog auswählen
                progress.Report("Automatische Suche erfolglos - bitte wählen Sie den Ordner manuell im Dialog aus.");
                _logger.Info($"Automatische Suche für {gameType} erfolglos - User kann Ordner manuell im Dialog auswählen");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.Info($"Game-Suche für {gameType} wurde abgebrochen");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Game-Suche für {gameType}: {ex.Message}", ex);
                // Keine automatische manuelle Auswahl - User kann später über Dialog auswählen
                return null;
            }
        }

        /// <summary>
        /// Findet Steam InstallPath aus Registry
        /// </summary>
        private async Task<string?> FindSteamInstallPathAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // HKEY_LOCAL_MACHINE
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                    {
                        if (key != null)
                        {
                            var installPath = key.GetValue("InstallPath") as string;
                            if (!string.IsNullOrEmpty(installPath) && _fileService.DirectoryExists(installPath))
                                return installPath;
                        }
                    }

                    // HKEY_CURRENT_USER (Fallback)
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                    {
                        if (key != null)
                        {
                            var installPath = key.GetValue("SteamPath") as string;
                            if (!string.IsNullOrEmpty(installPath) && _fileService.DirectoryExists(installPath))
                                return installPath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Fehler beim Lesen der Steam-Registry: {ex.Message}");
                }
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Parst libraryfolders.vdf und gibt alle Library-Pfade zurück
        /// </summary>
        private async Task<List<string>> ParseSteamLibraryFoldersAsync(string steamPath, CancellationToken cancellationToken)
        {
            var libraries = new List<string>();
            
            return await Task.Run(() =>
            {
                try
                {
                    string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    if (!_fileService.FileExists(vdfPath))
                        return libraries;

                    string content = _fileService.ReadAllTextAsync(vdfPath, cancellationToken).Result;
                    if (string.IsNullOrEmpty(content))
                        return libraries;

                    // Parse VDF (tolerant für alte und neue Formate)
                    // Suche nach "path" Einträgen
                    var pathMatches = Regex.Matches(content, @"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    foreach (Match match in pathMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string libraryPath = match.Groups[1].Value;
                            // Ersetze \\ mit \
                            libraryPath = libraryPath.Replace(@"\\", @"\");
                            if (_fileService.DirectoryExists(libraryPath))
                            {
                                libraries.Add(libraryPath);
                            }
                        }
                    }

                    // Auch Standard-Pfad hinzufügen
                    string defaultLibrary = Path.Combine(steamPath, "steamapps", "common");
                    if (_fileService.DirectoryExists(defaultLibrary) && !libraries.Contains(defaultLibrary))
                    {
                        libraries.Insert(0, defaultLibrary);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Fehler beim Parsen von libraryfolders.vdf: {ex.Message}");
                }
                return libraries;
            }, cancellationToken);
        }

        /// <summary>
        /// Prüft Steam-Pfad auf Game-Installation
        /// </summary>
        private async Task<string?> CheckSteamPathAsync(string steamLibraryPath, GameType gameType, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                string gameFolderName = gameType == GameType.KCD1 ? "KingdomComeDeliverance" : "KingdomComeDeliverance2";
                string gamePath = Path.Combine(steamLibraryPath, gameFolderName);
                
                if (_fileService.DirectoryExists(gamePath))
                {
                    if (ValidateGameFolderAsync(gameType, gamePath, cancellationToken).Result)
                        return gamePath;
                }
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Findet GOG InstallPath aus Registry
        /// </summary>
        private async Task<string?> FindGogInstallPathAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // GOG Galaxy Games-Pfad
                    string gogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games");
                    if (_fileService.DirectoryExists(gogPath))
                        return gogPath;

                    // Alternative: Registry (falls vorhanden)
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com"))
                    {
                        if (key != null)
                        {
                            var installPath = key.GetValue("path") as string;
                            if (!string.IsNullOrEmpty(installPath))
                            {
                                string gamesPath = Path.Combine(installPath, "Games");
                                if (_fileService.DirectoryExists(gamesPath))
                                    return gamesPath;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Fehler beim Lesen der GOG-Registry: {ex.Message}");
                }
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Prüft GOG-Pfad auf Game-Installation
        /// </summary>
        private async Task<string?> CheckGogPathAsync(string gogPath, GameType gameType, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                string gameFolderName = gameType == GameType.KCD1 
                    ? "Kingdom Come Deliverance" 
                    : "Kingdom Come Deliverance II";
                string gamePath = Path.Combine(gogPath, gameFolderName);
                
                if (_fileService.DirectoryExists(gamePath))
                {
                    if (ValidateGameFolderAsync(gameType, gamePath, cancellationToken).Result)
                        return gamePath;
                }
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Gibt Standard-Suchpfade zurück (relativ zu Laufwerk-Root)
        /// </summary>
        private List<string> GetStandardSearchPaths(GameType gameType)
        {
            var paths = new List<string>();
            string gameFolder = gameType == GameType.KCD1 ? "KingdomComeDeliverance" : "KingdomComeDeliverance2";
            
            paths.Add($@"Program Files (x86)\Steam\steamapps\common\{gameFolder}");
            paths.Add($@"Program Files\Steam\steamapps\common\{gameFolder}");
            paths.Add($@"Program Files (x86)\GOG Galaxy\Games\{gameFolder}");
            paths.Add($@"Program Files\GOG Galaxy\Games\{gameFolder}");
            
            return paths;
        }

        /// <summary>
        /// Fordert manuellen Pfad vom Benutzer an
        /// </summary>
        private async Task<string?> RequestManualPathAsync(GameType gameType, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                string gameName = gameType == GameType.KCD1 ? "Kingdom Come: Deliverance" : "Kingdom Come: Deliverance II";
                string description = $"Bitte wählen Sie den Installationsordner für {gameName} aus.\n\n" +
                                   $"Der Ordner sollte die Datei 'KingdomCome.exe' enthalten.";

                string? selectedPath = _dialogService.ShowFolderPicker(description);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (ValidateGameFolderAsync(gameType, selectedPath, cancellationToken).Result)
                    {
                        SaveGamePathAsync(gameType, selectedPath, "Manuell").Wait(cancellationToken);
                        return selectedPath;
                    }
                    else
                    {
                        _dialogService.ShowMessageBox(
                            Resources.Messages.ErrorInvalidPath ?? "Der ausgewählte Ordner enthält keine gültige Spiel-Installation.",
                            Resources.Messages.DialogTitleError ?? "Ungültiger Pfad",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Speichert Game-Pfad in Settings
        /// </summary>
        private async Task SaveGamePathAsync(GameType gameType, string path, string source)
        {
            await Task.Run(() =>
            {
                if (gameType == GameType.KCD1)
                    _settings.GamePath_KCD1 = path;
                else
                    _settings.GamePath_KCD2 = path;
                _settings.Save();
                _logger.Info($"Game-Pfad gespeichert ({gameType}): {path} (Quelle: {source})");
            });
        }

        /// <summary>
        /// Gibt die Quelle des gespeicherten Pfads zurück (für Logging)
        /// </summary>
        private string GetPathSource(GameType gameType)
        {
            string? path = gameType == GameType.KCD1 ? _settings.GamePath_KCD1 : _settings.GamePath_KCD2;
            if (string.IsNullOrEmpty(path))
                return "Nicht gespeichert";
            
            // Einfache Heuristik basierend auf Pfad
            if (path.Contains("Steam", StringComparison.OrdinalIgnoreCase))
                return "Steam";
            if (path.Contains("GOG", StringComparison.OrdinalIgnoreCase))
                return "GOG";
            return "Manuell";
        }

        /// <summary>
        /// Erstellt einen Installations-Deskriptor mit korrekten Pfaden
        /// 
        /// WICHTIG: ModsPath ist NICHT Bin\Mods, sondern direkt Root\Mods!
        /// </summary>
        private GameInstallDescriptor CreateInstallDescriptor(GameType gameType, string rootPath, string executablePath)
        {
            // Bin-Pfad aus Executable-Pfad extrahieren
            string binPath = Path.GetDirectoryName(executablePath) ?? string.Empty;

            var descriptor = new GameInstallDescriptor
            {
                GameType = gameType,
                RootPath = rootPath,
                ExecutablePath = executablePath,
                BinPath = binPath,
                // WICHTIG: Mods-Ordner ist direkt im Root, nicht in Bin!
                ModsPath = Path.Combine(rootPath, "Mods"),
                DataPath = Path.Combine(rootPath, "Data")
            };

            // Stelle sicher, dass der Mods-Ordner existiert
            if (!_fileService.DirectoryExists(descriptor.ModsPath))
            {
                _fileService.CreateDirectory(descriptor.ModsPath);
                _logger.Info($"Mods-Ordner erstellt: {descriptor.ModsPath}");
            }

            return descriptor;
        }

        public async Task<string?> GetGameVersionAsync(GameType gameType, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var install = gameType == GameType.KCD1 ? KCD1Install : KCD2Install;
                if (install == null || string.IsNullOrEmpty(install.ExecutablePath) || !_fileService.FileExists(install.ExecutablePath))
                {
                    return null;
                }

                try
                {
                    // Versuche zuerst version.txt zu lesen (falls vorhanden)
                    string versionTxtPath = Path.Combine(install.RootPath, "version.txt");
                    if (_fileService.FileExists(versionTxtPath))
                    {
                        string versionText = _fileService.ReadAllTextAsync(versionTxtPath, cancellationToken).Result;
                        if (!string.IsNullOrWhiteSpace(versionText))
                        {
                            return versionText.Trim();
                        }
                    }

                    // Fallback: Versionsinformationen aus der .exe-Datei lesen
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(install.ExecutablePath);
                    return versionInfo.FileVersion ?? versionInfo.ProductVersion;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler beim Ermitteln der Spiel-Version: {ex.Message}", ex);
                    return null;
                }
            }, cancellationToken);
        }

        public async Task<bool> IsModCompatibleAsync(string modGameVersion, GameType gameType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modGameVersion))
            {
                return true; // Wenn keine Version angegeben, als kompatibel annehmen
            }

            string? gameVersion = await GetGameVersionAsync(gameType, cancellationToken);
            if (string.IsNullOrEmpty(gameVersion))
            {
                return true; // Wenn Spiel-Version nicht ermittelt werden kann, als kompatibel annehmen
            }

            // Einfache Versionsvergleiche (kann erweitert werden)
            // Entferne Build-Nummern für Vergleich
            string normalizedModVersion = NormalizeVersion(modGameVersion);
            string normalizedGameVersion = NormalizeVersion(gameVersion);

            return normalizedModVersion == normalizedGameVersion || 
                   normalizedModVersion.StartsWith(normalizedGameVersion) ||
                   normalizedGameVersion.StartsWith(normalizedModVersion);
        }

        private string NormalizeVersion(string version)
        {
            // Entferne Build-Nummern und extrahiere Haupt- und Nebenversion
            var match = Regex.Match(version, @"(\d+)\.(\d+)");
            if (match.Success)
            {
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
            }
            return version;
        }
    }
}
