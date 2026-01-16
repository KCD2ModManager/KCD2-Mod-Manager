using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für die Verwaltung von Mod-Profilen
    /// 
    /// WICHTIG: Profile sind pro Spiel (KCD1/KCD2) getrennt gespeichert.
    /// Verzeichnisstruktur:
    ///   Profiles/
    ///     KCD1/
    ///       Default/
    ///         mod_order.txt
    ///       <Andere Profile>/
    ///         mod_order.txt
    ///     KCD2/
    ///       Default/
    ///         mod_order.txt
    ///       <Andere Profile>/
    ///         mod_order.txt
    /// 
    /// Jedes Profil hat sein eigenes Verzeichnis mit:
    ///   - profile.json (Metadaten: ProfileName, ActiveMods, LoadOrder)
    ///   - mod_order.txt (Format: # für deaktiviert, kein Prefix für aktiviert)
    /// 
    /// Beim Aktivieren eines Profils wird dessen mod_order.txt in das Spiel-Mods-Verzeichnis kopiert.
    /// </summary>
    public class ProfilesService : IProfilesService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly IAppSettings _settings;
        private readonly string _profilesRootDirectory;

        public ProfilesService(IFileService fileService, ILog logger, IAppSettings settings)
        {
            _fileService = fileService;
            _logger = logger;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // Pfad: %AppData%/KCDModManager/profiles/
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string managerDataPath = Path.Combine(appDataPath, "KCDModManager");
            _profilesRootDirectory = Path.Combine(managerDataPath, "profiles");

            // Verzeichnis erstellen, falls es nicht existiert
            if (!_fileService.DirectoryExists(_profilesRootDirectory))
            {
                _fileService.CreateDirectory(_profilesRootDirectory);
            }
        }

        /// <summary>
        /// Gibt das Verzeichnis für ein bestimmtes Spiel zurück
        /// </summary>
        private string GetGameProfilesDirectory(GameType gameType)
        {
            string gameDir = gameType == GameType.KCD1 ? "KCD1" : "KCD2";
            string gameProfilesDir = Path.Combine(_profilesRootDirectory, gameDir);
            
            // Verzeichnis erstellen, falls es nicht existiert
            if (!_fileService.DirectoryExists(gameProfilesDir))
            {
                _fileService.CreateDirectory(gameProfilesDir);
            }
            
            return gameProfilesDir;
        }

        /// <summary>
        /// Gibt das Verzeichnis für ein spezifisches Profil zurück
        /// </summary>
        private string GetProfileDirectory(GameType gameType, string profileName)
        {
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string gameProfilesDir = GetGameProfilesDirectory(gameType);
            string profileDir = Path.Combine(gameProfilesDir, safeName);
            
            // Verzeichnis erstellen, falls es nicht existiert
            if (!_fileService.DirectoryExists(profileDir))
            {
                _fileService.CreateDirectory(profileDir);
            }
            
            return profileDir;
        }

        /// <summary>
        /// Gibt den Pfad zur profile.json für ein Profil zurück
        /// </summary>
        private string GetProfileJsonPath(GameType gameType, string profileName)
        {
            string profileDir = GetProfileDirectory(gameType, profileName);
            return Path.Combine(profileDir, "profile.json");
        }

        /// <summary>
        /// Gibt den Pfad zur mod_order.txt für ein Profil zurück
        /// </summary>
        private string GetProfileModOrderPath(GameType gameType, string profileName)
        {
            string profileDir = GetProfileDirectory(gameType, profileName);
            return Path.Combine(profileDir, "mod_order.txt");
        }

        public async Task<List<ModProfile>> LoadProfilesAsync(GameType gameType, CancellationToken cancellationToken = default)
        {
            var profiles = new List<ModProfile>();

            try
            {
                string gameProfilesDir = GetGameProfilesDirectory(gameType);
                
                if (!_fileService.DirectoryExists(gameProfilesDir))
                {
                    return profiles;
                }

                // Durchsuche alle Unterverzeichnisse nach profile.json
                var profileDirs = _fileService.GetDirectories(gameProfilesDir);
                
                foreach (var profileDir in profileDirs)
                {
                    try
                    {
                        string profileJsonPath = Path.Combine(profileDir, "profile.json");
                        if (_fileService.FileExists(profileJsonPath))
                        {
                            string json = await _fileService.ReadAllTextAsync(profileJsonPath, cancellationToken);
                            var profile = JsonSerializer.Deserialize<ModProfile>(json);
                            if (profile != null)
                            {
                                profiles.Add(profile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Fehler beim Laden des Profils aus {profileDir}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der Profile für {gameType}: {ex.Message}", ex);
            }

            return profiles.OrderBy(p => p.ProfileName).ToList();
        }

        public async Task SaveProfileAsync(GameType gameType, ModProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                profile.LastModified = DateTime.UtcNow.ToString("O");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(profile, options);
                string profilePath = GetProfileJsonPath(gameType, profile.ProfileName);

                // Atomisches Schreiben
                string tempPath = profilePath + ".tmp";
                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }

                await _fileService.WriteAllTextAsync(tempPath, json, cancellationToken);

                if (_fileService.FileExists(profilePath))
                {
                    _fileService.DeleteFile(profilePath);
                }

                _fileService.MoveFile(tempPath, profilePath);
                
                _logger.Info($"Profil '{profile.ProfileName}' für {gameType} gespeichert: {profilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern des Profils {profile.ProfileName} für {gameType}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeleteProfileAsync(GameType gameType, string profileName, CancellationToken cancellationToken = default)
        {
            try
            {
                string profileDir = GetProfileDirectory(gameType, profileName);
                
                // Lösche das gesamte Profil-Verzeichnis
                if (_fileService.DirectoryExists(profileDir))
                {
                    // Lösche alle Dateien im Verzeichnis
                    var files = _fileService.EnumerateFiles(profileDir, "*", System.IO.SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        _fileService.DeleteFile(file);
                    }
                    
                    // Versuche das Verzeichnis zu löschen (kann fehlschlagen, wenn noch geöffnet)
                    try
                    {
                        _fileService.DeleteDirectory(profileDir, recursive: true);
                    }
                    catch
                    {
                        // Ignoriere Fehler beim Löschen des Verzeichnisses
                    }
                }
                
                _logger.Info($"Profil '{profileName}' für {gameType} gelöscht");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Löschen des Profils {profileName} für {gameType}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ModProfile?> LoadProfileAsync(GameType gameType, string profileName, CancellationToken cancellationToken = default)
        {
            try
            {
                string profilePath = GetProfileJsonPath(gameType, profileName);
                if (!_fileService.FileExists(profilePath))
                {
                    return null;
                }

                string json = await _fileService.ReadAllTextAsync(profilePath, cancellationToken);
                return JsonSerializer.Deserialize<ModProfile>(json);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden des Profils {profileName} für {gameType}: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<ModProfile> CreateProfileFromCurrentStateAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            var profile = new ModProfile
            {
                ProfileName = profileName,
                ActiveMods = mods.Where(m => !m.IsWorkshopMod && m.IsEnabled).Select(m => m.Id).ToList(),
                LoadOrder = mods.Where(m => !m.IsWorkshopMod).OrderBy(m => m.Number).Select(m => m.Id).ToList(),
                SeparatorsAfterModIds = mods
                    .Where(m => !m.IsWorkshopMod && m.HasSeparatorAfter)
                    .Select(m => m.Id)
                    .ToList()
            };

            await SaveProfileAsync(gameType, profile, cancellationToken);
            
            // Speichere auch mod_order.txt für dieses Profil
            await SaveProfileModOrderAsync(gameType, profileName, mods, cancellationToken);
            
            return profile;
        }

        public async Task ApplyProfileAsync(ModProfile profile, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            // Erstelle Dictionary für schnellen Zugriff
            var modDict = mods.ToDictionary(m => m.Id, m => m);

            // Setze alle Mods auf inaktiv
            foreach (var mod in mods)
            {
                mod.IsEnabled = false;
            }

            // Aktiviere Mods aus dem Profil
            foreach (var modId in profile.ActiveMods)
            {
                if (modDict.TryGetValue(modId, out var mod))
                {
                    mod.IsEnabled = true;
                }
            }

            // Wende Lade-Reihenfolge an
            int order = 1;
            foreach (var modId in profile.LoadOrder)
            {
                if (modDict.TryGetValue(modId, out var mod))
                {
                    mod.Number = order++;
                }
            }

            // Mods, die nicht im Profil sind, am Ende hinzufügen
            foreach (var mod in mods)
            {
                if (!profile.LoadOrder.Contains(mod.Id))
                {
                    mod.Number = order++;
                }
            }
        }

        /// <summary>
        /// Speichert mod_order.txt für ein Profil im Profil-Verzeichnis
        /// Format: # für deaktiviert, kein Prefix für aktiviert
        /// </summary>
        public async Task SaveProfileModOrderAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            try
            {
                string modOrderPath = GetProfileModOrderPath(gameType, profileName);

                var lines = new List<string>();
                foreach (var mod in mods.OrderBy(m => m.Number))
                {
                    // Format: # disabled_mod oder enabled_mod
                    string line = mod.IsEnabled ? mod.Id : $"# {mod.Id}";
                    lines.Add(line);
                }

                // Atomisches Schreiben
                string tempPath = modOrderPath + ".tmp";
                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }
                
                await _fileService.WriteAllLinesAsync(tempPath, lines, cancellationToken);
                
                if (_fileService.FileExists(modOrderPath))
                {
                    _fileService.DeleteFile(modOrderPath);
                }
                
                _fileService.MoveFile(tempPath, modOrderPath);
                _logger.Info($"mod_order.txt für Profil '{profileName}' ({gameType}) gespeichert: {modOrderPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern der mod_order.txt für Profil '{profileName}' ({gameType}): {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Lädt mod_order.txt für ein Profil aus dem Profil-Verzeichnis
        /// Format: # für deaktiviert, kein Prefix für aktiviert
        /// </summary>
        public async Task LoadProfileModOrderAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            try
            {
                string modOrderPath = GetProfileModOrderPath(gameType, profileName);

                if (!_fileService.FileExists(modOrderPath))
                {
                    _logger.Info($"Keine mod_order.txt für Profil '{profileName}' ({gameType}) gefunden, verwende Standard-Reihenfolge");
                    return;
                }

                var modDict = mods.ToDictionary(m => m.Id, m => m);
                var lines = await _fileService.ReadAllLinesAsync(modOrderPath, cancellationToken);

                int order = 1;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string trimmedLine = line.Trim();
                    bool isEnabled = !trimmedLine.StartsWith("#");
                    string modId = isEnabled ? trimmedLine : trimmedLine.Substring(1).Trim();

                    if (modDict.TryGetValue(modId, out var mod))
                    {
                        mod.Number = order++;
                        mod.IsEnabled = isEnabled;
                    }
                }

                _logger.Info($"mod_order.txt für Profil '{profileName}' ({gameType}) geladen: {lines.Length} Einträge");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der mod_order.txt für Profil '{profileName}' ({gameType}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Schreibt mod_order.txt in das Spiel-Mods-Verzeichnis (für das aktive Profil)
        /// WICHTIG: Respektiert das ModOrderEnabled-Setting:
        /// - Wenn ModOrderEnabled=true: Schreibt in mod_order.txt
        /// - Wenn ModOrderEnabled=false: Schreibt in mod_order_backup.txt und löscht mod_order.txt
        /// </summary>
        public async Task WriteModOrderToGameFolderAsync(string modFolder, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            try
            {
                // Bestimme Dateiname basierend auf Setting
                string modOrderFileName = _settings.ModOrderEnabled ? "mod_order.txt" : "mod_order_backup.txt";
                string modOrderPath = Path.Combine(modFolder, modOrderFileName);
                string modOrderMainPath = Path.Combine(modFolder, "mod_order.txt");

                var lines = new List<string>();
                foreach (var mod in mods.Where(m => !m.IsWorkshopMod).OrderBy(m => m.Number))
                {
                    // Format: # für deaktiviert, kein Prefix für aktiviert
                    string line = mod.IsEnabled ? mod.Id : $"# {mod.Id}";
                    lines.Add(line);
                }

                // Atomisches Schreiben
                string tempPath = modOrderPath + ".tmp";
                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }
                
                await _fileService.WriteAllLinesAsync(tempPath, lines, cancellationToken);
                
                if (_fileService.FileExists(modOrderPath))
                {
                    _fileService.DeleteFile(modOrderPath);
                }
                
                _fileService.MoveFile(tempPath, modOrderPath);
                
                // WICHTIG: Wenn ModOrderEnabled=false, lösche mod_order.txt falls vorhanden
                if (!_settings.ModOrderEnabled && _fileService.FileExists(modOrderMainPath))
                {
                    _fileService.DeleteFile(modOrderMainPath);
                    _logger.Info($"mod_order.txt gelöscht (ModOrderEnabled=false)");
                }
                
                _logger.Info($"Mod-Order in Spiel-Mods-Verzeichnis geschrieben: {modOrderPath} (ModOrderEnabled={_settings.ModOrderEnabled})");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Schreiben der Mod-Order in Spiel-Mods-Verzeichnis: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Erstellt ein Standard-Profil, falls noch keines existiert
        /// WICHTIG: Wird automatisch beim Start aufgerufen, wenn kein Profil vorhanden ist
        /// </summary>
        public async Task<ModProfile> EnsureDefaultProfileAsync(GameType gameType, List<Mod> mods, CancellationToken cancellationToken = default)
        {
            const string defaultProfileName = "Default";
            
            // Prüfe, ob bereits ein Default-Profil existiert
            var existingProfile = await LoadProfileAsync(gameType, defaultProfileName, cancellationToken);
            if (existingProfile != null)
            {
                return existingProfile;
            }

            // Erstelle neues Default-Profil mit allen aktuell aktivierten Mods
            _logger.Info($"Erstelle Standard-Profil '{defaultProfileName}' für {gameType}");
            var profile = await CreateProfileFromCurrentStateAsync(gameType, defaultProfileName, mods, cancellationToken);
            
            return profile;
        }
    }
}
