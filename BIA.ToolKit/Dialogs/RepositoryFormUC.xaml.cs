namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Infrastructure;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// DataContext (RepositoryFormViewModel) is set in constructor.
    /// Code-behind contains NO business logic.
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(RepositoryViewModel repository, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            DataContext = new RepositoryFormViewModel(repository, gitService, eventBroker, consoleWriter, App.GetService<IDialogService>());
            InitializeComponent();
        }
    }
}
