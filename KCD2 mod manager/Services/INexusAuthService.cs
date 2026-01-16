namespace KCD2_mod_manager.Services
{
    public interface INexusAuthService
    {
        Task<bool> StartNexusSSOAsync(CancellationToken cancellationToken = default);
        Task<bool> ValidateNexusUserAsync(string apiKey, CancellationToken cancellationToken = default);
        bool IsUserLoggedIn();
    }
}
