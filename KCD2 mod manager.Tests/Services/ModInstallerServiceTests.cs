using Xunit;
using KCD2_mod_manager.Services;
using Moq;
using System.Threading.Tasks;

namespace KCD2_mod_manager.Tests.Services
{
    /// <summary>
    /// Tests für ModInstallerService
    /// </summary>
    public class ModInstallerServiceTests
    {
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly Mock<IModManifestService> _manifestServiceMock;
        private readonly Mock<IAppSettings> _settingsMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IGameInstallService> _gameInstallServiceMock;
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IModCategoryAssignmentService> _categoryAssignmentServiceMock;
        private readonly ModInstallerService _service;

        public ModInstallerServiceTests()
        {
            _fileServiceMock = new Mock<IFileService>();
            _manifestServiceMock = new Mock<IModManifestService>();
            _settingsMock = new Mock<IAppSettings>();
            _dialogServiceMock = new Mock<IDialogService>();
            _gameInstallServiceMock = new Mock<IGameInstallService>();
            _loggerMock = new Mock<ILog>();
            _categoryAssignmentServiceMock = new Mock<IModCategoryAssignmentService>();

            _settingsMock.Setup(s => s.ModOrderEnabled).Returns(true);
            _settingsMock.Setup(s => s.CreateBackup).Returns(false);

            _service = new ModInstallerService(
                _fileServiceMock.Object,
                _manifestServiceMock.Object,
                _settingsMock.Object,
                _dialogServiceMock.Object,
                _gameInstallServiceMock.Object,
                _loggerMock.Object,
                _categoryAssignmentServiceMock.Object);
        }

        [Fact]
        public void ExtractVersionFromFileName_ValidFileName_ReturnsVersion()
        {
            // Arrange
            string fileName = "ModName-123-1-0-1234567890.zip";
            _fileServiceMock.Setup(f => f.GetFileNameWithoutExtension(fileName)).Returns("ModName-123-1-0-1234567890");

            // Act
            string result = _service.ExtractVersionFromFileName(fileName);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ExtractVersionFromFileName_InvalidFileName_ReturnsEmpty()
        {
            // Arrange
            string fileName = "NoVersion.zip";
            _fileServiceMock.Setup(f => f.GetFileNameWithoutExtension(fileName)).Returns("NoVersion");

            // Act
            string result = _service.ExtractVersionFromFileName(fileName);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ExtractModNumberFromFileName_ValidFileName_ReturnsNumber()
        {
            // Arrange
            string fileName = "ModName-123-1-0-1234567890.zip";
            _fileServiceMock.Setup(f => f.GetFileNameWithoutExtension(fileName)).Returns("ModName-123-1-0-1234567890");

            // Act
            int result = _service.ExtractModNumberFromFileName(fileName);

            // Assert
            Assert.True(result > 0);
        }
    }
}

