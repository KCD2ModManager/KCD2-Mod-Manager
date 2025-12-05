using System.IO;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Datei- und Verzeichnisoperationen
    /// </summary>
    public interface IFileService
    {
        Task ExtractArchiveAsync(string archivePath, string destination, CancellationToken cancellationToken = default);
        Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancellationToken = default);
        Task CopyModFilesAsync(string sourceDir, string targetDir, CancellationToken cancellationToken = default);
        Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
        Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);
        Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
        Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);
        Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive);
        void DeleteFile(string path);
        void MoveDirectory(string sourceDir, string destDir);
        void MoveFile(string sourceFile, string destFile);
        void CopyFile(string sourceFile, string destFile, bool overwrite);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
        IEnumerable<string> GetDirectories(string path);
        string Combine(params string[] paths);
        string GetDirectoryName(string path);
        string GetFileName(string path);
        string GetFileNameWithoutExtension(string path);
        string GetExtension(string path);
    }
}

