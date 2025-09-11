namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;

    public abstract class RepositoryViewModel : ObservableObject
    {
        private readonly Repository repository;
        protected readonly GitService gitService;
        protected readonly UIEventBroker eventBroker;
        protected readonly IConsoleWriter consoleWriter;

        protected RepositoryViewModel(Repository repository, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            ArgumentNullException.ThrowIfNull(repository, nameof(repository));
            this.repository = repository;
            this.gitService = gitService;
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
        }

        public IRepository Model => repository;
        public bool IsGitRepository => repository.RepositoryType == Domain.RepositoryType.Git;
        public bool IsFolderRepository => repository.RepositoryType == Domain.RepositoryType.Folder;
        public ICommand SynchronizeCommand => new RelayCommand((_) => Synchronize());
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

        public ReadOnlySpan<string> RepositoryTypes => Enum.GetNames<RepositoryType>();

        public string RepositoryType
        {
            get => $"{repository.RepositoryType}";
            set
            {
                repository.RepositoryType = Enum.Parse<RepositoryType>(value);
                RaisePropertyChanged(nameof(RepositoryType));
                RaisePropertyChanged(nameof(IsGitRepository));
                RaisePropertyChanged(nameof(IsFolderRepository));
            }
        }

        public string Resource
        {
            get
            {
                return repository switch
                {
                    RepositoryGit repositoryGit => repositoryGit.Url,
                    RepositoryFolder repositoryFolder => repositoryFolder.Path,
                    _ => throw new NotImplementedException()
                };
            }
        }

        private void Synchronize()
        {
            eventBroker.ExecuteActionWithWaiter(async () =>
            {
                if (IsGitRepository && repository is RepositoryGit repositoryGit)
                {
                    await gitService.Synchronize(repositoryGit);
                }
                await repository.FillReleasesAsync();
            });
        }

        private void CleanReleases()
        {
            eventBroker.ExecuteActionWithWaiter(async () =>
            {
                try
                {
                    consoleWriter.AddMessageLine($"Cleaning releases of repository {Name}...", "pink");
                    await Task.Run(repository.CleanReleases);
                    consoleWriter.AddMessageLine($"Releases cleaned", "green");
                }
                catch(Exception ex)
                {
                    consoleWriter.AddMessageLine($"Failed to clean releases : {ex.Message}");
                }
            });
        }
    }
}
