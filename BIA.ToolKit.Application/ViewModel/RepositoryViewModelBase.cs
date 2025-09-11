namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public abstract class RepositoryViewModelBase : ObservableObject
    {
        private readonly Repository repository;
        protected readonly GitService gitService;

        protected RepositoryViewModelBase(Repository repository, GitService gitService)
        {
            ArgumentNullException.ThrowIfNull(repository, nameof(repository));
            this.repository = repository;
            this.gitService = gitService;
        }

        public bool IsGitRepository => RepositoryType == RepositoryType.Git;
        public bool IsFolderRepository => RepositoryType == RepositoryType.Folder;
        public ICommand SynchronizeCommand => new RelayCommand(async (_) => await Synchronize());
        public ICommand CleanReleasesCommand => new RelayCommand((_) => CleanReleases());

        public string CompanyName
        {
            get => repository.CompanyName;
            set
            {
                repository.CompanyName = value;
                RaisePropertyChanged(nameof(CompanyName));
            }
        }

        public string Name
        {
            get => repository.Name;
            set
            {
                repository.Name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public string ProjectName
        {
            get => repository.ProjectName;
            set
            {
                repository.ProjectName = value;
                RaisePropertyChanged(nameof(ProjectName));
            }
        }

        public RepositoryType RepositoryType
        {
            get => repository.RepositoryType;
            set
            {
                repository.RepositoryType = value;
                RaisePropertyChanged(nameof(RepositoryType));
                RaisePropertyChanged(nameof(IsGitRepository));
                RaisePropertyChanged(nameof(IsFolderRepository));
            }
        }

        private async Task Synchronize()
        {
            if (IsGitRepository && repository is RepositoryGit repositoryGit)
            {
                await gitService.Synchronize(repositoryGit);
            }
            await repository.FillReleasesAsync();
        }

        private void CleanReleases()
        {
            repository.CleanReleases();
        }
    }
}
