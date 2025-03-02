using System.Windows;
using System.Windows.Media;

namespace KCD2_mod_manager
{
    public partial class DeleteConfirmationWindow : Window
    {
        public bool DontAskAgain { get; private set; } = false;
        public bool UserConfirmed { get; private set; } = false;

        public DeleteConfirmationWindow()
        {
            InitializeComponent();
            UpdateTheme();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            DontAskAgain = DontAskAgainCheckBox.IsChecked == true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            Close();
        }

         private void UpdateTheme()
            {
                bool isDarkMode = Settings.Default.IsDarkMode;
                if (isDarkMode)
                {
                    // Dark Mode Colors
                    this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    this.Resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                    this.Resources["ModListItemEvenBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    this.Resources["ModListItemOddBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    this.Resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.White);
                    this.Resources["SelectedItemBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 120));

                    this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                }
                else
                {
                    // Light Mode Colors
                    this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                    this.Resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
                    this.Resources["ModListItemEvenBrush"] = new SolidColorBrush(Colors.White);
                    this.Resources["ModListItemOddBrush"] = new SolidColorBrush(Colors.LightGray);
                    this.Resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.Black);
                    this.Resources["SelectedItemBrush"] = new SolidColorBrush(Colors.LightBlue);

                    this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                }
                ForceUIRefresh();
            }

            // Forces the UI to redraw the entire window.
            private void ForceUIRefresh()
            {
                var current = this.Content;
                this.Content = null;
                this.Content = current;
            }
        }
}
