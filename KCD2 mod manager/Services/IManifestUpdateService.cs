using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für das Aktualisieren von Mod-Manifesten
    /// </summary>
    public interface IManifestUpdateService
    {
        /// <summary>
        /// Aktualisiert die gameVersion in einem Mod-Manifest
        /// </summary>
        Task<bool> UpdateManifestGameVersionAsync(string manifestPath, string gameVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktualisiert die gameVersion in allen Mod-Manifesten
        /// </summary>
        Task<int> UpdateAllManifestsGameVersionAsync(string modFolder, string gameVersion, CancellationToken cancellationToken = default);
    }
}

