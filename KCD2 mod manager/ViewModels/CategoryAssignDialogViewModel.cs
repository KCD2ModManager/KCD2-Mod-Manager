using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Resources;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.ViewModels
{
    public class CategoryAssignDialogViewModel : ViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILocalizationService _localizationService;

        private ObservableCollection<ModCategory> _categories = new();
        private string? _selectedCategoryId;
        private string _newCategoryName = string.Empty;

        private string _title = Strings.ResourceManager.GetString("AssignCategoryTitle") ?? "Assign Category";
        private string _categoryLabel = Strings.ResourceManager.GetString("AssignCategoryLabel") ?? "Category:";
        private string _createLabel = Strings.ResourceManager.GetString("AssignCategoryCreateLabel") ?? "Create new category:";
        private string _createButtonText = Strings.ResourceManager.GetString("AssignCategoryCreateButton") ?? "Create";
        private string _saveButtonText = Strings.ResourceManager.GetString("AssignCategorySaveButton") ?? "Save";
        private string _cancelButtonText = Strings.ResourceManager.GetString("AssignCategoryCancelButton") ?? "Cancel";

        public CategoryAssignDialogViewModel(ICategoryService categoryService, ILocalizationService localizationService)
        {
            _categoryService = categoryService;
            _localizationService = localizationService;
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();

            CreateCategoryCommand = new RelayCommand(async _ => await CreateCategoryAsync(), _ => CanCreateCategory);
        }

        public ObservableCollection<ModCategory> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public string? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set => SetProperty(ref _selectedCategoryId, value);
        }

        public string NewCategoryName
        {
            get => _newCategoryName;
            set
            {
                if (SetProperty(ref _newCategoryName, value))
                {
                    OnPropertyChanged(nameof(CanCreateCategory));
                }
            }
        }

        public bool CanCreateCategory => !string.IsNullOrWhiteSpace(NewCategoryName);

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string CategoryLabel
        {
            get => _categoryLabel;
            set => SetProperty(ref _categoryLabel, value);
        }

        public string CreateLabel
        {
            get => _createLabel;
            set => SetProperty(ref _createLabel, value);
        }

        public string CreateButtonText
        {
            get => _createButtonText;
            set => SetProperty(ref _createButtonText, value);
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            set => SetProperty(ref _saveButtonText, value);
        }

        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        public ICommand CreateCategoryCommand { get; }

        public void Initialize(Mod mod, List<ModCategory> categories, string? selectedCategoryId)
        {
            Categories = new ObservableCollection<ModCategory>(categories.OrderBy(c => c.Order));
            SelectedCategoryId = selectedCategoryId;
        }

        private async Task CreateCategoryAsync()
        {
            string trimmed = NewCategoryName.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return;
            }

            if (Categories.Any(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var category = await _categoryService.CreateCategoryAsync(trimmed);
            Categories.Add(category);
            SelectedCategoryId = category.Id;
            NewCategoryName = string.Empty;
        }

        private void UpdateLocalizedStrings()
        {
            Title = Strings.ResourceManager.GetString("AssignCategoryTitle") ?? "Assign Category";
            CategoryLabel = Strings.ResourceManager.GetString("AssignCategoryLabel") ?? "Category:";
            CreateLabel = Strings.ResourceManager.GetString("AssignCategoryCreateLabel") ?? "Create new category:";
            CreateButtonText = Strings.ResourceManager.GetString("AssignCategoryCreateButton") ?? "Create";
            SaveButtonText = Strings.ResourceManager.GetString("AssignCategorySaveButton") ?? "Save";
            CancelButtonText = Strings.ResourceManager.GetString("AssignCategoryCancelButton") ?? "Cancel";
        }
    }
}
