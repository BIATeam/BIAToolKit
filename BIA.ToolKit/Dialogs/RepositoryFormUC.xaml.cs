namespace BIA.ToolKit.Dialogs
{
    using System;
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Infrastructure.Services;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// Refactored to use IFileDialogService (SOLID principle).
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;
        private readonly IFileDialogService fileDialogService;

        public RepositoryFormUC(
            RepositoryViewModel repository,
            GitService gitService,
            IMessenger messenger,
            IConsoleWriter consoleWriter,
            IFileDialogService fileDialogService = null)
        {
            this.fileDialogService = fileDialogService ?? new FileDialogService();
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
                var selectedPath = fileDialogService.BrowseFolder(
                    repositoryGit.LocalClonedFolderPath,
                    "Choose local cloned folder");

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    repositoryGit.LocalClonedFolderPath = selectedPath;
                }
            }
        }

        private void BrowseRepositoryFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Repository is RepositoryFolderViewModel repositoryFolder)
            {
                var selectedPath = fileDialogService.BrowseFolder(
                    repositoryFolder.Path,
                    "Choose source folder");

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    repositoryFolder.Path = selectedPath;
                }
            }
        }
    }
}
