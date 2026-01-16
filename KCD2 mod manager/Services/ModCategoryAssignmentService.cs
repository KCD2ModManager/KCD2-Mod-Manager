using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public class ModCategoryAssignmentService : IModCategoryAssignmentService
    {
        private class ModMetaSidecar
        {
            public string? CategoryId { get; set; }
            public bool? IsWorkshopMod { get; set; }
        }
        private readonly IUserModDataService _userModDataService;
        private readonly IFileService _fileService;
        private readonly ILog _logger;

        public ModCategoryAssignmentService(IUserModDataService userModDataService, IFileService fileService, ILog logger)
        {
            _userModDataService = userModDataService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Dictionary<string, string?>> LoadCategoryAssignmentsAsync(IEnumerable<Mod> mods, CancellationToken cancellationToken = default)
        {
            var assignments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var userData = await _userModDataService.LoadUserModDataAsync(cancellationToken);

            foreach (var mod in mods)
            {
                if (userData.TryGetValue(mod.Id, out var data) && !string.IsNullOrWhiteSpace(data.CategoryId))
                {
                    assignments[mod.Id] = data.CategoryId;
                    continue;
                }

                var sidecar = await LoadSidecarAsync(mod, cancellationToken);
                string? sidecarCategory = sidecar?.CategoryId;
                if (!string.IsNullOrWhiteSpace(sidecarCategory))
                {
                    assignments[mod.Id] = sidecarCategory;
                }
            }

            return assignments;
        }

        public async Task<string?> GetCategoryIdAsync(Mod mod, CancellationToken cancellationToken = default)
        {
            var data = await _userModDataService.GetModDataAsync(mod.Id, cancellationToken);
            if (data != null && !string.IsNullOrWhiteSpace(data.CategoryId))
            {
                return data.CategoryId;
            }

            return await LoadSidecarCategoryAsync(mod, cancellationToken);
        }

        public async Task<bool> GetWorkshopFlagAsync(Mod mod, CancellationToken cancellationToken = default)
        {
            var sidecar = await LoadSidecarAsync(mod, cancellationToken);
            return sidecar?.IsWorkshopMod == true;
        }

        public async Task SetCategoryIdAsync(Mod mod, string? categoryId, CancellationToken cancellationToken = default)
        {
            if (_userModDataService.UserDataFileExists())
            {
                await _userModDataService.SaveModDataAsync(mod.Id, categoryId: categoryId ?? string.Empty, cancellationToken: cancellationToken);
                await RemoveSidecarAsync(mod, cancellationToken);
                return;
            }

            await SaveSidecarAsync(mod, categoryId, cancellationToken);
        }

        public async Task ClearCategoryAsync(Mod mod, CancellationToken cancellationToken = default)
        {
            if (_userModDataService.UserDataFileExists())
            {
                await _userModDataService.SaveModDataAsync(mod.Id, categoryId: string.Empty, cancellationToken: cancellationToken);
            }

            await RemoveSidecarAsync(mod, cancellationToken);
        }

        private async Task<string?> LoadSidecarCategoryAsync(Mod mod, CancellationToken cancellationToken)
        {
            var sidecar = await LoadSidecarAsync(mod, cancellationToken);
            return sidecar?.CategoryId;
        }

        private async Task<ModMetaSidecar?> LoadSidecarAsync(Mod mod, CancellationToken cancellationToken)
        {
            try
            {
                string sidecarPath = GetSidecarPath(mod);
                if (!_fileService.FileExists(sidecarPath))
                {
                    return null;
                }

                string json = await _fileService.ReadAllTextAsync(sidecarPath, cancellationToken);
                var sidecar = JsonSerializer.Deserialize<ModMetaSidecar>(json);
                return sidecar;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Fehler beim Lesen der mod.meta.json für {mod.Name}: {ex.Message}");
                return null;
            }
        }

        private async Task SaveSidecarAsync(Mod mod, string? categoryId, CancellationToken cancellationToken)
        {
            try
            {
                string sidecarPath = GetSidecarPath(mod);
                if (string.IsNullOrWhiteSpace(categoryId))
                {
                    var existing = await LoadSidecarAsync(mod, cancellationToken);
                    if (existing == null || existing.IsWorkshopMod != true)
                    {
                        if (_fileService.FileExists(sidecarPath))
                        {
                            _fileService.DeleteFile(sidecarPath);
                        }
                        return;
                    }
                }

                var payload = await LoadSidecarAsync(mod, cancellationToken) ?? new ModMetaSidecar();
                payload.CategoryId = string.IsNullOrWhiteSpace(categoryId) ? null : categoryId;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(payload, options);
                string tempPath = sidecarPath + ".tmp";

                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }

                await _fileService.WriteAllTextAsync(tempPath, json, cancellationToken);

                if (_fileService.FileExists(sidecarPath))
                {
                    _fileService.DeleteFile(sidecarPath);
                }

                _fileService.MoveFile(tempPath, sidecarPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Schreiben der mod.meta.json für {mod.Name}: {ex.Message}", ex);
            }
        }

        private async Task RemoveSidecarAsync(Mod mod, CancellationToken cancellationToken)
        {
            try
            {
                string sidecarPath = GetSidecarPath(mod);
                if (_fileService.FileExists(sidecarPath))
                {
                    _fileService.DeleteFile(sidecarPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Fehler beim Löschen der mod.meta.json für {mod.Name}: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private string GetSidecarPath(Mod mod)
        {
            return _fileService.Combine(mod.Path, "mod.meta.json");
        }

        public async Task MarkWorkshopAsync(Mod mod, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = await LoadSidecarAsync(mod, cancellationToken) ?? new ModMetaSidecar();
                payload.IsWorkshopMod = true;

                string sidecarPath = GetSidecarPath(mod);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(payload, options);
                string tempPath = sidecarPath + ".tmp";

                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }

                await _fileService.WriteAllTextAsync(tempPath, json, cancellationToken);

                if (_fileService.FileExists(sidecarPath))
                {
                    _fileService.DeleteFile(sidecarPath);
                }

                _fileService.MoveFile(tempPath, sidecarPath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Fehler beim Markieren von Workshop-Metadaten für {mod.Name}: {ex.Message}");
            }
        }
    }
}
