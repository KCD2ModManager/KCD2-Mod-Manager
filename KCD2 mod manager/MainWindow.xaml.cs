using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using KCD2_mod_manager.ViewModels;
using KCD2_mod_manager.Services;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        private Point _dragStartPoint;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // ModsView für Filter setzen - nachdem ItemsSource gesetzt wurde
            ModList.ItemsSource = _viewModel.Mods;
            var modsView = CollectionViewSource.GetDefaultView(ModList.ItemsSource);
            _viewModel.SetModsView(modsView);

            // Event-Handler für Drag & Drop und UI-spezifische Events
            this.AllowDrop = true;
            this.DragOver += Window_DragOver;
            this.Drop += Window_Drop;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            // Theme initialisieren
            UpdateTheme();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            // Fenstergröße und Position aus Settings laden
            this.Width = _viewModel.Settings.WindowWidth;
            this.Height = _viewModel.Settings.WindowHeight;
            this.Left = _viewModel.Settings.WindowLeft;
            this.Top = _viewModel.Settings.WindowTop;

            if (System.Enum.TryParse(_viewModel.Settings.WindowState, out WindowState state))
            {
                this.WindowState = state;
            }

            EnsureWindowIsVisible();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel == null) return;

            // Fenstergröße und Position speichern
            if (this.WindowState == WindowState.Normal)
            {
                _viewModel.Settings.WindowWidth = (int)this.Width;
                _viewModel.Settings.WindowHeight = (int)this.Height;
                _viewModel.Settings.WindowLeft = (int)this.Left;
                _viewModel.Settings.WindowTop = (int)this.Top;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                _viewModel.Settings.WindowWidth = (int)this.RestoreBounds.Width;
                _viewModel.Settings.WindowHeight = (int)this.RestoreBounds.Height;
                _viewModel.Settings.WindowLeft = (int)this.RestoreBounds.Left;
                _viewModel.Settings.WindowTop = (int)this.RestoreBounds.Top;
            }

            _viewModel.Settings.WindowState = this.WindowState.ToString();
            _viewModel.Settings.Save();
        }

        private void EnsureWindowIsVisible()
        {
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight) this.Top = screenHeight - this.Height;
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) && _viewModel != null)
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (System.IO.Directory.Exists(file))
                    {
                        // Ordner-Verarbeitung
                        await _viewModel.ProcessModFolderAsync(file);
            }
            else
            {
                        // Datei-Verarbeitung
                        await _viewModel.ProcessModFileAsync(file);
                    }
                }
                await _viewModel.SaveModOrderAsync();
            }
        }

        private void ModList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            if (sender is ListBox listBox)
            {
                DependencyObject clickedElement = e.OriginalSource as DependencyObject;
                while (clickedElement != null && clickedElement != listBox)
                {
                    if (clickedElement is Button || clickedElement is CheckBox)
                        return;
                    clickedElement = System.Windows.Media.VisualTreeHelper.GetParent(clickedElement);
                }
                Point clickPosition = e.GetPosition(listBox);
                var clickedItem = listBox.InputHitTest(clickPosition) as FrameworkElement;
                if (clickedItem?.DataContext is Mod clickedMod)
                    listBox.SelectedItem = clickedMod;
            }
        }

        private void ModList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBox listBox && listBox.SelectedItem is Mod mod)
            {
                Point currentPoint = e.GetPosition(null);
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_viewModel != null && _viewModel.SortByLoadOrder)
                    {
                        DragDrop.DoDragDrop(listBox, mod, DragDropEffects.Move);
                    }
                }
            }
        }

        /// <summary>
        /// Drag & Drop Handler für Mod-Liste
        /// WICHTIG: 
        /// - Aktualisiert Number-Eigenschaften nach dem Verschieben
        /// - Speichert sofort in Profil und Spiel-Mods-Verzeichnis
        /// - UI wird automatisch aktualisiert
        /// </summary>
        private async void ModList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Mod)) is Mod draggedMod && sender is ListBox listBox && _viewModel != null)
            {
                if (!_viewModel.SortByLoadOrder) return;

                Point dropPosition = e.GetPosition(listBox);
                var targetMod = listBox.InputHitTest(dropPosition) as FrameworkElement;
                if (targetMod?.DataContext is Mod targetModData)
                {
                    int targetIndex = _viewModel.Mods.IndexOf(targetModData);
                    int draggedIndex = _viewModel.Mods.IndexOf(draggedMod);
                    
                    // WICHTIG: Mod in Collection verschieben
                    _viewModel.Mods.Move(draggedIndex, targetIndex);
                    
                    // WICHTIG: SaveModOrderAsync aktualisiert Number-Eigenschaften und speichert
                    // sowohl im Profil als auch im Spiel-Mods-Verzeichnis
                    await _viewModel.SaveModOrderAsync();
                }
                listBox.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void ModCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is Mod mod && _viewModel != null)
            {
                mod.IsEnabled = checkBox.IsChecked ?? false;
                _viewModel.ModCheckBoxCommand.Execute(mod);
            }
        }

        private void UpdateMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Mod mod && _viewModel != null)
            {
                _viewModel.UpdateModCommand.Execute(mod);
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Mod mod && _viewModel != null)
            {
                _viewModel.MoveUpCommand.Execute(mod);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Mod mod && _viewModel != null)
            {
                _viewModel.MoveDownCommand.Execute(mod);
            }
        }

        private void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Mod mod && _viewModel != null)
            {
                _viewModel.DeleteModCommand.Execute(mod);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Mod mod && _viewModel != null)
            {
                _viewModel.OpenFolderCommand.Execute(mod);
            }
        }

        /// <summary>
        /// Hilfsmethode: Holt das Mod-Objekt aus dem Context-Menü
        /// WICHTIG: Das Mod wird im Tag des ContextMenu gespeichert (siehe ModOptions_Click)
        /// </summary>
        private Mod? GetModFromContextMenu(MenuItem menuItem)
        {
            if (menuItem.Parent is ContextMenu contextMenu)
            {
                // Mod wurde im Tag gespeichert
                if (contextMenu.Tag is Mod mod)
                {
                    return mod;
                }
                
                // Fallback: Versuche aus PlacementTarget zu bekommen
                if (contextMenu.PlacementTarget is FrameworkElement fe && fe.DataContext is Mod buttonMod)
                {
                    return buttonMod;
                }
            }
            return null;
        }

        private void OpenModPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.OpenModPageCommand.Execute(mod);
                }
            }
        }

        private void ChangeModName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.ChangeModNameCommand.Execute(mod);
                }
            }
        }

        private void ChangeModNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.ChangeModNoteCommand.Execute(mod);
                }
            }
        }

        private void EndorseMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.EndorseModCommand.Execute(mod);
                }
            }
        }

        private void ChangeModNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.ChangeModNumberCommand.Execute(mod);
                }
            }
        }

        /// <summary>
        /// Handler für "Update-Prüfung umschalten" im Context-Menu
        /// WICHTIG: IsChecked-Binding (TwoWay) aktualisiert UpdateChecksEnabled automatisch.
        /// Dieser Handler speichert nur die Änderung in mod_versions.json.
        /// </summary>
        private void ToggleUpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                // WICHTIG: DataContext sollte das Mod-Objekt sein (gesetzt in ModOptions_Click)
                var mod = mi.DataContext as Mod;
                if (mod == null && mi.Parent is ContextMenu contextMenu)
                {
                    // Fallback: Versuche aus ContextMenu DataContext zu bekommen
                    mod = contextMenu.DataContext as Mod;
                }
                
                if (mod != null && _viewModel != null)
                {
                    // WICHTIG: IsChecked-Binding hat UpdateChecksEnabled bereits aktualisiert
                    // Wir müssen nur die Persistenz auslösen
                    _viewModel.ToggleUpdateCheckCommand.Execute(mod);
                }
            }
        }

        /// <summary>
        /// Handler für "Check for Update" im Context-Menu
        /// WICHTIG: Führt Update-Prüfung durch, öffnet KEINE Web-Seite
        /// </summary>
        private void CheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    // WICHTIG: RelayCommand.Execute ruft die async Methode auf, aber wartet nicht
                    // Die async Methode läuft im Hintergrund
                    _viewModel.CheckForUpdateCommand.Execute(mod);
                }
            }
        }

        private void SetModVersion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                var mod = GetModFromContextMenu(mi);
                if (mod != null && _viewModel != null)
                {
                    _viewModel.SetModVersionCommand.Execute(mod);
                }
            }
        }

        /// <summary>
        /// Öffnet das Context-Menü für Mod-Optionen
        /// WICHTIG: Setzt PlacementTarget defensiv, DataContext wird über XAML-Binding gesetzt
        /// </summary>
        private void ModOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                var mod = btn.DataContext as Mod;
                if (mod == null) return;

                // Set placement target & ensure DataContext binding works
                var cm = btn.ContextMenu;
                cm.PlacementTarget = btn;
                // Don't override cm.DataContext here — XAML binding handles it via PlacementTarget.DataContext
                cm.IsOpen = true;
            }
        }

        private void ModContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Mod mod && _viewModel != null)
            {
                // Context-Menu für Notizen öffnen
                _viewModel.ChangeModNoteCommand.Execute(mod);
            }
        }

        private void OpenSettingsWindow_Click(object sender, RoutedEventArgs e)
        {
            // SettingsWindow über DI erstellen - vereinfachte Version
            // Da wir keinen direkten Zugriff auf ServiceProvider haben, erstellen wir das ViewModel manuell
            // In einer vollständigen MVVM-Implementierung würde man einen ServiceLocator oder WindowFactory verwenden
            var app = (App)Application.Current;
            var serviceProvider = app.GetServiceProvider();
            if (serviceProvider != null)
            {
                var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
                settingsWindow.Owner = this;

                settingsWindow.ThemeChanged += (s, args) =>
                {
                    UpdateTheme();
                };

                settingsWindow.ShowDialog();
                UpdateTheme();
            }
        }

        private void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }

        private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.LogoutCommand.Execute(null);
            }
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.AddModCommand.Execute(null);
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ReloadCommand.Execute(null);
            }
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.StartGameCommand.Execute(null);
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SortCommand.Execute(null);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ClearSearchCommand.Execute(null);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && _viewModel != null)
            {
                _viewModel.SearchText = textBox.Text;
            }
        }

        /// <summary>
        /// Aktualisiert das Theme für MainWindow
        /// WICHTIG: Verwendet ThemeService zum Laden der Theme-Dictionaries
        /// </summary>
        private void UpdateTheme()
        {
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            if (serviceProvider != null)
            {
                var themeService = serviceProvider.GetService<IThemeService>();
                if (themeService != null)
                {
                    themeService.ApplyTheme(this.Resources, themeService.IsDarkMode);
                    this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                    if (StatusLabel != null)
                    {
                        StatusLabel.Foreground = (Brush)this.Resources["TextForegroundBrush"];
                    }
                }
            }
        }
    }
}


