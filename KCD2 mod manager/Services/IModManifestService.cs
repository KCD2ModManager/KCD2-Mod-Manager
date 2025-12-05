using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Mod-Manifest-Operationen
    /// </summary>
    public interface IModManifestService
    {
        Task<(string modId, string name, string version)?> ParseManifestAsync(string manifestPath, CancellationToken cancellationToken = default);
        Task<(string modId, string name, string version, string? gameVersion)?> ParseManifestWithGameVersionAsync(string manifestPath, CancellationToken cancellationToken = default);
        Task<string> GenerateManifestAsync(string folderPath, CancellationToken cancellationToken = default);
        Task<string> CorrectXmlVersionInFileAsync(string manifestPath, CancellationToken cancellationToken = default);
        string GenerateModId(string name);
    }
}

