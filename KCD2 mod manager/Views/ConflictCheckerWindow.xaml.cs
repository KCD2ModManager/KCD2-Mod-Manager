using System.Windows;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;

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
    }
}
