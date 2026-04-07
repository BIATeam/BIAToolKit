namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class RepositoryFormViewModel(RepositoryViewModel repository, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter, IDialogService dialogService) : ObservableObject
    {
        public RepositoryViewModel Repository
        {
            get => repository;
            set
            {
                repository = value;
                OnPropertyChanged(nameof(Repository));
            }
        }

        public static Array RepositoryTypes => Enum.GetValues<RepositoryType>();

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
                        eventBroker,
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
                        eventBroker,
                        consoleWriter)
                    {
                        IsVisibleCompanyName = repository.IsVisibleCompanyName,
                        IsVisibleProjectName = repository.IsVisibleProjectName
                    },
                    _ => throw new NotImplementedException(),
                };
                OnPropertyChanged(nameof(RepositoryType));
            }
        }

        [RelayCommand]
        private void BrowseLocalClonedFolder()
        {
            if (Repository is RepositoryGitViewModel repositoryGit)
            {
                repositoryGit.LocalClonedFolderPath = dialogService.BrowseFolder(repositoryGit.LocalClonedFolderPath, "Choose local cloned folder");
            }
        }

        [RelayCommand]
        private void BrowseRepositoryFolder()
        {
            if (Repository is RepositoryFolderViewModel repositoryFolder)
            {
                repositoryFolder.Path = dialogService.BrowseFolder(repositoryFolder.Path, "Choose source folder");
            }
        }
    }
}
