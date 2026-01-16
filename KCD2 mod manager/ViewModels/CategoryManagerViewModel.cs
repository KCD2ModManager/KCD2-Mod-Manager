using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Resources;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.ViewModels
{
    public class CategoryManagerViewModel : ViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IModCategoryAssignmentService _assignmentService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IServiceProvider _serviceProvider;

        private ObservableCollection<ModCategory> _categories = new();
        private ModCategory? _selectedCategory;
        private List<Mod> _mods = new();
        private bool _hasCategories;

        private string _title = Strings.ResourceManager.GetString("CategoryManagerTitle") ?? "Manage Categories";
        private string _createText = Strings.ResourceManager.GetString("CategoryManagerCreate") ?? "Create";
        private string _renameText = Strings.ResourceManager.GetString("CategoryManagerRename") ?? "Rename";
        private string _deleteText = Strings.ResourceManager.GetString("CategoryManagerDelete") ?? "Delete";
        private string _saveText = Strings.ResourceManager.GetString("CategoryManagerSave") ?? "Save";
        private string _cancelText = Strings.ResourceManager.GetString("CategoryManagerCancel") ?? "Cancel";
        private string _emptyText = Strings.ResourceManager.GetString("CategoryManagerEmpty") ?? "No categories yet.";
        private string _orderHeader = Strings.ResourceManager.GetString("CategoryManagerOrderHeader") ?? "Order";
        private string _nameHeader = Strings.ResourceManager.GetString("CategoryManagerNameHeader") ?? "Name";

        public CategoryManagerViewModel(
            ICategoryService categoryService,
            IModCategoryAssignmentService assignmentService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IServiceProvider serviceProvider)
        {
            _categoryService = categoryService;
            _assignmentService = assignmentService;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _serviceProvider = serviceProvider;

            CreateCommand = new RelayCommand(async _ => await CreateCategoryAsync());
            RenameCommand = new RelayCommand(async _ => await RenameCategoryAsync(), _ => SelectedCategory != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteCategoryAsync(), _ => SelectedCategory != null);

            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        public ObservableCollection<ModCategory> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public ModCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string CreateText
        {
            get => _createText;
            set => SetProperty(ref _createText, value);
        }

        public string RenameText
        {
            get => _renameText;
            set => SetProperty(ref _renameText, value);
        }

        public string DeleteText
        {
            get => _deleteText;
            set => SetProperty(ref _deleteText, value);
        }

        public string SaveText
        {
            get => _saveText;
            set => SetProperty(ref _saveText, value);
        }

        public string CancelText
        {
            get => _cancelText;
            set => SetProperty(ref _cancelText, value);
        }

        public string EmptyText
        {
            get => _emptyText;
            set => SetProperty(ref _emptyText, value);
        }

        public bool HasCategories
        {
            get => _hasCategories;
            private set => SetProperty(ref _hasCategories, value);
        }

        public string OrderHeader
        {
            get => _orderHeader;
            set => SetProperty(ref _orderHeader, value);
        }

        public string NameHeader
        {
            get => _nameHeader;
            set => SetProperty(ref _nameHeader, value);
        }

        public ICommand CreateCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }

        public void Initialize(List<ModCategory> categories, List<Mod> mods)
        {
            _mods = mods;
            Categories = new ObservableCollection<ModCategory>(
                categories.OrderBy(c => c.Order).Select(c => new ModCategory
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order
                }));

            Categories.CollectionChanged += (s, e) => UpdateHasCategories();
            UpdateHasCategories();
        }

        public void MoveCategory(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0 || oldIndex >= Categories.Count || newIndex >= Categories.Count)
            {
                return;
            }

            var item = Categories[oldIndex];
            Categories.RemoveAt(oldIndex);
            Categories.Insert(newIndex, item);
            UpdateOrderValues();
        }

        public async Task SaveAsync()
        {
            UpdateOrderValues();
            _categoryService.SetCategories(Categories);
            await _categoryService.SaveAsync();
        }

        private Task CreateCategoryAsync()
        {
            string? name = _dialogService.ShowInputDialog(
                Strings.ResourceManager.GetString("CategoryCreatePrompt") ?? "Enter category name:",
                Strings.ResourceManager.GetString("CategoryCreateTitle") ?? "Create Category");

            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.CompletedTask;
            }

            string trimmed = name.Trim();
            if (Categories.Any(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                _dialogService.ShowMessageBox(
                    Strings.ResourceManager.GetString("CategoryNameDuplicate") ?? "Category name already exists.",
                    Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            var category = new ModCategory
            {
                Id = $"cat-{Guid.NewGuid():N}",
                Name = trimmed,
                Order = Categories.Count
            };

            Categories.Add(category);
            SelectedCategory = category;
            return Task.CompletedTask;
        }

        private Task RenameCategoryAsync()
        {
            if (SelectedCategory == null)
            {
                return Task.CompletedTask;
            }

            string? name = _dialogService.ShowInputDialog(
                Strings.ResourceManager.GetString("CategoryRenamePrompt") ?? "Enter new category name:",
                Strings.ResourceManager.GetString("CategoryRenameTitle") ?? "Rename Category",
                SelectedCategory.Name);

            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.CompletedTask;
            }

            string trimmed = name.Trim();
            if (Categories.Any(c => c.Id != SelectedCategory.Id && c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                _dialogService.ShowMessageBox(
                    Strings.ResourceManager.GetString("CategoryNameDuplicate") ?? "Category name already exists.",
                    Strings.ResourceManager.GetString("ErrorTitle") ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            SelectedCategory.Name = trimmed;
            OnPropertyChanged(nameof(Categories));
            return Task.CompletedTask;
        }

        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null)
            {
                return;
            }

            var deleteDialog = _serviceProvider.GetRequiredService<Views.CategoryDeleteDialog>();
            if (deleteDialog.DataContext is CategoryDeleteDialogViewModel deleteViewModel)
            {
                deleteViewModel.Initialize(Categories.Where(c => c.Id != SelectedCategory.Id).ToList());
            }

            deleteDialog.Owner = Application.Current.MainWindow;
            bool? result = deleteDialog.ShowDialog();
            if (result != true || deleteDialog.DataContext is not CategoryDeleteDialogViewModel deleteResult)
            {
                return;
            }

            string deletedId = SelectedCategory.Id;
            string? moveToId = deleteResult.UnassignSelected ? null : deleteResult.SelectedCategoryId;

            foreach (var mod in _mods.Where(m => m.CategoryId == deletedId))
            {
                if (string.IsNullOrWhiteSpace(moveToId))
                {
                    await _assignmentService.ClearCategoryAsync(mod);
                    mod.CategoryId = string.Empty;
                    mod.CategoryName = string.Empty;
                }
                else
                {
                    await _assignmentService.SetCategoryIdAsync(mod, moveToId);
                    mod.CategoryId = moveToId;
                    mod.CategoryName = Categories.FirstOrDefault(c => c.Id == moveToId)?.Name ?? string.Empty;
                }
            }

            Categories.Remove(SelectedCategory);
            SelectedCategory = null;
            UpdateOrderValues();
        }

        private void UpdateOrderValues()
        {
            for (int i = 0; i < Categories.Count; i++)
            {
                Categories[i].Order = i;
            }
        }

        private void UpdateHasCategories()
        {
            HasCategories = Categories.Count > 0;
        }

        private void UpdateLocalizedStrings()
        {
            Title = Strings.ResourceManager.GetString("CategoryManagerTitle") ?? "Manage Categories";
            CreateText = Strings.ResourceManager.GetString("CategoryManagerCreate") ?? "Create";
            RenameText = Strings.ResourceManager.GetString("CategoryManagerRename") ?? "Rename";
            DeleteText = Strings.ResourceManager.GetString("CategoryManagerDelete") ?? "Delete";
            SaveText = Strings.ResourceManager.GetString("CategoryManagerSave") ?? "Save";
            CancelText = Strings.ResourceManager.GetString("CategoryManagerCancel") ?? "Cancel";
            EmptyText = Strings.ResourceManager.GetString("CategoryManagerEmpty") ?? "No categories yet.";
            OrderHeader = Strings.ResourceManager.GetString("CategoryManagerOrderHeader") ?? "Order";
            NameHeader = Strings.ResourceManager.GetString("CategoryManagerNameHeader") ?? "Name";
        }
    }
}
