using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Mod-Installations-Operationen
    /// </summary>
    public interface IModInstallerService
    {
        Task<Mod?> ProcessModFileAsync(string filePath, string modFolder, CancellationToken cancellationToken = default);
        Task<Mod?> ProcessModFolderAsync(string folderPath, string modFolder, CancellationToken cancellationToken = default);
        Task<Mod?> ProcessModUpdateAsync(string archivePath, string targetDir, string originalId, CancellationToken cancellationToken = default);
        Task<List<Mod>> LoadModsAsync(string modFolder, CancellationToken cancellationToken = default);
        Task SaveModOrderAsync(List<Mod> mods, string modFolder, CancellationToken cancellationToken = default);
        Task SaveModVersionAsync(string modId, string version, int modNumber, string installedFileName, string modFolder, bool updateChecksEnabled = true, CancellationToken cancellationToken = default);
        Task<Dictionary<string, ModVersionInfo>> LoadModVersionsAsync(string modFolder, CancellationToken cancellationToken = default);
        Task SaveModNotesAsync(Dictionary<string, string> modNotes, string modFolder, CancellationToken cancellationToken = default);
        Task<Dictionary<string, string>> LoadModNotesAsync(string modFolder, CancellationToken cancellationToken = default);
        Task CreateModsBackupAsync(string modFolder, CancellationToken cancellationToken = default);
        int ExtractModNumberFromFileName(string fileName);
        string ExtractVersionFromFileName(string fileName);
    }
}

