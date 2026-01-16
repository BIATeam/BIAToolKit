namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Application.Messages;

    public abstract class RepositoryViewModel : ObservableObject
    {
        private readonly Repository repository;
        protected readonly GitService gitService;
        protected readonly UIEventBroker eventBroker;
        protected readonly IMessenger messenger;
        protected readonly IConsoleWriter consoleWriter;

        protected RepositoryViewModel(Repository repository, GitService gitService, IMessenger messenger, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            ArgumentNullException.ThrowIfNull(repository, nameof(repository));
            this.repository = repository;
            this.gitService = gitService;
            this.messenger = messenger;
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
            
            // IMessenger subscription
            messenger.Register<RepositoryViewModelVersionXYZChangedMessage>(this, (r, m) => 
            {
                if (m.Repository != this)
                    IsVersionXYZ = false;
            });
            
            // Legacy subscription (dual support)
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
        public ICommand SynchronizeCommand => new RelayCommand((_) => Synchronize());
        public ICommand OpenSourceCommand => new RelayCommand((_) => OpenSource());
        public ICommand OpenSynchronizedFolderCommand => new RelayCommand((_) => OpenSynchronizedFolder());
        public ICommand GetReleasesDataCommand => new RelayCommand((_) => GetReleasesData());
        public ICommand CleanReleasesCommand => new RelayCommand((_) => CleanReleases());
        public ICommand OpenFormCommand => new RelayCommand((_) =>
        {
            messenger.Send(new OpenRepositoryFormRequestMessage(this, RepositoryFormMode.Edit));
            eventBroker.RequestOpenRepositoryForm(this, RepositoryFormMode.Edit); // Legacy
        });
        public ICommand DeleteCommand => new RelayCommand((_) =>
        {
            messenger.Send(new RepositoryViewModelDeletedMessage(this));
            eventBroker.NotifyRepositoryViewModelDeleted(this); // Legacy
        });

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
                    RaisePropertyChanged(nameof(IsVersionXYZ));
                    if (value == true)
                    {
                        messenger.Send(new RepositoryViewModelVersionXYZChangedMessage(this));
                        eventBroker.NotifyViewModelVersionXYZChanged(this); // Legacy
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
                RaisePropertyChanged(nameof(UseRepository));
                messenger.Send(new RepositoriesUpdatedMessage());
                eventBroker.NotifyRepositoriesUpdated(); // Legacy
            }
        }

        private void Synchronize()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
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
            }));
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
            }); // Legacy
        }

        private void GetReleasesData()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    await repository.FillReleasesAsync();
                    messenger.Send(new RepositoryViewModelReleaseDataUpdatedMessage(this));
                    eventBroker.NotifyRepositoryViewModelReleaseDataUpdated(this); // Legacy
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
            }));
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
            }); // Legacy
        }

        private void CleanReleases()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
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
            }));
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
            }); // Legacy
        }

        private void OpenSynchronizedFolder()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                if (!Directory.Exists(Model.LocalPath))
                {
                    consoleWriter.AddMessageLine($"Synchronized folder {Model.LocalPath} not found");
                }

                await Task.Run(() => Process.Start("explorer.exe", Model.LocalPath));
            }));
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                if (!Directory.Exists(Model.LocalPath))
                {
                    consoleWriter.AddMessageLine($"Synchronized folder {Model.LocalPath} not found");
                }

                await Task.Run(() => Process.Start("explorer.exe", Model.LocalPath));
            }); // Legacy
            
        }

        private void OpenSource()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
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
            }));
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
            }); // Legacy
        }
    }
}
