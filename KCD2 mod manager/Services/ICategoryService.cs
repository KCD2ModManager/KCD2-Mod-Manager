using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public interface ICategoryService
    {
        IReadOnlyList<ModCategory> Categories { get; }
        event EventHandler? CategoriesChanged;

        Task LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<ModCategory> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);
        Task<bool> RenameCategoryAsync(string categoryId, string newName, CancellationToken cancellationToken = default);
        Task<bool> DeleteCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
        void SetCategories(IEnumerable<ModCategory> categories);
    }
}
