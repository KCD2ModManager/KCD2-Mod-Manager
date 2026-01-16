using Xunit;
using KCD2_mod_manager.Services;
using Moq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace KCD2_mod_manager.Tests.Services
{
    /// <summary>
    /// Tests für ModManifestService
    /// </summary>
    public class ModManifestServiceTests
    {
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IAppSettings> _settingsMock;
        private readonly RenameService _renameService;
        private readonly ModManifestService _service;

        public ModManifestServiceTests()
        {
            _fileServiceMock = new Mock<IFileService>();
            _loggerMock = new Mock<ILog>();
            _settingsMock = new Mock<IAppSettings>();
            _settingsMock.SetupGet(s => s.EnableFileRenaming).Returns(true);
            _renameService = new RenameService(_settingsMock.Object);
            _service = new ModManifestService(_fileServiceMock.Object, _loggerMock.Object, _renameService, _settingsMock.Object);
        }

        [Fact]
        public void GenerateModId_RemovesOnlyInvalidCharacters()
        {
            // Arrange
            string name = "Test_Mod-Name<>:\"/\\|?*";

            // Act
            string result = _service.GenerateModId(name);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("Test_Mod-Name", result);
        }

        [Fact]
        public void GenerateModId_InvalidCharactersOnly_ReturnsFallback()
        {
            // Arrange
            string name = "<>:\"/\\|?*";

            // Act
            string result = _service.GenerateModId(name);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GenerateModId_EmptyString_ReturnsFallback()
        {
            // Arrange
            string name = "";

            // Act
            string result = _service.GenerateModId(name);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}

