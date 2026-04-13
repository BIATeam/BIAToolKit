namespace BIA.ToolKit.Dialogs
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.ViewModels;

    public partial class RepositoryFormUC : UserControl
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(RepositoryViewModel repository, GitService gitService, IConsoleWriter consoleWriter)
        {
            DataContext = new RepositoryFormViewModel(repository, gitService, consoleWriter, App.GetService<IDialogService>());
            InitializeComponent();
        }
    }
}
