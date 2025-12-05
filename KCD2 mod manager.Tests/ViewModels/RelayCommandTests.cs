using Xunit;
using KCD2_mod_manager.ViewModels;

namespace KCD2_mod_manager.Tests.ViewModels
{
    /// <summary>
    /// Tests für RelayCommand
    /// </summary>
    public class RelayCommandTests
    {
        [Fact]
        public void RelayCommand_Execute_CallsAction()
        {
            // Arrange
            bool executed = false;
            var command = new RelayCommand(_ => executed = true);

            // Act
            command.Execute(null);

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public void RelayCommand_CanExecute_ReturnsTrueWhenNoPredicate()
        {
            // Arrange
            var command = new RelayCommand(_ => { });

            // Act
            bool canExecute = command.CanExecute(null);

            // Assert
            Assert.True(canExecute);
        }

        [Fact]
        public void RelayCommand_CanExecute_RespectsPredicate()
        {
            // Arrange
            bool canExecuteValue = false;
            var command = new RelayCommand(_ => { }, _ => canExecuteValue);

            // Act & Assert
            Assert.False(command.CanExecute(null));
            canExecuteValue = true;
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void RelayCommandT_Execute_CallsActionWithParameter()
        {
            // Arrange
            string? receivedValue = null;
            var command = new RelayCommand<string>(value => receivedValue = value);

            // Act
            command.Execute("test");

            // Assert
            Assert.Equal("test", receivedValue);
        }
    }
}

