using KCD2_mod_manager.Models;

using Microsoft.Extensions.Http;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Nexus Mods API-Operationen
    /// </summary>
    public interface INexusService
    {
        Task<bool> StartNexusSSOAsync(CancellationToken cancellationToken = default);
        Task<bool> ValidateNexusUserAsync(string apiKey, CancellationToken cancellationToken = default);
        Task<bool> EndorseModAsync(string gameDomain, int modId, string apiKey, CancellationToken cancellationToken = default);
        Task<string?> GetLatestVersionAsync(string modPageUrl, CancellationToken cancellationToken = default);
        Task<NexusModFilesResponse?> GetModFilesAsync(string gameDomain, int modNumber, string apiKey, CancellationToken cancellationToken = default);
        Task<string?> GetDownloadLinkAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default);
        Task<byte[]> DownloadFileAsync(string downloadUrl, CancellationToken cancellationToken = default);
        Task<string?> PerformPremiumUpdateAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default);
        bool IsUserLoggedIn();
    }
}

