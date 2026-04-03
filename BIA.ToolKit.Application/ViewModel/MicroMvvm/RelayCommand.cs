namespace BIA.ToolKit.Application.ViewModel.MicroMvvm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;

    internal class RelayCommand(Action<object> execute, Predicate<object> canExecute) : ICommand
    {
        private readonly Action<object> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Predicate<object> _canExecute = canExecute;

        public RelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

#pragma warning disable CS0067 // CanExecuteChanged is required by ICommand interface but not used in this simple implementation
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }

    internal class RelayCommand<T>(Action<T> execute, Predicate<T> canExecute) : IRelayCommand<T>
    {
        private readonly Action<T> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Predicate<T> _canExecute = canExecute;

        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || (parameter is T param && _canExecute(param));
        }

        public void Execute(object parameter)
        {
            if (parameter is T param)
            {
                _execute(param);
            }
            else
            {
                throw new InvalidOperationException($"Invalid parameter type, expected: {typeof(T)}, received: {parameter?.GetType()}");
            }
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
