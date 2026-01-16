namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;

    public class RepositoryFormViewModel(RepositoryViewModel repository, GitService gitService, IMessenger messenger, IConsoleWriter consoleWriter) : ObservableObject
    {
        protected void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

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
    }
}
