using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für die Erkennung von KCD1 und KCD2 Installationen mit korrekten Pfadstrukturen
    /// 
    /// WICHTIG: Die Mods-Ordner befinden sich NICHT in Bin\Mods, sondern direkt im Root-Verzeichnis:
    /// - KCD1: ...\KingdomComeDeliverance\Mods
    /// - KCD2: ...\KingdomComeDeliverance2\Mods
    /// 
    /// Diese Logik muss für beide Spiele konsistent sein, nur die Root-Pfade unterscheiden sich.
    /// </summary>
    public class GameInstallService : IGameInstallService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;

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

        public GameInstallService(IFileService fileService, ILog logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public async Task DetectInstalledGamesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                // Steam-Pfade prüfen
                string steamPath = @"C:\Program Files (x86)\Steam\steamapps\common";
                string kcd1SteamRoot = Path.Combine(steamPath, "KingdomComeDeliverance");
                string kcd2SteamRoot = Path.Combine(steamPath, "KingdomComeDeliverance2");

                // GOG-Pfade prüfen
                string gogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games");
                string kcd1GogRoot = Path.Combine(gogPath, "Kingdom Come Deliverance");
                string kcd2GogRoot = Path.Combine(gogPath, "Kingdom Come Deliverance II");

                // KCD1 erkennen
                GameInstallDescriptor? kcd1Descriptor = null;
                
                // Steam KCD1
                if (_fileService.DirectoryExists(kcd1SteamRoot))
                {
                    string exePath = Path.Combine(kcd1SteamRoot, "Bin", "Win64", "KingdomCome.exe");
                    if (_fileService.FileExists(exePath))
                    {
                        kcd1Descriptor = CreateInstallDescriptor(GameType.KCD1, kcd1SteamRoot, exePath);
                    }
                }
                // GOG KCD1
                else if (_fileService.DirectoryExists(kcd1GogRoot))
                {
                    string exePath = Path.Combine(kcd1GogRoot, "Bin", "Win64", "KingdomCome.exe");
                    if (_fileService.FileExists(exePath))
                    {
                        kcd1Descriptor = CreateInstallDescriptor(GameType.KCD1, kcd1GogRoot, exePath);
                    }
                }

                // KCD2 erkennen
                GameInstallDescriptor? kcd2Descriptor = null;
                
                // Steam KCD2
                if (_fileService.DirectoryExists(kcd2SteamRoot))
                {
                    // KCD2 kann verschiedene Bin-Pfade haben
                    string[] possibleBinPaths = new[]
                    {
                        Path.Combine(kcd2SteamRoot, "Bin", "Win64MasterMasterSteamPGO", "KingdomCome.exe"),
                        Path.Combine(kcd2SteamRoot, "Bin", "Win64", "KingdomCome.exe")
                    };

                    foreach (var exePath in possibleBinPaths)
                    {
                        if (_fileService.FileExists(exePath))
                        {
                            kcd2Descriptor = CreateInstallDescriptor(GameType.KCD2, kcd2SteamRoot, exePath);
                            break;
                        }
                    }
                }
                // GOG KCD2
                else if (_fileService.DirectoryExists(kcd2GogRoot))
                {
                    string[] possibleBinPaths = new[]
                    {
                        Path.Combine(kcd2GogRoot, "Bin", "Win64MasterMasterSteamPGO", "KingdomCome.exe"),
                        Path.Combine(kcd2GogRoot, "Bin", "Win64", "KingdomCome.exe")
                    };

                    foreach (var exePath in possibleBinPaths)
                    {
                        if (_fileService.FileExists(exePath))
                        {
                            kcd2Descriptor = CreateInstallDescriptor(GameType.KCD2, kcd2GogRoot, exePath);
                            break;
                        }
                    }
                }

                // Installations-Deskriptoren setzen
                KCD1Install = kcd1Descriptor;
                KCD2Install = kcd2Descriptor;

                // InstallType setzen
                if (kcd1Descriptor != null && kcd2Descriptor != null)
                {
                    InstallType = GameInstallType.Both;
                }
                else if (kcd1Descriptor != null)
                {
                    InstallType = GameInstallType.KCD1;
                    SelectedGame = GameType.KCD1;
                }
                else if (kcd2Descriptor != null)
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
