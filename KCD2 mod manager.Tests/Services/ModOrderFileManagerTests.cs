using System.IO;
using System.Threading.Tasks;
using Xunit;
using KCD2_mod_manager.Services;
using Moq;

namespace KCD2_mod_manager.Tests.Services
{
    /// <summary>
    /// Unit-Tests für ModOrderFileManager
    /// WICHTIG: Diese Tests verwenden echte Datei-Operationen für zuverlässigere Ergebnisse
    /// </summary>
    public class ModOrderFileManagerTests : IDisposable
    {
        private readonly string _testModFolder;
        private readonly FileService _fileService;
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly ModOrderFileManager _manager;

        public ModOrderFileManagerTests()
        {
            // Erstelle temporäres Test-Verzeichnis
            _testModFolder = Path.Combine(Path.GetTempPath(), $"ModOrderTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testModFolder);

            _fileService = new FileService();
            _loggerMock = new Mock<ILog>();
            _dialogServiceMock = new Mock<IDialogService>();

            _manager = new ModOrderFileManager(_fileService, _loggerMock.Object, _dialogServiceMock.Object);
        }

        public void Dispose()
        {
            // Cleanup: Lösche Test-Verzeichnis
            try
            {
                if (Directory.Exists(_testModFolder))
                {
                    Directory.Delete(_testModFolder, true);
                }
            }
            catch
            {
                // Ignoriere Cleanup-Fehler
            }
        }

        [Fact]
        public async Task ApplyModOrderSetting_EnabledTrue_WithBackup_RestoresModOrderTxt()
        {
            // Arrange
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            string modOrderBackupPath = Path.Combine(_testModFolder, "mod_order_backup.txt");
            
            // Erstelle Backup-Datei
            await File.WriteAllLinesAsync(modOrderBackupPath, new[] { "mod1", "mod2" });

            // Act
            await _manager.ApplyModOrderSettingAsync(true, _testModFolder);

            // Assert
            Assert.True(File.Exists(modOrderPath), "mod_order.txt sollte existieren");
            Assert.False(File.Exists(modOrderBackupPath), "mod_order_backup.txt sollte nicht existieren");
            var content = await File.ReadAllLinesAsync(modOrderPath);
            Assert.Equal(2, content.Length);
            Assert.Equal("mod1", content[0]);
            Assert.Equal("mod2", content[1]);
        }

        [Fact]
        public async Task ApplyModOrderSetting_EnabledFalse_WithModOrderTxt_MovesToBackup()
        {
            // Arrange
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            string modOrderBackupPath = Path.Combine(_testModFolder, "mod_order_backup.txt");
            
            // Erstelle mod_order.txt
            await File.WriteAllLinesAsync(modOrderPath, new[] { "mod1", "mod2" });

            // Act
            await _manager.ApplyModOrderSettingAsync(false, _testModFolder);

            // Assert
            Assert.False(File.Exists(modOrderPath), "mod_order.txt sollte nicht existieren");
            Assert.True(File.Exists(modOrderBackupPath), "mod_order_backup.txt sollte existieren");
            var content = await File.ReadAllLinesAsync(modOrderBackupPath);
            Assert.Equal(2, content.Length);
            Assert.Equal("mod1", content[0]);
            Assert.Equal("mod2", content[1]);
        }

        [Fact]
        public async Task ConsolidateModOrderFiles_EnabledTrue_BackupExists_RestoresModOrderTxt()
        {
            // Arrange
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            string modOrderBackupPath = Path.Combine(_testModFolder, "mod_order_backup.txt");
            
            await File.WriteAllLinesAsync(modOrderBackupPath, new[] { "mod1" });

            // Act
            await _manager.ConsolidateModOrderFilesAsync(true, _testModFolder);

            // Assert
            Assert.True(File.Exists(modOrderPath));
            Assert.False(File.Exists(modOrderBackupPath));
            var content = await File.ReadAllLinesAsync(modOrderPath);
            Assert.Single(content);
            Assert.Equal("mod1", content[0]);
        }

        [Fact]
        public async Task ConsolidateModOrderFiles_EnabledFalse_ModOrderTxtExists_MovesToBackup()
        {
            // Arrange
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            string modOrderBackupPath = Path.Combine(_testModFolder, "mod_order_backup.txt");
            
            await File.WriteAllLinesAsync(modOrderPath, new[] { "mod1" });

            // Act
            await _manager.ConsolidateModOrderFilesAsync(false, _testModFolder);

            // Assert
            Assert.False(File.Exists(modOrderPath));
            Assert.True(File.Exists(modOrderBackupPath));
            var content = await File.ReadAllLinesAsync(modOrderBackupPath);
            Assert.Single(content);
            Assert.Equal("mod1", content[0]);
        }

        [Fact]
        public async Task ConsolidateModOrderFiles_EnabledFalse_BothExist_DeletesModOrderTxt()
        {
            // Arrange
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            string modOrderBackupPath = Path.Combine(_testModFolder, "mod_order_backup.txt");
            
            await File.WriteAllLinesAsync(modOrderPath, new[] { "mod1" });
            await File.WriteAllLinesAsync(modOrderBackupPath, new[] { "mod2" });

            // Act
            await _manager.ConsolidateModOrderFilesAsync(false, _testModFolder);

            // Assert
            Assert.False(File.Exists(modOrderPath));
            Assert.True(File.Exists(modOrderBackupPath));
            var content = await File.ReadAllLinesAsync(modOrderBackupPath);
            Assert.Single(content);
            Assert.Equal("mod2", content[0]); // Backup bleibt unverändert
        }

        [Fact]
        public async Task ApplyModOrderSetting_EnabledTrue_NoBackup_CreatesEmptyModOrderTxt()
        {
            // Arrange: Keine Dateien vorhanden

            // Act
            await _manager.ApplyModOrderSettingAsync(true, _testModFolder);

            // Assert
            string modOrderPath = Path.Combine(_testModFolder, "mod_order.txt");
            Assert.True(File.Exists(modOrderPath));
            var content = await File.ReadAllLinesAsync(modOrderPath);
            Assert.Empty(content); // Leere Datei
        }
    }
}

