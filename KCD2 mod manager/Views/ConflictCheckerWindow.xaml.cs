using System.Windows;
using System.Windows.Controls;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Views
{
    public partial class ConflictCheckerWindow : Window
    {
        private readonly IThemeService? _themeService;

        public ConflictCheckerWindow(ConflictCheckerViewModel viewModel, IThemeService? themeService = null)
        {
            InitializeComponent();
            DataContext = viewModel;
            _themeService = themeService;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (_themeService == null)
            {
                return;
            }

            _themeService.ApplyTheme(Resources, _themeService.IsDarkMode);
            Background = (System.Windows.Media.Brush)Resources["WindowBackgroundBrush"];
        }

        private async void IgnoreConflictsForMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ModConflictEntry entry && DataContext is ConflictCheckerViewModel vm)
            {
                await vm.ToggleIgnoreAsync(entry.ModId);
            }
        }
    }
}
