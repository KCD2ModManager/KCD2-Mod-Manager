using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public class ConflictCheckerService : IConflictCheckerService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;

        public ConflictCheckerService(IFileService fileService, ILog logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ModConflictGroup>> AnalyzeConflictsAsync(IEnumerable<Mod> mods, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var conflictMap = new Dictionary<string, List<ModConflictEntry>>(StringComparer.OrdinalIgnoreCase);

                foreach (var mod in mods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IEnumerable<string> pakFiles;
                    try
                    {
                        pakFiles = _fileService.EnumerateFiles(mod.Path, "*.pak", SearchOption.AllDirectories);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Konflikt-Scan: Konnte .pak-Dateien nicht lesen: {mod.Path} ({ex.Message})");
                        continue;
                    }

                    foreach (var pakPath in pakFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            using var archive = ArchiveFactory.Open(pakPath);
                            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                string key = (entry.Key ?? string.Empty).Replace('\\', '/');
                                if (string.IsNullOrWhiteSpace(key))
                                {
                                    continue;
                                }

                                if (!conflictMap.TryGetValue(key, out var list))
                                {
                                    list = new List<ModConflictEntry>();
                                    conflictMap[key] = list;
                                }

                                if (list.Any(e => e.ModId.Equals(mod.Id, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }

                                list.Add(new ModConflictEntry
                                {
                                    ModId = mod.Id,
                                    ModName = mod.Name,
                                    IsWorkshopMod = mod.IsWorkshopMod
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Konflikt-Scan: Fehler beim Lesen von {pakPath}: {ex.Message}");
                        }
                    }
                }

                return conflictMap
                    .Where(kvp => kvp.Value.Count > 1)
                    .Select(kvp => new ModConflictGroup
                    {
                        FilePath = kvp.Key,
                        Mods = kvp.Value
                    })
                    .OrderBy(group => group.FilePath)
                    .ToList()
                    .AsReadOnly();
            }, cancellationToken);
        }
    }
}
