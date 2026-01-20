namespace BIA.ToolKit.ViewModels
{
    using System;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// ViewModel for the Repository Form dialog.
    /// Refactored to use DI for all services and Commands for UI actions.
    /// </summary>
    public partial class RepositoryFormViewModel : ObservableObject
    {
        private readonly GitService gitService;
        private readonly IMessenger messenger;
        private readonly IConsoleWriter consoleWriter;
        private readonly IFileDialogService fileDialogService;
        private RepositoryViewModel repository;

        protected void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        public RepositoryFormViewModel(
            RepositoryViewModel repository,
            GitService gitService,
            IMessenger messenger,
            IConsoleWriter consoleWriter,
            IFileDialogService fileDialogService)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        }

        public RepositoryViewModel Repository
        {
            get => repository;
            set
            {
                repository = value;
                RaisePropertyChanged(nameof(Repository));
            }
        }

        public Array RepositoryTypes => Enum.GetValues<RepositoryType>();

        public RepositoryType RepositoryType
        {
            get => Repository.RepositoryType;
            set
            {
                Repository = value switch
                {
                    RepositoryType.Git => new RepositoryGitViewModel(
                        RepositoryGit.CreateWithReleaseTypeGit(
                            repository.Name, RepositoryGitKind.Github,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            companyName: repository.CompanyName,
                            projectName: repository.ProjectName,
                            useRepository: repository.UseRepository),
                        gitService,
                        messenger,
                        consoleWriter)
                    { 
                        IsVisibleCompanyName = repository.IsVisibleCompanyName, 
                        IsVisibleProjectName = repository.IsVisibleProjectName 
                    },

                    RepositoryType.Folder => new RepositoryFolderViewModel(
                        new RepositoryFolder(
                            repository.Name,
                            string.Empty,
                            companyName: repository.CompanyName,
                            projectName: repository.ProjectName,
                            useRepository: repository.UseRepository),
                        gitService,
                        messenger,
                        consoleWriter)
                    {
                        IsVisibleCompanyName = repository.IsVisibleCompanyName,
                        IsVisibleProjectName = repository.IsVisibleProjectName
                    },
                        _ => throw new NotImplementedException(),
                };
                RaisePropertyChanged(nameof(RepositoryType));
            }
        }

        /// <summary>
        /// Command to browse for local cloned folder (Git repository).
        /// </summary>
        [RelayCommand]
        private void BrowseLocalClonedFolder()
        {
            if (Repository is RepositoryGitViewModel repositoryGit)
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

        /// <summary>
        /// Command to browse for repository folder (Folder repository).
        /// </summary>
        [RelayCommand]
        private void BrowseRepositoryFolder()
        {
            if (Repository is RepositoryFolderViewModel repositoryFolder)
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