namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Infrastructure;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(RepositoryViewModel repository, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            DataContext = new RepositoryFormViewModel(repository, gitService, eventBroker, consoleWriter, App.GetService<IDialogService>());
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BrowseLocalClonedFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Repository is RepositoryGitViewModel repositoryGit)
            {
                repositoryGit.LocalClonedFolderPath = FileDialog.BrowseFolder(repositoryGit.LocalClonedFolderPath, "Choose local cloned folder");
            }
        }

        private void BrowseRepositoryFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Repository is RepositoryFolderViewModel repositoryFolder)
            {
                repositoryFolder.Path = FileDialog.BrowseFolder(repositoryFolder.Path, "Choose source folder");
            }
        }
    }
}
