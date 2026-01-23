using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von IModManifestService für Mod-Manifest-Operationen
    /// </summary>
    public class ModManifestService : IModManifestService
    {
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly IRenameService _renameService;
        private readonly IAppSettings _settings;

        public ModManifestService(IFileService fileService, ILog logger, IRenameService renameService, IAppSettings settings)
        {
            _fileService = fileService;
            _logger = logger;
            _renameService = renameService;
            _settings = settings;
        }

        public async Task<(string modId, string name, string version)?> ParseManifestAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true)
        {
            try
            {
                string xmlContent = await CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                    return null;

                var name = infoElement.Element("name")?.Value?.Trim();
                var version = infoElement.Element("version")?.Value?.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                    return null;

                var modidElement = infoElement.Element("modid");
                string id;
                bool shouldWrite = false;
                if (modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value))
                {
                    id = modidElement.Value.Trim();
                }
                else
                {
                    id = GenerateModId(name);
                    if (allowWriteModId)
                    {
                        if (modidElement == null)
                        {
                            infoElement.Add(new XElement("modid", id));
                        }
                        else
                        {
                            modidElement.Value = id;
                        }
                        shouldWrite = true;
                    }
                }

                if (shouldWrite)
                {
                    await _fileService.WriteAllTextAsync(manifestPath, doc.ToString(), cancellationToken);
                }

                return (id, name, version);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Parsen des Manifests: {manifestPath}", ex);
                return null;
            }
        }

        public async Task<(string modId, string name, string version, bool hasExplicitModId)?> ParseManifestWithModIdInfoAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true)
        {
            try
            {
                string xmlContent = await CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                    return null;

                var name = infoElement.Element("name")?.Value?.Trim();
                var version = infoElement.Element("version")?.Value?.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                    return null;

                var modidElement = infoElement.Element("modid");
                string id;
                bool shouldWrite = false;
                bool hasExplicitModId = modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value);
                if (hasExplicitModId)
                {
                    id = modidElement!.Value.Trim();
                }
                else
                {
                    id = GenerateModId(name);
                    if (allowWriteModId)
                    {
                        if (modidElement == null)
                        {
                            infoElement.Add(new XElement("modid", id));
                        }
                        else
                        {
                            modidElement.Value = id;
                        }
                        shouldWrite = true;
                    }
                }

                if (shouldWrite)
                {
                    await _fileService.WriteAllTextAsync(manifestPath, doc.ToString(), cancellationToken);
                }

                return (id, name, version, hasExplicitModId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Parsen des Manifests: {manifestPath}", ex);
                return null;
            }
        }

        public async Task<(string modId, string name, string version, string? gameVersion)?> ParseManifestWithGameVersionAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true)
        {
            try
            {
                string xmlContent = await CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                    return null;

                var name = infoElement.Element("name")?.Value?.Trim();
                var version = infoElement.Element("version")?.Value?.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                    return null;

                var modidElement = infoElement.Element("modid");
                string id;
                bool shouldWrite = false;
                if (modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value))
                {
                    id = modidElement.Value.Trim();
                }
                else
                {
                    id = GenerateModId(name);
                    if (allowWriteModId)
                    {
                        if (modidElement == null)
                        {
                            infoElement.Add(new XElement("modid", id));
                        }
                        else
                        {
                            modidElement.Value = id;
                        }
                        shouldWrite = true;
                    }
                }

                // Feature 9: gameVersion aus Manifest lesen
                var gameVersionElement = infoElement.Element("gameVersion");
                string? gameVersion = gameVersionElement?.Value?.Trim();

                if (shouldWrite)
                {
                    await _fileService.WriteAllTextAsync(manifestPath, doc.ToString(), cancellationToken);
                }

                return (id, name, version, gameVersion);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Parsen des Manifests: {manifestPath}", ex);
                return null;
            }
        }

        public async Task<string> GenerateManifestAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            var folderInfo = new DirectoryInfo(folderPath);
            string originalName = folderInfo.Name;

            string modNameSuggestion = originalName.Trim();
            string versionSuggestion = "N/A";
            Regex regex = new Regex(@"^(.*?)-\d+-(\d+)-(\d+)-.*$");
            Match match = regex.Match(originalName);
            if (match.Success)
            {
                versionSuggestion = $"{match.Groups[2].Value}.{match.Groups[3].Value}";
            }

            string modId = GenerateModId(modNameSuggestion);

            string manifestContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<kcd_mod>
  <info>
    <name>{modNameSuggestion}</name>
    <description>auto generated manifest from kcd2 modmanager</description>
    <author>N/A</author>
    <version>{versionSuggestion}</version>
    <modid>{modId}</modid>
  </info>
