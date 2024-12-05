namespace BIA.ToolKit.Application.ViewModel.MicroMvvm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;

    internal interface IRelayCommand<T> : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}
