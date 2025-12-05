using System.Windows.Input;

namespace KCD2_mod_manager.ViewModels
{
    /// <summary>
    /// Einfache RelayCommand-Implementierung für MVVM
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }

    /// <summary>
    /// Generische RelayCommand-Implementierung
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
                return _canExecute == null || _canExecute(default);
            
            if (parameter is T typedParameter)
                return _canExecute == null || _canExecute(typedParameter);
            
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
                _execute(typedParameter);
            else if (parameter == null && !typeof(T).IsValueType)
                _execute(default);
        }
    }
}

