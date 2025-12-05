using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Service für das Aktualisieren von gameVersion-Feldern in Mod-Manifesten
    /// </summary>
    public class ManifestUpdateService : IManifestUpdateService
    {
        private readonly IFileService _fileService;
        private readonly IModManifestService _manifestService;
        private readonly ILog _logger;

        public ManifestUpdateService(IFileService fileService, IModManifestService manifestService, ILog logger)
        {
            _fileService = fileService;
            _manifestService = manifestService;
            _logger = logger;
        }

        public async Task<bool> UpdateManifestGameVersionAsync(string manifestPath, string gameVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_fileService.FileExists(manifestPath))
                {
                    _logger.Warning($"Manifest-Datei nicht gefunden: {manifestPath}");
                    return false;
                }

                // Manifest lesen
                string xmlContent = await _manifestService.CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                {
                    _logger.Warning($"Manifest enthält kein 'info'-Element: {manifestPath}");
                    return false;
                }

                // gameVersion-Element finden oder erstellen
                var gameVersionElement = infoElement.Element("gameVersion");
                if (gameVersionElement == null)
                {
                    gameVersionElement = new XElement("gameVersion", gameVersion);
                    infoElement.Add(gameVersionElement);
                }
                else
                {
                    gameVersionElement.Value = gameVersion;
                }

                // Atomisches Schreiben
                string tempPath = manifestPath + ".tmp";
                await _fileService.WriteAllTextAsync(tempPath, doc.ToString(), cancellationToken);

                if (_fileService.FileExists(manifestPath))
                {
                    _fileService.DeleteFile(manifestPath);
                }

                _fileService.MoveFile(tempPath, manifestPath);

                _logger.Info($"gameVersion in Manifest aktualisiert: {manifestPath} -> {gameVersion}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Aktualisieren des Manifests: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<int> UpdateAllManifestsGameVersionAsync(string modFolder, string gameVersion, CancellationToken cancellationToken = default)
        {
            int updatedCount = 0;

            try
            {
                if (!_fileService.DirectoryExists(modFolder))
                {
                    return 0;
                }

                // Alle mod.manifest-Dateien finden
                var manifestFiles = _fileService.EnumerateFiles(modFolder, "mod.manifest", System.IO.SearchOption.AllDirectories);

                foreach (var manifestPath in manifestFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (await UpdateManifestGameVersionAsync(manifestPath, gameVersion, cancellationToken))
                    {
                        updatedCount++;
                    }
                }

                _logger.Info($"{updatedCount} Manifeste aktualisiert mit gameVersion: {gameVersion}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Aktualisieren aller Manifeste: {ex.Message}", ex);
            }

            return updatedCount;
        }
    }
}

