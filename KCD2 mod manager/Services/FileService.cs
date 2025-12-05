using SharpCompress.Archives;
using SharpCompress.Common;
using System.IO;
using System.Collections.Generic;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von IFileService mit asynchronen Operationen
    /// </summary>
    public class FileService : IFileService
    {
        public async Task ExtractArchiveAsync(string archivePath, string destination, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath);
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entry.WriteToDirectory(destination, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }, cancellationToken);
        }

        public async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string destFile = Combine(destDir, GetFileName(file));
                    File.Copy(file, destFile, true);
                }

                foreach (var subDir in Directory.GetDirectories(sourceDir))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string destSubDir = Combine(destDir, GetFileName(subDir));
                    CreateDirectory(destSubDir);
                    CopyDirectoryAsync(subDir, destSubDir, cancellationToken).Wait(cancellationToken);
                }
            }, cancellationToken);
        }

        public async Task CopyModFilesAsync(string sourceDir, string targetDir, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var targetSubDir = Combine(targetDir, GetFileName(dir));
                    CreateDirectory(targetSubDir);
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        var targetFile = Combine(targetSubDir, GetFileName(file));
                        CopyFile(file, targetFile, true);
                    }
                }
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var targetFile = Combine(targetDir, GetFileName(file));
                    CopyFile(file, targetFile, true);
                }
            }, cancellationToken);
        }

        public async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(path, contents, cancellationToken);
        }

        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        public async Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await File.WriteAllLinesAsync(path, lines, cancellationToken);
        }

        public async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        {
            return await File.ReadAllLinesAsync(path, cancellationToken);
        }

        public async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        }

        public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        public bool FileExists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        /// <summary>
        /// Erstellt ein Verzeichnis, falls es nicht existiert
        /// WICHTIG: Alle Mod-Ordner-Operationen müssen sicherstellen, dass Verzeichnisse existieren
        /// </summary>
        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
        public void DeleteFile(string path) => File.Delete(path);
        public void MoveDirectory(string sourceDir, string destDir) => Directory.Move(sourceDir, destDir);
        public void MoveFile(string sourceFile, string destFile) => File.Move(sourceFile, destFile);
        public void CopyFile(string sourceFile, string destFile, bool overwrite) => File.Copy(sourceFile, destFile, overwrite);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => Directory.EnumerateFiles(path, searchPattern, searchOption);
        public IEnumerable<string> GetDirectories(string path) => Directory.GetDirectories(path);
        public string Combine(params string[] paths) => System.IO.Path.Combine(paths);
        public string GetDirectoryName(string path) => System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        public string GetFileName(string path) => System.IO.Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
        public string GetExtension(string path) => System.IO.Path.GetExtension(path);
    }
}

