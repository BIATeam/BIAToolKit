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

        protected abstract bool EnsureIsValid();

        public IRepository Model => repository;
        public bool IsGitRepository => repository.RepositoryType == Domain.RepositoryType.Git;
        public bool IsFolderRepository => repository.RepositoryType == Domain.RepositoryType.Folder;
        public ICommand SynchronizeCommand => new RelayCommand((_) => Synchronize());
        public ICommand GetReleasesCommand => new RelayCommand((_) => GetReleases());
        public ICommand CleanReleasesCommand => new RelayCommand((_) => CleanReleases());
        public ICommand OpenFormCommand => new RelayCommand((_) => eventBroker.RequestOpenRepositoryForm(this, RepositoryFormMode.Edit));
        public ICommand DeleteCommand => new RelayCommand((_) => eventBroker.NotifyRepositoryViewModelDeleted(this));

        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && EnsureIsValid();

        public bool IsVisibleCompanyName { get; set; } = true;
        public bool IsVisibleProjectName { get; set; } = true;

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
                RaisePropertyChanged(nameof(IsValid));
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

        public RepositoryType RepositoryType => repository.RepositoryType;

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

        public bool UseRepository
        {
            get => repository.UseRepository;
            set
            {
                repository.UseRepository = value;
                RaisePropertyChanged(nameof(UseRepository));
                eventBroker.NotifyRepositoriesUpdated();
            }
        }

        private void Synchronize()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                try
                {
                    if (IsGitRepository && repository is RepositoryGit repositoryGit)
                    {
                        await gitService.Synchronize(repositoryGit);
                    }

                    GetReleases();
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            });
        }

        private void GetReleases()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    await repository.FillReleasesAsync();
                    consoleWriter.AddMessageLine("Releases data got successfully", "green");
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            });
        }

        private void CleanReleases()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
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
