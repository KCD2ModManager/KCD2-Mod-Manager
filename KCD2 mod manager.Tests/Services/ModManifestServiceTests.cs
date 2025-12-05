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
        private readonly ModManifestService _service;

        public ModManifestServiceTests()
        {
            _fileServiceMock = new Mock<IFileService>();
            _loggerMock = new Mock<ILog>();
            _service = new ModManifestService(_fileServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void GenerateModId_ValidName_ReturnsLowercaseAlphabetic()
        {
            // Arrange
            string name = "Test Mod Name 123";

            // Act
            string result = _service.GenerateModId(name);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, c => Assert.True(char.IsLower(c) && char.IsLetter(c)));
        }

        [Fact]
        public void GenerateModId_SpecialCharactersOnly_ReturnsFallback()
        {
            // Arrange
            string name = "123!@#$%";

            // Act
            string result = _service.GenerateModId(name);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // Sollte einen Fallback-Wert zurückgeben
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

