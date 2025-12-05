using Xunit;
using KCD2_mod_manager.Services;
using System.IO;
using System.Threading.Tasks;

namespace KCD2_mod_manager.Tests.Services
{
    /// <summary>
    /// Tests für FileService
    /// </summary>
    public class FileServiceTests
    {
        private readonly FileService _service;

        public FileServiceTests()
        {
            _service = new FileService();
        }

        [Fact]
        public void Combine_MultiplePaths_ReturnsCombinedPath()
        {
            // Arrange
            string[] paths = { "C:", "Test", "Folder", "File.txt" };

            // Act
            string result = _service.Combine(paths);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Test", result);
            Assert.Contains("Folder", result);
            Assert.Contains("File.txt", result);
        }

        [Fact]
        public void GetFileName_ValidPath_ReturnsFileName()
        {
            // Arrange
            string path = @"C:\Test\Folder\File.txt";

            // Act
            string result = _service.GetFileName(path);

            // Assert
            Assert.Equal("File.txt", result);
        }

        [Fact]
        public void GetFileNameWithoutExtension_ValidPath_ReturnsFileNameWithoutExtension()
        {
            // Arrange
            string path = @"C:\Test\Folder\File.txt";

            // Act
            string result = _service.GetFileNameWithoutExtension(path);

            // Assert
            Assert.Equal("File", result);
        }

        [Fact]
        public void GetExtension_ValidPath_ReturnsExtension()
        {
            // Arrange
            string path = @"C:\Test\Folder\File.txt";

            // Act
            string result = _service.GetExtension(path);

            // Assert
            Assert.Equal(".txt", result);
        }

        [Fact]
        public void GetDirectoryName_ValidPath_ReturnsDirectoryName()
        {
            // Arrange
            string path = @"C:\Test\Folder\File.txt";

            // Act
            string result = _service.GetDirectoryName(path);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Folder", result);
        }
    }
}

