using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public interface IConflictCheckerService
    {
        Task<IReadOnlyList<ModConflictGroup>> AnalyzeConflictsAsync(
            IEnumerable<Mod> mods,
            bool onlyEnabledMods = true,
            ISet<string>? ignoredModIds = null,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
