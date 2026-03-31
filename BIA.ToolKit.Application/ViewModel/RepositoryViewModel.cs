namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public abstract partial class RepositoryViewModel : ObservableObject
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
            eventBroker.OnRepositoryViewModelVersionXYZChanged += EventBroker_OnRepositoryViewModelVersionXYZChanged;
        }

        private void EventBroker_OnRepositoryViewModelVersionXYZChanged(RepositoryViewModel repository)
        {
            if (repository == this)
                return;

            IsVersionXYZ = false;
        }

        protected abstract bool EnsureIsValid();

        public IRepository Model => repository;
        public bool IsGitRepository => repository.RepositoryType == Domain.RepositoryType.Git;
        public bool IsFolderRepository => repository.RepositoryType == Domain.RepositoryType.Folder;

        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && EnsureIsValid();
        public bool IsVisibleCompanyName { get; set; } = true;
        public bool IsVisibleProjectName { get; set; } = true;
        public bool CanBeVersionXYZ { get; set; }

        public bool IsVersionXYZ
        {
            get => repository is RepositoryGit repositoryGit && repositoryGit.IsVersionXYZ;
            set
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    repositoryGit.IsVersionXYZ = value;
                    OnPropertyChanged(nameof(IsVersionXYZ));
                    if (value == true)
                    {
                        eventBroker.NotifyViewModelVersionXYZChanged(this);
                    }
                }
            }
        }

        public string CompanyName
        {
            get => repository.CompanyName;
            set
            {
                repository.CompanyName = value;
                OnPropertyChanged(nameof(CompanyName));
            }
        }

        public string Name
        {
            get => repository.Name;
            set
            {
                repository.Name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ProjectName
        {
            get => repository.ProjectName;
            set
            {
                repository.ProjectName = value;
                OnPropertyChanged(nameof(ProjectName));
            }
        }

        public RepositoryType RepositoryType => repository.RepositoryType;

        public string Source
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

        public string LocalPath => repository.LocalPath;

        public bool UseRepository
        {
            get => repository.UseRepository;
            set
            {
                repository.UseRepository = value;
                OnPropertyChanged(nameof(UseRepository));
                eventBroker.NotifyRepositoriesUpdated();
            }
        }

        [RelayCommand]
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
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            });
        }

        [RelayCommand]
        private void OpenSource()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                if (Model is RepositoryFolder repoFolder)
                {
                    if (!Directory.Exists(repoFolder.Path))
                    {
                        consoleWriter.AddMessageLine($"Source folder {repoFolder.Path} not found");
                    }

                    await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = Model.LocalPath, UseShellExecute = true }));
                }

                if(Model is RepositoryGit repoGit)
                {
                    await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = repoGit.Url, UseShellExecute = true }));
                }
            });
        }

        [RelayCommand]
        private void OpenSynchronizedFolder()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                if (!Directory.Exists(Model.LocalPath))
                {
                    consoleWriter.AddMessageLine($"Synchronized folder {Model.LocalPath} not found");
                }

                await Task.Run(() => Process.Start("explorer.exe", Model.LocalPath));
            });
        }

        [RelayCommand]
        private void GetReleasesData()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    await repository.FillReleasesAsync();
                    eventBroker.NotifyRepositoryViewModelReleaseDataUpdated(this);
                    consoleWriter.AddMessageLine("Releases data got successfully", "green");
                    if (repository.UseDownloadedReleases)
                    {
                        consoleWriter.AddMessageLine($"WARNING: Releases data got from downloaded releases", "orange");
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            });
        }

        [RelayCommand]
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

        [RelayCommand]
        private void OpenForm()
        {
            eventBroker.RequestOpenRepositoryForm(this, RepositoryFormMode.Edit);
        }

        [RelayCommand]
        private void Delete()
        {
            eventBroker.NotifyRepositoryViewModelDeleted(this);
        }
    }
}
