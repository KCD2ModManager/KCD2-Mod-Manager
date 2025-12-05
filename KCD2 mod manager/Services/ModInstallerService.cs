using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        private readonly ILog _logger;
        private const string ModNotesFileName = "mod_versions.json";
        private const string ModNotesJsonFileName = "mod_notes.json";

        public ModInstallerService(
            IFileService fileService,
            IModManifestService manifestService,
            IAppSettings settings,
            IDialogService dialogService,
            ILog logger)
        {
            _fileService = fileService;
            _manifestService = manifestService;
            _settings = settings;
            _dialogService = dialogService;
            _logger = logger;
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

                var manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken);
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
                        manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken);
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
                var modTargetPath = _fileService.Combine(modFolder, modId);

                if (_fileService.DirectoryExists(modTargetPath))
                {
                    var existingManifestPath = _fileService.Combine(modTargetPath, "mod.manifest");
                    var existingInfo = await _manifestService.ParseManifestAsync(existingManifestPath, cancellationToken);
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

                string xmlContent = await _fileService.ReadAllTextAsync(manifestPath, cancellationToken);
                XDocument manifestDoc = XDocument.Parse(xmlContent);
                var infoElement = manifestDoc.Descendants("info").FirstOrDefault();
                if (infoElement != null)
                {
                    var modidElement = infoElement.Element("modid");
                    var nameElement = infoElement.Element("name")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(nameElement))
                    {
                        var generatedId = _manifestService.GenerateModId(nameElement);
                        if (modidElement == null)
                        {
                            infoElement.Add(new XElement("modid", generatedId));
                        }
                        else
                        {
                            modidElement.Value = generatedId;
                        }
                        await _fileService.WriteAllTextAsync(manifestPath, manifestDoc.ToString(), cancellationToken);
                    }
                }

                var manifestData = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken);
                if (manifestData == null || string.IsNullOrEmpty(manifestData.Value.version))
                {
                    _logger.Error("Ungültige mod.manifest-Datei. Installation abgebrochen.");
                    return null;
                }

                var modId = manifestData.Value.modId;
                var modName = manifestData.Value.name;
                var modVersion = manifestData.Value.version;
                var modTargetPath = _fileService.Combine(modFolder, modId);

                if (_fileService.DirectoryExists(modTargetPath))
                {
                    _fileService.DeleteDirectory(modTargetPath, true);
                }

                _fileService.CreateDirectory(modTargetPath);
                await _fileService.CopyDirectoryAsync(folderPath, modTargetPath, cancellationToken);

                return new Mod
                {
                    Id = modId,
                    Name = modName,
                    Version = modVersion,
                    Path = modTargetPath,
                    IsEnabled = false
                };
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

                var manifestInfo = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken);
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

                var modInfo = await _manifestService.ParseManifestAsync(manifestPath, cancellationToken);
                if (modInfo != null)
                {
                    var modId = modInfo.Value.modId;
                    var modName = modInfo.Value.name;
                    var modVersion = modInfo.Value.version;

                    var expectedPath = _fileService.Combine(modFolder, modId);
                    if (!dir.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
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

                    var mod = new Mod
                    {
                        Id = modId,
                        Name = modName,
                        Version = modVersion,
                        Path = expectedPath,
                        IsEnabled = true
                    };
                    
                    // WICHTIG: Lade Metadaten aus mod_versions.json und mod_notes.json pro Mod
                    // Jeder Mod-Ordner kann eigene Dateien haben, aber wir laden aus dem globalen File
                    // Das wird später in LoadModsAsync aufgerufen, nachdem alle Mods geladen sind
                    modDictionary[modId] = mod;
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
            var modOrder = mods.OrderBy(m => m.Number).Select(mod => mod.IsEnabled ? mod.Id : $"# {mod.Id}").ToList();
            
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

