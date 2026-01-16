using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KCD2_mod_manager.Models;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.ViewModels;

namespace KCD2_mod_manager.Views
{
    public partial class CategoryManagerWindow : Window
    {
        private readonly IThemeService? _themeService;
        private Point _dragStartPoint;

        public CategoryManagerWindow(CategoryManagerViewModel viewModel, IThemeService? themeService = null)
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

        private void CategoryList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void CategoryList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || sender is not ListView listView)
            {
                return;
            }

            Point currentPoint = e.GetPosition(null);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (listView.SelectedItem is ModCategory item)
            {
                DragDrop.DoDragDrop(listView, item, DragDropEffects.Move);
            }
        }

        private void CategoryList_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListView listView || DataContext is not CategoryManagerViewModel viewModel)
            {
                return;
            }

            if (!e.Data.GetDataPresent(typeof(ModCategory)))
            {
                return;
            }

            var dropped = (ModCategory)e.Data.GetData(typeof(ModCategory));
            var target = GetItemAtPoint(listView, e.GetPosition(listView));
            if (target == null || dropped == null)
            {
                return;
            }

            int oldIndex = listView.Items.IndexOf(dropped);
            int newIndex = listView.Items.IndexOf(target);
            viewModel.MoveCategory(oldIndex, newIndex);
        }

        private ModCategory? GetItemAtPoint(ListView listView, Point point)
        {
            var element = listView.InputHitTest(point) as DependencyObject;
            while (element != null && element is not ListViewItem)
            {
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }

            return element is ListViewItem item ? item.DataContext as ModCategory : null;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CategoryManagerViewModel viewModel)
            {
                await viewModel.SaveAsync();
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
