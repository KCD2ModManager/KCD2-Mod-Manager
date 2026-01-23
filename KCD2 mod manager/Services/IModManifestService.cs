using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Mod-Manifest-Operationen
    /// </summary>
    public interface IModManifestService
    {
        Task<(string modId, string name, string version)?> ParseManifestAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true);
        Task<(string modId, string name, string version, bool hasExplicitModId)?> ParseManifestWithModIdInfoAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true);
        Task<(string modId, string name, string version, string? gameVersion)?> ParseManifestWithGameVersionAsync(string manifestPath, CancellationToken cancellationToken = default, bool allowWriteModId = true);
        Task<string> GenerateManifestAsync(string folderPath, CancellationToken cancellationToken = default);
        Task<string> CorrectXmlVersionInFileAsync(string manifestPath, CancellationToken cancellationToken = default);
        Task<bool> UpdateManifestNameAndIdAsync(string manifestPath, string newName, string newModId, CancellationToken cancellationToken = default);
        Task<bool> EnsureManifestModIdAsync(string manifestPath, string modId, CancellationToken cancellationToken = default);
        string GenerateModId(string name);
    }
}

