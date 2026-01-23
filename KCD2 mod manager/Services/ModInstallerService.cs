using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von IModInstallerService für Mod-Installations-Operationen
    /// </summary>
    public class ModInstallerService : IModInstallerService
    {
        private readonly IFileService _fileService;
        private readonly IModManifestService _manifestService;
        private readonly IAppSettings _settings;
        private readonly IDialogService _dialogService;
        private readonly IGameInstallService _gameInstallService;
        private readonly ILog _logger;
        private readonly IModCategoryAssignmentService _categoryAssignmentService;
        private const string ModNotesFileName = "mod_versions.json";
        private const string ModNotesJsonFileName = "mod_notes.json";

        public ModInstallerService(
            IFileService fileService,
            IModManifestService manifestService,
            IAppSettings settings,
            IDialogService dialogService,
            IGameInstallService gameInstallService,
            ILog logger,
            IModCategoryAssignmentService categoryAssignmentService)
        {
            _fileService = fileService;
            _manifestService = manifestService;
            _settings = settings;
            _dialogService = dialogService;
            _gameInstallService = gameInstallService;
            _logger = logger;
            _categoryAssignmentService = categoryAssignmentService;
        }

        public async Task<Mod?> ProcessModFileAsync(string filePath, string modFolder, CancellationToken cancellationToken = default)
        {
            var tempDir = _fileService.Combine(System.IO.Path.GetTempPath(), _fileService.GetFileNameWithoutExtension(filePath));
            _fileService.CreateDirectory(tempDir);

            try
            {
                int modNumber = ExtractModNumberFromFileName(filePath);
                if (modNumber == -1)
                {
                    string? input = _dialogService.ShowInputDialog("Enter Mod Number:", "Mod Number", "");
                    if (string.IsNullOrEmpty(input) || !int.TryParse(input, out modNumber))
                    {
                        _logger.Warning("Keine gültige Mod-Nummer eingegeben. Installation abgebrochen.");
                        _fileService.DeleteDirectory(tempDir, true);
                        return null;
                    }
                }

                await _fileService.ExtractArchiveAsync(filePath, tempDir, cancellationToken);

                var manifestPath = _fileService.EnumerateFiles(tempDir, "mod.manifest", SearchOption.AllDirectories).FirstOrDefault();
                if (manifestPath == null)
                {
                    bool hasPakFiles = _fileService.EnumerateFiles(tempDir, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = await _manifestService.GenerateManifestAsync(tempDir, cancellationToken);
                    }
                    else
                    {
                        _logger.Error("Mod ist nicht kompatibel (kein mod.manifest und keine .pak-Dateien gefunden).");
                        _fileService.DeleteDirectory(tempDir, true);
                        return null;
                    }
                }

                await _manifestService.CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);

                var manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                if (manifestData == null || string.IsNullOrEmpty(manifestData.Value.version))
                {
                    var result = _dialogService.ShowMessageBox(
                        "The mod.manifest file is invalid. Do you want to attempt to generate a new manifest?",
                        "Invalid Manifest",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == true)
                    {
                        manifestPath = await _manifestService.GenerateManifestAsync(tempDir, cancellationToken);
                        manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                        if (manifestData == null || string.IsNullOrEmpty(manifestData.Value.version))
                        {
                            _logger.Error("Fehler beim Generieren eines gültigen Manifests. Installation abgebrochen.");
                            _fileService.DeleteDirectory(tempDir, true);
                            return null;
                        }
                    }
                    else
                    {
                        _logger.Warning("Installation abgebrochen.");
                        _fileService.DeleteDirectory(tempDir, true);
                        return null;
                    }
                }

                var modId = manifestData.Value.modId;
                var modName = manifestData.Value.name;
                var modVersion = manifestData.Value.version;
                var modTargetPath = _settings.EnableFileRenaming
                    ? _fileService.Combine(modFolder, modId)
                    : GetOriginalModTargetPath(tempDir, manifestPath, filePath, modFolder);

                if (_fileService.DirectoryExists(modTargetPath))
                {
                    var existingManifestPath = _fileService.Combine(modTargetPath, "mod.manifest");
                    var existingInfo = await _manifestService.ParseManifestAsync(existingManifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                    if (existingInfo != null && Version.TryParse(existingInfo.Value.version, out var existingVersion) &&
                        Version.TryParse(modVersion, out var newVersion) && newVersion <= existingVersion)
                    {
                        _logger.Warning($"Mod {modName} ist bereits mit einer gleichwertigen oder neueren Version installiert. Installation abgebrochen.");
                        _fileService.DeleteDirectory(tempDir, true);
                        return null;
                    }
                    _fileService.DeleteDirectory(modTargetPath, true);
                }

                var targetManifestPath = _fileService.Combine(modTargetPath, "mod.manifest");
                _fileService.CreateDirectory(modTargetPath);
                _fileService.MoveFile(manifestPath, targetManifestPath);
                await _fileService.CopyModFilesAsync(_fileService.GetDirectoryName(manifestPath)!, modTargetPath, cancellationToken);

                await SaveModVersionAsync(modId, modVersion, modNumber, _fileService.GetFileName(filePath), modFolder, true, cancellationToken);

                return new Mod
                {
                    Id = modId,
                    Name = modName,
                    Version = modVersion,
                    Path = modTargetPath,
                    ModNumber = modNumber,
                    IsEnabled = false
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Installieren des Mods: {ex.Message}", ex);
                return null;
            }
            finally
            {
                if (_fileService.DirectoryExists(tempDir))
                    _fileService.DeleteDirectory(tempDir, true);
            }
        }

        public async Task<Mod?> ProcessModFolderAsync(string folderPath, string modFolder, CancellationToken cancellationToken = default)
        {
            try
            {
                bool isWorkshopSource = IsWorkshopPath(folderPath);
                var manifestPath = _fileService.EnumerateFiles(folderPath, "mod.manifest", SearchOption.AllDirectories).FirstOrDefault();
                if (manifestPath == null)
                {
                    bool hasPakFiles = _fileService.EnumerateFiles(folderPath, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = await _manifestService.GenerateManifestAsync(folderPath, cancellationToken);
                    }
                    else
                    {
                        _logger.Error("Dieser Mod-Ordner ist nicht kompatibel (fehlendes mod.manifest und keine .pak-Dateien gefunden).");
                        return null;
                    }
                }

                await _manifestService.CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);

                string modName;
                string modVersion;
                string modId;
                bool hasExplicitModId = true;

                if (isWorkshopSource)
                {
                    var manifestData = await _manifestService.ParseManifestWithModIdInfoAsync(manifestPath, cancellationToken, allowWriteModId: false);
                    if (manifestData == null || string.IsNullOrEmpty(manifestData.Value.version))
                    {
                        _logger.Error("Ungültige mod.manifest-Datei. Installation abgebrochen.");
                        return null;
                    }

                    modName = manifestData.Value.name;
                    modVersion = manifestData.Value.version;
                    modId = manifestData.Value.modId;
                    hasExplicitModId = manifestData.Value.hasExplicitModId;
                }
                else
                {
                    var manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                    if (manifestData == null || string.IsNullOrEmpty(manifestData.Value.version))
                    {
                        _logger.Error("Ungültige mod.manifest-Datei. Installation abgebrochen.");
                        return null;
                    }

                    modName = manifestData.Value.name;
                    modVersion = manifestData.Value.version;
                    modId = manifestData.Value.modId;
                }

                if (string.IsNullOrEmpty(modVersion))
                {
                    _logger.Error("Ungültige mod.manifest-Datei. Installation abgebrochen.");
                    return null;
                }

                if (isWorkshopSource && !hasExplicitModId)
                {
                    string workshopId = _fileService.GetFileName(folderPath);
                    modId = GetWorkshopDerivedModId(workshopId, modName, folderPath);
                    await _manifestService.EnsureManifestModIdAsync(manifestPath, modId, cancellationToken);
                }

                var modTargetPath = isWorkshopSource
                    ? _fileService.Combine(modFolder, _fileService.GetFileName(folderPath))
                    : (_settings.EnableFileRenaming
                        ? _fileService.Combine(modFolder, modId)
                        : _fileService.Combine(modFolder, _fileService.GetFileName(folderPath)));

                if (_fileService.DirectoryExists(modTargetPath))
                {
                    _fileService.DeleteDirectory(modTargetPath, true);
                }

                _fileService.CreateDirectory(modTargetPath);
                await _fileService.CopyDirectoryAsync(folderPath, modTargetPath, cancellationToken);

                var mod = new Mod
                {
                    Id = modId,
                    Name = modName,
                    Version = modVersion,
                    Path = modTargetPath,
                    IsEnabled = false,
                    IsWorkshopMod = isWorkshopSource,
                    WorkshopId = isWorkshopSource ? _fileService.GetFileName(folderPath) : string.Empty
                };

                if (isWorkshopSource)
                {
                    _logger.Info($"Workshop-Mod-Import erkannt: {modName} ({folderPath})");
                    await _categoryAssignmentService.MarkWorkshopAsync(mod, cancellationToken);
                }

                return mod;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Verarbeiten des Mod-Ordners: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<Mod?> ProcessModUpdateAsync(string archivePath, string targetDir, string originalId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_fileService.DirectoryExists(targetDir))
                {
                    _fileService.DeleteDirectory(targetDir, true);
                }
                _fileService.CreateDirectory(targetDir);

                await _fileService.ExtractArchiveAsync(archivePath, targetDir, cancellationToken);

                string? manifestPath = _fileService.EnumerateFiles(targetDir, "mod.manifest", SearchOption.AllDirectories).FirstOrDefault();

                if (manifestPath == null)
                {
                    manifestPath = await _manifestService.GenerateManifestAsync(targetDir, cancellationToken);
                }
                else
                {
                    string manifestDir = _fileService.GetDirectoryName(manifestPath)!;
                    if (!manifestDir.Equals(targetDir, StringComparison.OrdinalIgnoreCase))
                    {
                        // WICHTIG: Verschiebe alle Unterordner und Dateien in das Zielverzeichnis
                        foreach (var dir in _fileService.GetDirectories(manifestDir))
                        {
                            string destDir = _fileService.Combine(targetDir, _fileService.GetFileName(dir));
                            _fileService.MoveDirectory(dir, destDir);
                        }
                        foreach (var file in _fileService.EnumerateFiles(manifestDir, "*", SearchOption.TopDirectoryOnly))
                        {
                            string destFile = _fileService.Combine(targetDir, _fileService.GetFileName(file));
                            _fileService.MoveFile(file, destFile);
                        }
                        
                        // WICHTIG: Lösche leeren Extraktions-Ordner nach erfolgreichem Verschieben
                        // Prüfe ob der Ordner leer ist (außer möglichen versteckten Dateien)
                        try
                        {
                            var remainingDirs = _fileService.GetDirectories(manifestDir);
                            var remainingFiles = _fileService.EnumerateFiles(manifestDir, "*", SearchOption.TopDirectoryOnly);
                            if (!remainingDirs.Any() && !remainingFiles.Any())
                            {
                                _fileService.DeleteDirectory(manifestDir, false);
                                _logger.Info($"Leeren Extraktions-Ordner gelöscht: {manifestDir}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Nicht kritisch - nur loggen
                            _logger.Warning($"Konnte leeren Extraktions-Ordner nicht löschen: {ex.Message}");
                        }
                        
                        manifestPath = _fileService.Combine(targetDir, "mod.manifest");
                    }
                }

                var manifestInfo = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                if (manifestInfo == null)
                {
                    throw new Exception("Fehler beim Parsen des Manifests nach dem Update.");
                }

                return new Mod
                {
                    Id = originalId,
                    Name = manifestInfo.Value.name,
                    Version = manifestInfo.Value.version,
                    Path = targetDir,
                    ModNumber = -1
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Verarbeiten des Mod-Updates: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<List<Mod>> LoadModsAsync(string modFolder, CancellationToken cancellationToken = default)
        {
            if (!_fileService.DirectoryExists(modFolder))
                _fileService.CreateDirectory(modFolder);

            var mods = new List<Mod>();

            string modOrderFileName = _settings.ModOrderEnabled ? "mod_order.txt" : "mod_order_backup.txt";
            var modOrderPath = _fileService.Combine(modFolder, modOrderFileName);
            var modOrderList = new List<(string modId, bool isEnabled)>();
            if (_fileService.FileExists(modOrderPath))
            {
                var lines = await _fileService.ReadAllLinesAsync(modOrderPath, cancellationToken);
                foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var trimmed = line.Trim();
                    bool isEnabled = true;
                    if (trimmed.StartsWith("#"))
                    {
                        isEnabled = false;
                        trimmed = trimmed.Substring(1).Trim();
                    }
                    modOrderList.Add((trimmed, isEnabled));
                }
            }

            var allMods = _fileService.GetDirectories(modFolder)
                .Where(dir =>
                    _fileService.FileExists(_fileService.Combine(dir, "mod.manifest")) ||
                    _fileService.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories).Any());

            var modDictionary = new Dictionary<string, Mod>();
            foreach (var dir in allMods)
            {
                string manifestPath = _fileService.Combine(dir, "mod.manifest");
                if (!_fileService.FileExists(manifestPath))
                {
                    bool hasPakFiles = _fileService.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = await _manifestService.GenerateManifestAsync(dir, cancellationToken);
                    }
                    else
                    {
                        continue;
                    }
                }

                bool isWorkshop = IsWorkshopPath(dir);
                if (isWorkshop)
                {
                    var modInfo = await _manifestService.ParseManifestWithModIdInfoAsync(manifestPath, cancellationToken, allowWriteModId: false);
                    if (modInfo != null)
                    {
                        var modName = modInfo.Value.name;
                        var modVersion = modInfo.Value.version;
                        string workshopId = _fileService.GetFileName(dir);
                        string modId = modInfo.Value.hasExplicitModId
                            ? modInfo.Value.modId
                            : GetWorkshopDerivedModId(workshopId, modName, dir);

                        if (!modInfo.Value.hasExplicitModId)
                        {
                            await _manifestService.EnsureManifestModIdAsync(manifestPath, modId, cancellationToken);
                        }

                        var mod = new Mod
                        {
                            Id = modId,
                            Name = modName,
                            Version = modVersion,
                            Path = dir,
                            IsEnabled = true,
                            IsWorkshopMod = true,
                            WorkshopId = workshopId
                        };

                        // WICHTIG: Lade Metadaten aus mod_versions.json und mod_notes.json pro Mod
                        // Jeder Mod-Ordner kann eigene Dateien haben, aber wir laden aus dem globalen File
                        // Das wird später in LoadModsAsync aufgerufen, nachdem alle Mods geladen sind
                        modDictionary[modId] = mod;
                        _logger.Info($"Workshop-Mod erkannt: {modName} ({dir})");
                    }
                }
                else
                {
                    var modInfo = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken, allowWriteModId: _settings.EnableFileRenaming);
                    if (modInfo != null)
                    {
                        var modId = modInfo.Value.modId;
                        var modName = modInfo.Value.name;
                        var modVersion = modInfo.Value.version;

                        var expectedPath = _fileService.Combine(modFolder, modId);
                        if (_settings.EnableFileRenaming && !dir.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (_fileService.DirectoryExists(expectedPath))
                                {
                                    _fileService.DeleteDirectory(expectedPath, true);
                                }
                                _fileService.MoveDirectory(dir, expectedPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Fehler beim Verschieben des Mod-Ordners von {dir} nach {expectedPath}: {ex.Message}", ex);
                                expectedPath = dir;
                            }
                        }
                        else if (!_settings.EnableFileRenaming)
                        {
                            expectedPath = dir;
                        }

                        var mod = new Mod
                        {
                            Id = modId,
                            Name = modName,
                            Version = modVersion,
                            Path = expectedPath,
                            IsEnabled = true,
                            IsWorkshopMod = false
                        };

                        // WICHTIG: Lade Metadaten aus mod_versions.json und mod_notes.json pro Mod
                        // Jeder Mod-Ordner kann eigene Dateien haben, aber wir laden aus dem globalen File
                        // Das wird später in LoadModsAsync aufgerufen, nachdem alle Mods geladen sind
                        modDictionary[modId] = mod;
                    }
                }
            }

            int index = 1;
            foreach (var (modId, isEnabled) in modOrderList)
            {
                if (modDictionary.ContainsKey(modId))
                {
                    var mod = modDictionary[modId];
                    mod.Number = index++;
                    mod.IsEnabled = isEnabled;
                    mods.Add(mod);
                    modDictionary.Remove(modId);
                }
            }

            foreach (var remainingMod in modDictionary.Values)
            {
                remainingMod.Number = 0;
                mods.Add(remainingMod);
            }

            var selectedInstall = _gameInstallService.SelectedInstall;
            if (selectedInstall != null)
            {
                var workshopMods = await LoadWorkshopModsAsync(selectedInstall, cancellationToken);
                foreach (var workshopMod in workshopMods)
                {
                    if (mods.Any(m => m.Id.Equals(workshopMod.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    workshopMod.Number = 0;
                    mods.Add(workshopMod);
                }
            }

            // WICHTIG: Lade Metadaten aus mod_versions.json und mod_notes.json und mappe sie auf Mods
            await LoadAndMapModMetadataAsync(mods, modFolder, cancellationToken);

            return mods;
        }

        /// <summary>
        /// Lädt Metadaten aus mod_versions.json und mod_notes.json und mappt sie auf Mod-Objekte
        /// WICHTIG: Diese Methode lädt die globalen Dateien und mappt sie basierend auf modId
        /// </summary>
        private async Task LoadAndMapModMetadataAsync(List<Mod> mods, string modFolder, CancellationToken cancellationToken)
        {
            try
            {
                // Lade globale mod_versions.json
                var modVersions = await LoadModVersionsAsync(modFolder, cancellationToken);
                
                // Lade globale mod_notes.json
                var modNotes = await LoadModNotesAsync(modFolder, cancellationToken);

                // Mappe Metadaten auf Mod-Objekte
                foreach (var mod in mods)
                {
                    // Mappe Version-Informationen
                    if (modVersions.TryGetValue(mod.Id, out var versionInfo))
                    {
                        mod.ModNumber = versionInfo.ModNumber;
                        // Version wird nur gesetzt, wenn sie nicht bereits aus dem Manifest geladen wurde
                        // oder wenn sie leer ist
                        if (string.IsNullOrEmpty(mod.Version) && !string.IsNullOrEmpty(versionInfo.Version))
                        {
                            mod.Version = versionInfo.Version;
                        }
                        // UpdateChecksEnabled aus Metadaten laden
                        mod.UpdateChecksEnabled = versionInfo.UpdateChecksEnabled;
                    }

                    // Mappe Notizen
                    if (modNotes.TryGetValue(mod.Id, out var note))
                    {
                        mod.Note = note;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden und Mappen der Mod-Metadaten: {ex.Message}", ex);
                // Fehler nicht weiterwerfen - Mods können auch ohne Metadaten funktionieren
            }
        }

        /// <summary>
        /// Speichert mod_order.txt im Spiel-Mods-Verzeichnis
        /// Format: # für deaktiviert, kein Prefix für aktiviert
        /// WICHTIG: Diese Methode wird nur verwendet, wenn kein Profil aktiv ist.
        /// Bei aktivem Profil wird WriteModOrderToGameFolderAsync aus ProfilesService verwendet.
        /// </summary>
        public async Task SaveModOrderAsync(List<Mod> mods, string modFolder, CancellationToken cancellationToken = default)
        {
            string modOrderFileName = _settings.ModOrderEnabled ? "mod_order.txt" : "mod_order_backup.txt";
            string modOrderPath = _fileService.Combine(modFolder, modOrderFileName);

            // Format: # für deaktiviert, kein Prefix für aktiviert
            var modOrder = mods
                .OrderBy(m => m.Number)
                .Select(mod => mod.IsEnabled ? mod.Id : $"# {mod.Id}")
                .ToList();
            
            // Atomisches Schreiben
            string tempPath = modOrderPath + ".tmp";
            if (_fileService.FileExists(tempPath))
            {
                _fileService.DeleteFile(tempPath);
            }
            
            await _fileService.WriteAllLinesAsync(tempPath, modOrder, cancellationToken);
            
            if (_fileService.FileExists(modOrderPath))
            {
                _fileService.DeleteFile(modOrderPath);
            }
            
            _fileService.MoveFile(tempPath, modOrderPath);
        }

        /// <summary>
        /// Speichert Mod-Version-Metadaten in mod_versions.json
        /// WICHTIG: Speichert UpdateChecksEnabled korrekt - behält existierenden Wert, falls vorhanden
        /// </summary>
        public async Task SaveModVersionAsync(string modId, string version, int modNumber, string installedFileName, string modFolder, bool updateChecksEnabled = true, CancellationToken cancellationToken = default)
        {
            string jsonPath = _fileService.Combine(modFolder, ModNotesFileName);
            Dictionary<string, ModVersionInfo> modData;

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            if (_fileService.FileExists(jsonPath))
            {
                string json = await _fileService.ReadAllTextAsync(jsonPath, cancellationToken);
                modData = JsonSerializer.Deserialize<Dictionary<string, ModVersionInfo>>(json, serializerOptions) ?? new Dictionary<string, ModVersionInfo>();
            }
            else
            {
                modData = new Dictionary<string, ModVersionInfo>();
            }

            // WICHTIG: Behalte existierenden UpdateChecksEnabled-Wert, falls vorhanden
            bool finalUpdateChecksEnabled = updateChecksEnabled;
            if (modData.TryGetValue(modId, out var existingInfo))
            {
                // Behalte existierenden Wert, es sei denn, er wird explizit überschrieben
                // In diesem Fall verwenden wir den übergebenen Parameter
                finalUpdateChecksEnabled = updateChecksEnabled;
            }

            modData[modId] = new ModVersionInfo
            {
                Version = version,
                ModNumber = modNumber,
                FileName = installedFileName,
                UpdateChecksEnabled = finalUpdateChecksEnabled
            };

            // Atomisches Schreiben
            string tempPath = jsonPath + ".tmp";
            if (_fileService.FileExists(tempPath))
            {
                _fileService.DeleteFile(tempPath);
            }

            string jsonOutput = JsonSerializer.Serialize(modData, serializerOptions);
            await _fileService.WriteAllTextAsync(tempPath, jsonOutput, cancellationToken);

            if (_fileService.FileExists(jsonPath))
            {
                _fileService.DeleteFile(jsonPath);
            }

            _fileService.MoveFile(tempPath, jsonPath);
        }

        public async Task<Dictionary<string, ModVersionInfo>> LoadModVersionsAsync(string modFolder, CancellationToken cancellationToken = default)
        {
            string jsonPath = _fileService.Combine(modFolder, ModNotesFileName);
            if (!_fileService.FileExists(jsonPath))
                return new Dictionary<string, ModVersionInfo>();

            try
            {
                string json = await _fileService.ReadAllTextAsync(jsonPath, cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, ModVersionInfo>>(json) ?? new Dictionary<string, ModVersionInfo>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der Mod-Versionen: {ex.Message}", ex);
                return new Dictionary<string, ModVersionInfo>();
            }
        }

        public async Task SaveModNotesAsync(Dictionary<string, string> modNotes, string modFolder, CancellationToken cancellationToken = default)
        {
            string notesPath = _fileService.Combine(modFolder, ModNotesJsonFileName);
            string json = JsonSerializer.Serialize(modNotes, new JsonSerializerOptions { WriteIndented = true });
            await _fileService.WriteAllTextAsync(notesPath, json, cancellationToken);
        }

        public async Task<Dictionary<string, string>> LoadModNotesAsync(string modFolder, CancellationToken cancellationToken = default)
        {
            string notesPath = _fileService.Combine(modFolder, ModNotesJsonFileName);
            if (!_fileService.FileExists(notesPath))
                return new Dictionary<string, string>();

            try
            {
                string json = await _fileService.ReadAllTextAsync(notesPath, cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der Mod-Notizen: {ex.Message}", ex);
                return new Dictionary<string, string>();
            }
        }

        private string GetOriginalModTargetPath(string tempDir, string manifestPath, string filePath, string modFolder)
        {
            string manifestDir = _fileService.GetDirectoryName(manifestPath);
            if (!string.IsNullOrEmpty(manifestDir) && !manifestDir.Equals(tempDir, StringComparison.OrdinalIgnoreCase))
            {
                return _fileService.Combine(modFolder, _fileService.GetFileName(manifestDir));
            }

            string fallbackName = _fileService.GetFileName(tempDir);
            if (string.IsNullOrWhiteSpace(fallbackName))
            {
                fallbackName = _fileService.GetFileNameWithoutExtension(filePath);
            }

            return _fileService.Combine(modFolder, fallbackName);
        }

        private bool IsWorkshopPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace('/', '\\');
            return normalized.IndexOf("\\steamapps\\workshop\\content\\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetWorkshopDerivedModId(string workshopId, string modName, string folderPath)
        {
            string candidateName = modName;
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                candidateName = workshopId;
            }

            if (string.IsNullOrWhiteSpace(candidateName))
            {
                candidateName = _fileService.GetFileName(folderPath);
            }

            return _manifestService.GenerateModId(candidateName ?? string.Empty);
        }

        private async Task<List<Mod>> LoadWorkshopModsAsync(GameInstallDescriptor install, CancellationToken cancellationToken)
        {
            var mods = new List<Mod>();

            string? workshopPath = await GetWorkshopContentPathAsync(install, cancellationToken);
            if (string.IsNullOrEmpty(workshopPath) || !_fileService.DirectoryExists(workshopPath))
            {
                return mods;
            }

            foreach (var dir in _fileService.GetDirectories(workshopPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string manifestPath = _fileService.Combine(dir, "mod.manifest");
                if (!_fileService.FileExists(manifestPath))
                {
                    bool hasPakFiles = _fileService.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = await _manifestService.GenerateManifestAsync(dir, cancellationToken);
                    }
                    else
                    {
                        continue;
                    }
                }

                var modInfo = await _manifestService.ParseManifestWithModIdInfoAsync(manifestPath, cancellationToken, allowWriteModId: false);
                if (modInfo != null)
                {
                    string workshopId = _fileService.GetFileName(dir);
                    string modId = modInfo.Value.hasExplicitModId
                        ? modInfo.Value.modId
                        : GetWorkshopDerivedModId(workshopId, modInfo.Value.name, dir);

                    if (!modInfo.Value.hasExplicitModId)
                    {
                        await _manifestService.EnsureManifestModIdAsync(manifestPath, modId, cancellationToken);
                    }

                    mods.Add(new Mod
                    {
                        Id = modId,
                        Name = modInfo.Value.name,
                        Version = modInfo.Value.version,
                        Path = dir,
                        IsEnabled = true,
                        IsWorkshopMod = true,
                        WorkshopId = workshopId
                    });
                }
            }

            return mods;
        }

        private async Task<string?> GetWorkshopContentPathAsync(GameInstallDescriptor install, CancellationToken cancellationToken)
        {
            string? steamAppsPath = GetSteamAppsPath(install.RootPath);
            if (string.IsNullOrEmpty(steamAppsPath) || !_fileService.DirectoryExists(steamAppsPath))
            {
                return null;
            }

            string? appId = await FindSteamAppIdAsync(steamAppsPath, install.RootPath, cancellationToken);
            if (string.IsNullOrEmpty(appId))
            {
                return null;
            }

            return _fileService.Combine(steamAppsPath, "workshop", "content", appId);
        }

        private string? GetSteamAppsPath(string rootPath)
        {
            string normalized = rootPath.Replace('/', '\\');
            string marker = "\\steamapps\\common";
            int index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return normalized.Substring(0, index + "\\steamapps".Length);
            }

            string? current = rootPath;
            while (!string.IsNullOrEmpty(current) && current != Path.GetPathRoot(current))
            {
                string candidate = _fileService.Combine(current, "steamapps");
                if (_fileService.DirectoryExists(candidate))
                {
                    return candidate;
                }
                current = Path.GetDirectoryName(current);
            }

            return null;
        }

        private async Task<string?> FindSteamAppIdAsync(string steamAppsPath, string rootPath, CancellationToken cancellationToken)
        {
            string installDir = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(installDir))
            {
                return null;
            }

            var manifestFiles = _fileService.EnumerateFiles(steamAppsPath, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
            Regex regex = new Regex("\"installdir\"\\s+\"(?<dir>.+)\"", RegexOptions.IgnoreCase);

            foreach (var manifestPath in manifestFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string[] lines = await _fileService.ReadAllLinesAsync(manifestPath, cancellationToken);
                foreach (var line in lines)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        string dir = match.Groups["dir"].Value.Trim();
                        if (dir.Equals(installDir, StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = _fileService.GetFileName(manifestPath);
                            string appId = fileName.Replace("appmanifest_", "").Replace(".acf", "");
                            return appId;
                        }
                    }
                }
            }

            return null;
        }

        public async Task CreateModsBackupAsync(string modFolder, CancellationToken cancellationToken = default)
        {
            if (!_settings.CreateBackup)
                return;

            try
            {
                string parentDir = _fileService.GetDirectoryName(modFolder) ?? modFolder;
                string backupRoot = _fileService.Combine(parentDir, "Mods_Backup");

                if (!_fileService.DirectoryExists(backupRoot))
                {
                    _fileService.CreateDirectory(backupRoot);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFolder = _fileService.Combine(backupRoot, $"Mods_Backup_{timestamp}");
                _fileService.CreateDirectory(backupFolder);

                await _fileService.CopyDirectoryAsync(modFolder, backupFolder, cancellationToken);

                int maxBackups = _settings.BackupMaxCount;
                var backupFolders = _fileService.GetDirectories(backupRoot)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.CreationTime)
                    .ToList();

                if (backupFolders.Count > maxBackups)
                {
                    foreach (var oldBackup in backupFolders.Skip(maxBackups))
                    {
                        _fileService.DeleteDirectory(oldBackup.FullName, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Erstellen des Mod-Backups: {ex.Message}", ex);
            }
        }

        public int ExtractModNumberFromFileName(string fileName)
        {
            var match = Regex.Match(_fileService.GetFileNameWithoutExtension(fileName), @"\b(\d+)\b");
            return match.Success ? int.Parse(match.Value) : -1;
        }

        public string ExtractVersionFromFileName(string fileName)
        {
            var parts = _fileService.GetFileNameWithoutExtension(fileName).Split('-');
            if (parts.Length >= 4)
            {
                return parts[2];
            }
            return "0";
        }
    }
}

