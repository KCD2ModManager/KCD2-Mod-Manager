using Xunit;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.Tests.Services
{
    /// <summary>
    /// Tests für AppSettings
    /// </summary>
    public class AppSettingsTests
    {
        [Fact]
        public void AppSettings_GetSetProperties_WorksCorrectly()
        {
            // Arrange
            var settings = new AppSettings();
            string testPath = @"C:\Test\Game.exe";
            bool testBool = true;
            int testInt = 42;

            // Act & Assert
            settings.GamePath = testPath;
            Assert.Equal(testPath, settings.GamePath);

            settings.IsDarkMode = testBool;
            Assert.Equal(testBool, settings.IsDarkMode);

            settings.BackupMaxCount = testInt;
            Assert.Equal(testInt, settings.BackupMaxCount);
        }

        [Fact]
        public void AppSettings_Save_DoesNotThrow()
        {
            // Arrange
            var settings = new AppSettings();

            // Act & Assert
            var exception = Record.Exception(() => settings.Save());
            Assert.Null(exception);
        }
    }
}

