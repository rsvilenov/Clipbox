using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace Setup.MsiBuilder.ViewModel
{
    /// <summary>
    /// DelegateCommand for parameterless commands
    /// </summary>
    public class DelegateCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;
        private bool _isExecuting;
        private ICommand changeClicked;

        public DelegateCommand(Action execute)
                       : this(execute, null)
        {
        }

        public DelegateCommand(Action execute,
                       Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public DelegateCommand(ICommand changeClicked)
        {
            this.changeClicked = changeClicked;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return !_isExecuting && _canExecute();
        }

        public void Execute(object parameter)
        {
            _isExecuting = true;
            try
            {
                RaiseCanExecuteChanged();
                _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
        
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private readonly Predicate<T> _canExecute;
        private readonly Action<T> _execute;
        private readonly DispatcherPriority? _dispatcherPriority;

        private bool _isExecuting;

        public DelegateCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action<T> execute, DispatcherPriority dispatcherPriority)
            : this(execute, null, dispatcherPriority)
        {
        }

        public DelegateCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public DelegateCommand(Action<T> execute, Predicate<T> canExecute, DispatcherPriority dispatcherPriority)
        {
            _execute = execute;
            _canExecute = canExecute;
            _dispatcherPriority = dispatcherPriority;
        }

        public bool CanExecute(T parameter)
        {
            if (_canExecute != null)
            {
                if (_dispatcherPriority.HasValue)
                {
                    return (bool)Dispatcher.CurrentDispatcher.Invoke(_dispatcherPriority.Value,
                        new Func<bool>(() => _canExecute(parameter)));
                }
                else
                {
                    return _canExecute(parameter);
                }
            }

            return true;
        }

        public void Execute(T parameter)
        {
            if (_dispatcherPriority.HasValue)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(_dispatcherPriority.Value,
                    new Action(() => _execute(parameter)));
            }
            else
            {
                _execute(parameter);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        
        #region ICommand Members

        bool ICommand.CanExecute(object parameter)
        {
            return !_isExecuting && CanExecute((T)parameter);
        }

        void ICommand.Execute(object parameter)
        {
            _isExecuting = true;
            try
            {
                RaiseCanExecuteChanged();
                Execute((T)parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}
