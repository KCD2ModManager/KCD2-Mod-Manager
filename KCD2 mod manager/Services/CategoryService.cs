using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    public class CategoryService : ICategoryService
    {
        private const int CurrentSchemaVersion = 1;
        private readonly IFileService _fileService;
        private readonly ILog _logger;
        private readonly string _categoriesPath;
        private List<ModCategory> _categories = new();

        public CategoryService(IFileService fileService, ILog logger)
        {
            _fileService = fileService;
            _logger = logger;

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string managerDataPath = Path.Combine(appDataPath, "KCDModManager");
            if (!_fileService.DirectoryExists(managerDataPath))
            {
                _fileService.CreateDirectory(managerDataPath);
            }

            _categoriesPath = Path.Combine(managerDataPath, "categories.json");
        }

        public IReadOnlyList<ModCategory> Categories => _categories.AsReadOnly();
        public event EventHandler? CategoriesChanged;

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!_fileService.FileExists(_categoriesPath))
            {
                _categories = new List<ModCategory>();
                await SaveAsync(cancellationToken);
                CategoriesChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            try
            {
                string json = await _fileService.ReadAllTextAsync(_categoriesPath, cancellationToken);
                var store = JsonSerializer.Deserialize<CategoryStore>(json);
                if (store == null || store.SchemaVersion <= 0)
                {
                    throw new JsonException("Invalid category schema.");
                }

                _categories = store.Categories
                    .OrderBy(c => c.Order)
                    .Select(c => new ModCategory
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Order = c.Order
                    })
                    .ToList();

                CategoriesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                try
                {
                    string backupPath = _categoriesPath + ".bak";
                    if (_fileService.FileExists(backupPath))
                    {
                        _fileService.DeleteFile(backupPath);
                    }
                    _fileService.MoveFile(_categoriesPath, backupPath);
                }
                catch (Exception backupEx)
                {
                    _logger.Error($"Fehler beim Sichern der beschädigten categories.json: {backupEx.Message}", backupEx);
                }

                _logger.Error($"Fehler beim Laden von categories.json: {ex.Message}", ex);
                _categories = new List<ModCategory>();
                await SaveAsync(cancellationToken);
                CategoriesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var store = new CategoryStore
                {
                    SchemaVersion = CurrentSchemaVersion,
                    Categories = _categories
                        .Select(c => new ModCategory
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Order = c.Order
                        })
                        .OrderBy(c => c.Order)
                        .ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(store, options);
                string tempPath = _categoriesPath + ".tmp";

                if (_fileService.FileExists(tempPath))
                {
                    _fileService.DeleteFile(tempPath);
                }

                await _fileService.WriteAllTextAsync(tempPath, json, cancellationToken);

                if (_fileService.FileExists(_categoriesPath))
                {
                    _fileService.DeleteFile(_categoriesPath);
                }

                _fileService.MoveFile(tempPath, _categoriesPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Speichern von categories.json: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ModCategory> CreateCategoryAsync(string name, CancellationToken cancellationToken = default)
        {
            string trimmed = name.Trim();
            int nextOrder = _categories.Count == 0 ? 0 : _categories.Max(c => c.Order) + 1;
            var category = new ModCategory
            {
                Id = $"cat-{Guid.NewGuid():N}",
                Name = trimmed,
                Order = nextOrder
            };

            _categories.Add(category);
            await SaveAsync(cancellationToken);
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
            return category;
        }

        public async Task<bool> RenameCategoryAsync(string categoryId, string newName, CancellationToken cancellationToken = default)
        {
            var category = _categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return false;
            }

            category.Name = newName.Trim();
            await SaveAsync(cancellationToken);
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
        {
            var category = _categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
            {
                return false;
            }

            _categories.Remove(category);
            ReorderCategories();
            await SaveAsync(cancellationToken);
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void SetCategories(IEnumerable<ModCategory> categories)
        {
            _categories = categories
                .Select((c, index) => new ModCategory
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order
                })
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .ToList();

            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReorderCategories()
        {
            int order = 0;
            foreach (var category in _categories.OrderBy(c => c.Order).ThenBy(c => c.Name))
            {
                category.Order = order++;
            }
        }
    }
}
