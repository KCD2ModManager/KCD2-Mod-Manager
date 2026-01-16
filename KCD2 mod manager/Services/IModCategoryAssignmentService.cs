using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public interface IModCategoryAssignmentService
    {
        Task<Dictionary<string, string?>> LoadCategoryAssignmentsAsync(IEnumerable<Mod> mods, CancellationToken cancellationToken = default);
        Task<string?> GetCategoryIdAsync(Mod mod, CancellationToken cancellationToken = default);
        Task<bool> GetWorkshopFlagAsync(Mod mod, CancellationToken cancellationToken = default);
        Task SetCategoryIdAsync(Mod mod, string? categoryId, CancellationToken cancellationToken = default);
        Task ClearCategoryAsync(Mod mod, CancellationToken cancellationToken = default);
        Task MarkWorkshopAsync(Mod mod, CancellationToken cancellationToken = default);
    }
}
