using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;


namespace KCD2_mod_manager
{
    public partial class SettingsWindow : Window
    {
        private const string DefaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\KingdomComeDeliverance2\Bin\Win64MasterMasterSteamPGO\KingdomCome.exe";
        private string GamePath;
        public event EventHandler ThemeChanged;
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            HookToggleEvents();

            UpdateTheme();
            CheckAndLoadGamePath();
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
            //ForceUIRefresh();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        // Forces the UI to redraw the entire window.
        private void ForceUIRefresh()
        {
            var current = this.Content;
            this.Content = null;
            this.Content = current;
        }
        private void CheckAndLoadGamePath()
        {
            // Lade den GamePath aus den Einstellungen
            GamePath = Settings.Default.GamePath;

            if (!string.IsNullOrWhiteSpace(GamePath))
            {
                // Neue Validierung der Dateierweiterung
                if (!Path.GetExtension(GamePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Invalid file type in settings. Only .exe files are allowed.", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    GamePath = null; // Erzwinge Neuaustwahl
                    Settings.Default.GamePath = ""; // Zurücksetzen
                    Settings.Default.Save();
                }
            }

            if (string.IsNullOrWhiteSpace(GamePath))
            {
                // Prüfe den Standardpfad
                if (File.Exists(DefaultGamePath))
                {
                    GamePath = DefaultGamePath;
                    Settings.Default.GamePath = GamePath;
                    Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("The game was not found in the default path. Please select the game executable manually.", "Game Path Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "Game Executable (*.exe)|*.exe",
                        Title = "Select Kingdom Come Deliverance 2 Executable"
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        GamePath = openFileDialog.FileName;
                        Settings.Default.GamePath = GamePath;
                        Settings.Default.Save();
                    }
                    else
                    {
                        MessageBox.Show("The game path is required to continue. Exiting the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
            }

            if (!File.Exists(GamePath))
            {
                MessageBox.Show("The saved game path is invalid. Please update the path in settings.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Bestimme den Mods-Ordner (eine Ebene oberhalb der Spielinstallation, anpassen wie benötigt)
            //ModFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(GamePath))), "Mods");
        }
        // Loads the current settings into the toggle buttons.
        private void LoadSettings()
        {
            // Load settings from Settings.Default
            ToggleDarkMode.IsChecked = Settings.Default.IsDarkMode;
            ToggleDevMode.IsChecked = Settings.Default.IsDevMode;
            ToggleDeleteConfirmation.IsChecked = Settings.Default.AskOnDelete;
            ToggleUpdateNotifications.IsChecked = Settings.Default.EnableUpdateNotifications;
            ToggleModOrderCreation.IsChecked = Settings.Default.ModOrderEnabled;
            ToggleBackupCreation.IsChecked = Settings.Default.CreateBackup;
            ToggleBackupOnStartup.IsChecked = Settings.Default.BackupOnStartup;
        }

        // Attach event handlers for Checked and Unchecked events.
        private void HookToggleEvents()
        {
            ToggleDarkMode.Checked += ToggleDarkMode_Checked;
            ToggleDarkMode.Unchecked += ToggleDarkMode_Unchecked;

            ToggleDevMode.Checked += ToggleDevMode_Checked;
            ToggleDevMode.Unchecked += ToggleDevMode_Unchecked;

            ToggleDeleteConfirmation.Checked += ToggleDeleteConfirmation_Checked;
            ToggleDeleteConfirmation.Unchecked += ToggleDeleteConfirmation_Unchecked;

            ToggleUpdateNotifications.Checked += ToggleUpdateNotifications_Checked;
            ToggleUpdateNotifications.Unchecked += ToggleUpdateNotifications_Unchecked;

            ToggleModOrderCreation.Checked += ToggleModOrderCreation_Checked;
            ToggleModOrderCreation.Unchecked += ToggleModOrderCreation_Unchecked;

            ToggleBackupCreation.Checked += ToggleBackupCreation_Checked;
            ToggleBackupCreation.Unchecked += ToggleBackupCreation_Unchecked;

            ToggleBackupOnStartup.Checked += ToggleBackupOnStartup_Checked;
            ToggleBackupOnStartup.Unchecked += ToggleBackupOnStartup_Unchecked;
        }

        // Event handlers to update settings on toggle changes
        private void ToggleDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsDarkMode = true;
            Settings.Default.Save();
            UpdateTheme();
        }
        private void ToggleDarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsDarkMode = false;
            Settings.Default.Save();
            UpdateTheme();
        }

        private void ToggleDevMode_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsDevMode = true;
            Settings.Default.Save();
        }
        private void ToggleDevMode_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsDevMode = false;
            Settings.Default.Save();
        }

        private void ToggleDeleteConfirmation_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AskOnDelete = true;
            Settings.Default.Save();
        }
        private void ToggleDeleteConfirmation_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AskOnDelete = false;
            Settings.Default.Save();
        }

        private void ToggleUpdateNotifications_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableUpdateNotifications = true;
            Settings.Default.Save();
        }
        private void ToggleUpdateNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableUpdateNotifications = false;
            Settings.Default.Save();
        }

        private void ToggleModOrderCreation_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.ModOrderEnabled = true;
            Settings.Default.Save();
        }
        private void ToggleModOrderCreation_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.ModOrderEnabled = false;
            Settings.Default.Save();
        }

        private void ToggleBackupCreation_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.CreateBackup = true;
            Settings.Default.Save();
        }
        private void ToggleBackupCreation_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.CreateBackup = false;
            Settings.Default.Save();
        }

        private void ToggleBackupOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.BackupOnStartup = true;
            Settings.Default.Save();
        }
        private void ToggleBackupOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.BackupOnStartup = false;
            Settings.Default.Save();
        }

        // Placeholder for Set Game Path action
        private void SetGamePath_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Set Game Path clicked.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            // Implement folder selection and update Settings.Default.GamePath here.
        }

        // Placeholder for Set Max Backups action
        private void SetMaxBackups_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter the maximum number of backups to keep:",
                                                                       "Set Max Backups",
                                                                       Settings.Default.BackupMaxCount.ToString());

            if (int.TryParse(input, out int maxBackups) && maxBackups > 0)
            {
                Settings.Default.BackupMaxCount = maxBackups;
                Settings.Default.Save();
                MessageBox.Show($"Max Backups set to {maxBackups}.", "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Invalid number. Please enter a positive integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Game Executable (*.exe)|*.exe",
                Title = "Select Kingdom Come Deliverance 2 Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GamePath = openFileDialog.FileName;
                SaveGamePath();
                //GamePathTextBox.Text = GamePath;
                //StatusLabel.Content = "Game path updated.";
            }
        }

        private void SaveGamePath()
        {
            Settings.Default.GamePath = GamePath;
            Settings.Default.Save();
            //ModFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(GamePath))), "Mods");
        }

    }
}
