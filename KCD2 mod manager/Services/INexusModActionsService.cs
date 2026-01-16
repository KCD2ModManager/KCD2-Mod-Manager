namespace KCD2_mod_manager.Services
{
    public interface INexusModActionsService
    {
        Task<bool> EndorseModAsync(string gameDomain, int modId, string apiKey, CancellationToken cancellationToken = default);
    }
}
