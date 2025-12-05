using Xunit;
using KCD2_mod_manager.Models;
using System.Windows;

namespace KCD2_mod_manager.Tests.Models
{
    /// <summary>
    /// Tests für Mod-Model
    /// </summary>
    public class ModTests
    {
        [Fact]
        public void Mod_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var mod = new Mod();

            // Assert
            Assert.NotNull(mod.Id);
            Assert.NotNull(mod.Name);
            Assert.NotNull(mod.Version);
            Assert.NotNull(mod.Path);
            Assert.False(mod.IsEnabled);
            Assert.Equal(0, mod.Number);
            Assert.False(mod.HasUpdate);
        }

        [Fact]
        public void Mod_HasUpdate_UpdateVisibilityIsVisible()
        {
            // Arrange
            var mod = new Mod { HasUpdate = true };

            // Act
            var visibility = mod.UpdateVisibility;

            // Assert
            Assert.Equal(Visibility.Visible, visibility);
        }

        [Fact]
        public void Mod_NoUpdate_UpdateVisibilityIsCollapsed()
        {
            // Arrange
            var mod = new Mod { HasUpdate = false };

            // Act
            var visibility = mod.UpdateVisibility;

            // Assert
            Assert.Equal(Visibility.Collapsed, visibility);
        }

        [Fact]
        public void Mod_PropertyChanged_IsRaised()
        {
            // Arrange
            var mod = new Mod();
            bool propertyChangedRaised = false;
            mod.PropertyChanged += (s, e) => propertyChangedRaised = true;

            // Act
            mod.Name = "New Name";

            // Assert
            Assert.True(propertyChangedRaised);
        }
    }
}

