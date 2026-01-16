using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Resources;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.ViewModels
{
    public class CategoryDeleteDialogViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private ObservableCollection<ModCategory> _categories = new();
        private string? _selectedCategoryId;
        private bool _unassignSelected = true;

        private string _title = Strings.ResourceManager.GetString("CategoryDeleteTitle") ?? "Delete Category";
        private string _message = Strings.ResourceManager.GetString("CategoryDeleteMessage") ?? "How do you want to handle assigned mods?";
        private string _unassignText = Strings.ResourceManager.GetString("CategoryDeleteUnassign") ?? "Unassign mods";
        private string _moveToText = Strings.ResourceManager.GetString("CategoryDeleteMoveTo") ?? "Move mods to:";
        private string _confirmText = Strings.ResourceManager.GetString("CategoryDeleteConfirm") ?? "Delete";
        private string _cancelText = Strings.ResourceManager.GetString("CategoryDeleteCancel") ?? "Cancel";

        public CategoryDeleteDialogViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        public ObservableCollection<ModCategory> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public string? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set
            {
                if (SetProperty(ref _selectedCategoryId, value))
                {
                    OnPropertyChanged(nameof(CanConfirm));
                }
            }
        }

        public bool UnassignSelected
        {
            get => _unassignSelected;
            set
            {
                if (SetProperty(ref _unassignSelected, value))
                {
                    OnPropertyChanged(nameof(MoveSelected));
                    OnPropertyChanged(nameof(CanConfirm));
                }
            }
        }

        public bool MoveSelected
        {
            get => !_unassignSelected;
            set
            {
                if (SetProperty(ref _unassignSelected, !value))
                {
                    OnPropertyChanged(nameof(UnassignSelected));
                    OnPropertyChanged(nameof(CanConfirm));
                }
            }
        }

        public bool CanConfirm => UnassignSelected || !string.IsNullOrWhiteSpace(SelectedCategoryId);

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string UnassignText
        {
            get => _unassignText;
            set => SetProperty(ref _unassignText, value);
        }

        public string MoveToText
        {
            get => _moveToText;
            set => SetProperty(ref _moveToText, value);
        }

        public string ConfirmText
        {
            get => _confirmText;
            set => SetProperty(ref _confirmText, value);
        }

        public string CancelText
        {
            get => _cancelText;
            set => SetProperty(ref _cancelText, value);
        }

        public void Initialize(IEnumerable<ModCategory> categories)
        {
            Categories = new ObservableCollection<ModCategory>(categories.OrderBy(c => c.Order));
            SelectedCategoryId = Categories.FirstOrDefault()?.Id;
        }

        private void UpdateLocalizedStrings()
        {
            Title = Strings.ResourceManager.GetString("CategoryDeleteTitle") ?? "Delete Category";
            Message = Strings.ResourceManager.GetString("CategoryDeleteMessage") ?? "How do you want to handle assigned mods?";
            UnassignText = Strings.ResourceManager.GetString("CategoryDeleteUnassign") ?? "Unassign mods";
            MoveToText = Strings.ResourceManager.GetString("CategoryDeleteMoveTo") ?? "Move mods to:";
            ConfirmText = Strings.ResourceManager.GetString("CategoryDeleteConfirm") ?? "Delete";
            CancelText = Strings.ResourceManager.GetString("CategoryDeleteCancel") ?? "Cancel";
        }
    }
}
