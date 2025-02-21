using System.Windows;

namespace KCD2_mod_manager
{
    public partial class DeleteConfirmationWindow : Window
    {
        public bool DontAskAgain { get; private set; } = false;
        public bool UserConfirmed { get; private set; } = false;

        public DeleteConfirmationWindow()
        {
            InitializeComponent();
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
    }
}
