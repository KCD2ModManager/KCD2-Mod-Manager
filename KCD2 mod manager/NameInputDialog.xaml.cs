using System.Windows;
using System.Windows.Media;

namespace KCD2_mod_manager
{
    public partial class NameInputDialog : Window
    {
        public string EnteredText { get; private set; }

        private bool isDarkMode = false;
        public string Prompt
        {
            get => PromptTextBlock.Text;
            set => PromptTextBlock.Text = value;
        }

        public NameInputDialog(string suggestedName)
        {
            InitializeComponent();
            isDarkMode = Settings.Default.IsDarkMode;
            UpdateTheme();
            ModNameTextBox.Text = suggestedName;

        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            EnteredText = ModNameTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }


        private void ForceUIRefresh()
        {
            // Erzwinge ein vollständiges Redraw des Fensters
            var current = this.Content;
            this.Content = null;
            this.Content = current;
        }

        private void UpdateTheme()
        {
            if (isDarkMode)
            {
                // Dark Mode
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Resources["SearchBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                this.Resources["SearchForegroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["SearchBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                this.Resources["ClearButtonBrush"] = new SolidColorBrush(Colors.White);


                ForceUIRefresh();
            }
            else
            {
                // Light Mode
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["SearchBackgroundBrush"] = new SolidColorBrush(Colors.LightGray);
                this.Resources["SearchForegroundBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["SearchBorderBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                this.Resources["ClearButtonBrush"] = new SolidColorBrush(Colors.Black);



                ForceUIRefresh();
            }
        }
    }
}
