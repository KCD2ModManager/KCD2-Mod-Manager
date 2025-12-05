using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für die Verwaltung persistenter Mod-Metadaten außerhalb des Manager-Update-Pfads
    /// </summary>
    public class UserModDataService : IUserModDataService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly string _userDataPath;

        public UserModDataService(IFileService fileService, ILog logger)
        {
            _fileService = fileService;
            _logger = logger;

            // Pfad: %AppData%/KCDModManager/user_mod_data.json
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string managerDataPath = Path.Combine(appDataPath, "KCDModManager");
            _userDataPath = Path.Combine(managerDataPath, "user_mod_data.json");

            // Verzeichnis erstellen, falls es nicht existiert
            if (!_fileService.DirectoryExists(managerDataPath))
            {
                _fileService.CreateDirectory(managerDataPath);
            }
        }

        public async Task<Dictionary<string, UserModData>> LoadUserModDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_fileService.FileExists(_userDataPath))
                {
                    return new Dictionary<string, UserModData>();
                }

                string json = await _fileService.ReadAllTextAsync(_userDataPath, cancellationToken);
                var data = JsonSerializer.Deserialize<Dictionary<string, UserModData>>(json);

                return data ?? new Dictionary<string, UserModData>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Laden der Benutzer-Mod-Daten: {ex.Message}", ex);
                return new Dictionary<string, UserModData>();
            }
        }

        public async Task SaveUserModDataAsync(Dictionary<string, UserModData> userModData, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(userModData, options);

                // Atomisches Schreiben: Temp-Datei erstellen und dann umbenennen
                string tempPath = _userDataPath + ".tmp";
                
                // Temp-Datei löschen, falls vorhanden
                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }

                await _fileService.WriteAllTextAsync(tempPath, json, cancellationToken);

                // Alte Datei löschen, falls vorhanden
                if (_fileService.FileExists(_userDataPath))
                {
                    _fileService.DeleteFile(_userDataPath);
                }

                // Temp-Datei umbenennen
                _fileService.MoveFile(tempPath, _userDataPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern der Benutzer-Mod-Daten: {ex.Message}", ex);
                throw;
            }
        }

        public async Task SaveModDataAsync(string modId, string? customVersion = null, string? detectedVersion = null, string? customNote = null, CancellationToken cancellationToken = default)
        {
            var allData = await LoadUserModDataAsync(cancellationToken);

            if (!allData.TryGetValue(modId, out UserModData? modData))
            {
                modData = new UserModData { ModId = modId };
                allData[modId] = modData;
            }

            if (customVersion != null)
            {
                modData.CustomVersion = customVersion;
            }

            if (detectedVersion != null)
            {
                modData.LastDetectedVersion = detectedVersion;
            }

            if (customNote != null)
            {
                modData.CustomNote = customNote;
            }

            modData.LastUpdated = DateTime.UtcNow.ToString("O");

            await SaveUserModDataAsync(allData, cancellationToken);
        }

        public async Task<UserModData?> GetModDataAsync(string modId, CancellationToken cancellationToken = default)
        {
            var allData = await LoadUserModDataAsync(cancellationToken);
            return allData.TryGetValue(modId, out UserModData? modData) ? modData : null;
        }

        public async Task<string> MergeVersionAsync(string modId, string detectedVersion, CancellationToken cancellationToken = default)
        {
            var userData = await GetModDataAsync(modId, cancellationToken);

            // Wenn der Benutzer eine benutzerdefinierte Version gesetzt hat, diese verwenden
            if (userData != null && !string.IsNullOrEmpty(userData.CustomVersion))
            {
                // Aktualisiere die erkannte Version, aber behalte die benutzerdefinierte
                await SaveModDataAsync(modId, detectedVersion: detectedVersion, cancellationToken: cancellationToken);
                return userData.CustomVersion;
            }

            // Ansonsten die erkannte Version verwenden und speichern
            await SaveModDataAsync(modId, detectedVersion: detectedVersion, cancellationToken: cancellationToken);
            return detectedVersion;
        }
    }
}

