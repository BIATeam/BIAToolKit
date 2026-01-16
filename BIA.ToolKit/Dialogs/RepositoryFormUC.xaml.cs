namespace BIA.ToolKit.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(RepositoryViewModel repository, GitService gitService, IMessenger messenger, IConsoleWriter consoleWriter)
        {
            DataContext = new RepositoryFormViewModel(repository, gitService, messenger, consoleWriter);
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BrowseLocalClonedFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.Repository is RepositoryGitViewModel repositoryGit)
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
