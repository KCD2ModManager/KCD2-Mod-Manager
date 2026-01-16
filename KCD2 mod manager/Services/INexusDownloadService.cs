using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public interface INexusDownloadService
    {
        Task<NexusModFilesResponse?> GetModFilesAsync(string gameDomain, int modNumber, string apiKey, CancellationToken cancellationToken = default);
        Task<string?> GetDownloadLinkAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default);
        Task<byte[]> DownloadFileAsync(string downloadUrl, CancellationToken cancellationToken = default);
        Task<string?> PerformPremiumUpdateAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default);
    }
}
