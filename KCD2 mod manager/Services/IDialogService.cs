using System.Windows;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Dialog-Services
    /// </summary>
    public interface IDialogService
    {
        bool? ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon);
        string? ShowOpenFileDialog(string filter, string title, bool multiselect = false);
        string? ShowSaveFileDialog(string filter, string title);
        string? ShowInputDialog(string prompt, string title, string defaultValue = "", bool isMultiline = false);
        bool? ShowDeleteConfirmation(out bool dontAskAgain);
        string? ShowFolderPicker(string description, string? initialPath = null);
        void ShowProgressDialog(string title, string message, CancellationToken cancellationToken, IProgress<string>? progress = null);
    }
}

