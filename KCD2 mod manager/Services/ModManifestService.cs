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

        public ModManifestService(IFileService fileService, ILog logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<(string modId, string name, string version)?> ParseManifestAsync(string manifestPath, CancellationToken cancellationToken = default)
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
                if (modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value))
                {
                    id = modidElement.Value.Trim();
                }
                else
                {
                    id = GenerateModId(name);
                    if (modidElement == null)
                    {
                        infoElement.Add(new XElement("modid", id));
                    }
                    else
                    {
                        modidElement.Value = id;
                    }
                }

                await _fileService.WriteAllTextAsync(manifestPath, doc.ToString(), cancellationToken);

                return (id, name, version);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Parsen des Manifests: {manifestPath}", ex);
                return null;
            }
        }

        public async Task<(string modId, string name, string version, string? gameVersion)?> ParseManifestWithGameVersionAsync(string manifestPath, CancellationToken cancellationToken = default)
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
                if (modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value))
                {
                    id = modidElement.Value.Trim();
                }
                else
                {
                    id = GenerateModId(name);
                    if (modidElement == null)
                    {
                        infoElement.Add(new XElement("modid", id));
                    }
                    else
                    {
                        modidElement.Value = id;
                    }
                }

                // Feature 9: gameVersion aus Manifest lesen
                var gameVersionElement = infoElement.Element("gameVersion");
                string? gameVersion = gameVersionElement?.Value?.Trim();

                await _fileService.WriteAllTextAsync(manifestPath, doc.ToString(), cancellationToken);

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

            string modNameSuggestion = originalName;
            string versionSuggestion = "N/A";
            Regex regex = new Regex(@"^(.*?)-\d+-(\d+)-(\d+)-.*$");
            Match match = regex.Match(originalName);
            if (match.Success)
            {
                modNameSuggestion = match.Groups[1].Value.Trim();
                versionSuggestion = $"{match.Groups[2].Value}.{match.Groups[3].Value}";
            }
            else
            {
                modNameSuggestion = Regex.Replace(originalName, @"[-\s\d\(\)]+$", "").Trim();
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

        public string GenerateModId(string name)
        {
            string id = Regex.Replace(name.ToLowerInvariant(), @"[^a-z]", "");
            if (string.IsNullOrEmpty(id))
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(name);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    string fallback = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    fallback = Regex.Replace(fallback, @"[^a-z]", "");
                    if (fallback.Length > 8)
                        id = fallback.Substring(0, 8);
                    else if (fallback.Length > 0)
                        id = fallback;
                    else
                        id = "moddefault";
                }
            }
            return id;
        }
    }
}

