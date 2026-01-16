using System.Windows;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.ViewModels;

namespace KCD2_mod_manager.Views
{
    public partial class CategoryDeleteDialog : Window
    {
        private readonly IThemeService? _themeService;

        public CategoryDeleteDialog(CategoryDeleteDialogViewModel viewModel, IThemeService? themeService = null)
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

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
