using System.Collections.ObjectModel;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Resources;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.ViewModels
{
    public class ConflictCheckerViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private ObservableCollection<ModConflictGroup> _conflictGroups = new();
        private string _title = Strings.ResourceManager.GetString("ConflictCheckerTitle") ?? "Mod Conflicts";
        private string _noConflictsText = Strings.ResourceManager.GetString("ConflictCheckerEmpty") ?? "No conflicts detected.";
        private string _workshopBadgeText = Strings.ResourceManager.GetString("WorkshopBadgeText") ?? "Workshop";
        private string _workshopBadgeTooltip = Strings.ResourceManager.GetString("WorkshopBadgeTooltip") ?? "Installed from Steam Workshop";
        private string _workshopConflictNote = Strings.ResourceManager.GetString("WorkshopConflictNote") ?? "Workshop files may be managed by Steam.";

        public ConflictCheckerViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedStrings();
            UpdateLocalizedStrings();
        }

        public ObservableCollection<ModConflictGroup> ConflictGroups
        {
            get => _conflictGroups;
            set
            {
                if (SetProperty(ref _conflictGroups, value))
                {
                    OnPropertyChanged(nameof(HasConflicts));
                }
            }
        }

        public bool HasConflicts => ConflictGroups.Count > 0;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string NoConflictsText
        {
            get => _noConflictsText;
            set => SetProperty(ref _noConflictsText, value);
        }

        public string WorkshopBadgeText
        {
            get => _workshopBadgeText;
            set => SetProperty(ref _workshopBadgeText, value);
        }

        public string WorkshopBadgeTooltip
        {
            get => _workshopBadgeTooltip;
            set => SetProperty(ref _workshopBadgeTooltip, value);
        }

        public string WorkshopConflictNote
        {
            get => _workshopConflictNote;
            set => SetProperty(ref _workshopConflictNote, value);
        }

        public void SetConflicts(IEnumerable<ModConflictGroup> conflicts)
        {
            ConflictGroups = new ObservableCollection<ModConflictGroup>(conflicts);
        }

        private void UpdateLocalizedStrings()
        {
            Title = Strings.ResourceManager.GetString("ConflictCheckerTitle") ?? "Mod Conflicts";
            NoConflictsText = Strings.ResourceManager.GetString("ConflictCheckerEmpty") ?? "No conflicts detected.";
            WorkshopBadgeText = Strings.ResourceManager.GetString("WorkshopBadgeText") ?? "Workshop";
            WorkshopBadgeTooltip = Strings.ResourceManager.GetString("WorkshopBadgeTooltip") ?? "Installed from Steam Workshop";
            WorkshopConflictNote = Strings.ResourceManager.GetString("WorkshopConflictNote") ?? "Workshop files may be managed by Steam.";
        }
    }
}