</kcd_mod>";

            string manifestPath = _fileService.Combine(folderPath, "mod.manifest");
            await _fileService.WriteAllTextAsync(manifestPath, manifestContent, cancellationToken);

            return manifestPath;
        }

        public async Task<string> CorrectXmlVersionInFileAsync(string manifestPath, CancellationToken cancellationToken = default)
        {
            string xmlContent = await _fileService.ReadAllTextAsync(manifestPath, cancellationToken);

            if (xmlContent.Contains("<?xml version=\"2.0\""))
            {
                xmlContent = xmlContent.Replace("<?xml version=\"2.0\"", "<?xml version=\"1.0\"");
                await _fileService.WriteAllTextAsync(manifestPath, xmlContent, cancellationToken);
            }
            return xmlContent;
        }

        public async Task<bool> UpdateManifestNameAndIdAsync(string manifestPath, string newName, string newModId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_fileService.FileExists(manifestPath))
                {
                    _logger.Warning($"Manifest-Datei nicht gefunden: {manifestPath}");
                    return false;
                }

                string xmlContent = await CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                {
                    _logger.Warning($"Manifest enthält kein 'info'-Element: {manifestPath}");
                    return false;
                }

                var nameElement = infoElement.Element("name");
                if (nameElement == null)
                {
                    infoElement.Add(new XElement("name", newName));
                }
                else
                {
                    nameElement.Value = newName;
                }

                if (_settings.EnableFileRenaming)
                {
                    var modidElement = infoElement.Element("modid");
                    if (modidElement == null)
                    {
                        infoElement.Add(new XElement("modid", newModId));
                    }
                    else
                    {
                        modidElement.Value = newModId;
                    }
                }

                string tempPath = manifestPath + ".tmp";
                await _fileService.WriteAllTextAsync(tempPath, doc.ToString(), cancellationToken);

                if (_fileService.FileExists(manifestPath))
                {
                    _fileService.DeleteFile(manifestPath);
                }

                _fileService.MoveFile(tempPath, manifestPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Aktualisieren des Manifests: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> EnsureManifestModIdAsync(string manifestPath, string modId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_fileService.FileExists(manifestPath))
                {
                    _logger.Warning($"Manifest-Datei nicht gefunden: {manifestPath}");
                    return false;
                }

                string xmlContent = await CorrectXmlVersionInFileAsync(manifestPath, cancellationToken);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                {
                    _logger.Warning($"Manifest enthält kein 'info'-Element: {manifestPath}");
                    return false;
                }

                var modidElement = infoElement.Element("modid");
                if (modidElement != null && string.Equals(modidElement.Value?.Trim(), modId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (modidElement == null)
                {
                    infoElement.Add(new XElement("modid", modId));
                }
                else
                {
                    modidElement.Value = modId;
                }

                string tempPath = manifestPath + ".tmp";
                await _fileService.WriteAllTextAsync(tempPath, doc.ToString(), cancellationToken);

                if (_fileService.FileExists(manifestPath))
                {
                    _fileService.DeleteFile(manifestPath);
                }

                _fileService.MoveFile(tempPath, manifestPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Aktualisieren der ModId im Manifest: {ex.Message}", ex);
                return false;
            }
        }

        public string GenerateModId(string name)
        {
            return _renameService.GenerateModId(name);
        }
    }
}

