namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Messages;
    using BIA.ToolKit.Domain;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;

    public abstract partial class RepositoryViewModel : ObservableObject,
        IRecipient<RepositoryVersionXYZChangedMessage>
    {
        private readonly Repository repository;
        protected readonly GitService gitService;
        protected readonly IConsoleWriter consoleWriter;

        protected RepositoryViewModel(Repository repository, GitService gitService, IConsoleWriter consoleWriter)
        {
            ArgumentNullException.ThrowIfNull(repository, nameof(repository));
            this.repository = repository;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(RepositoryVersionXYZChangedMessage message)
        {
            if (message.Repository == this)
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
        public bool CanBeVersionXYZ => repository is RepositoryGit;

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
                        WeakReferenceMessenger.Default.Send(new RepositoryVersionXYZChangedMessage(this));
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
                WeakReferenceMessenger.Default.Send(new RepositoriesUpdatedMessage());
            }
        }

        [RelayCommand]
        private void Synchronize()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                try
                {
                    if (IsGitRepository && repository is RepositoryGit repositoryGit)
                    {
                        await gitService.Synchronize(repositoryGit, ct);
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            }));
        }

        [RelayCommand]
        private void OpenSource()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                if (Model is RepositoryFolder repoFolder)
                {
                    if (!Directory.Exists(repoFolder.Path))
                    {
                        consoleWriter.AddMessageLine($"Source folder {repoFolder.Path} not found");
                    }

                    await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = Model.LocalPath, UseShellExecute = true }), ct);
                }

                if(Model is RepositoryGit repoGit)
                {
                    await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = repoGit.Url, UseShellExecute = true }), ct);
                }
            }));
        }

        [RelayCommand]
        private void OpenSynchronizedFolder()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                if (!Directory.Exists(Model.LocalPath))
                {
                    consoleWriter.AddMessageLine($"Synchronized folder {Model.LocalPath} not found");
                }

                await Task.Run(() => Process.Start("explorer.exe", Model.LocalPath), ct);
            }));
        }

        [RelayCommand]
        private void GetReleasesData()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    await repository.FillReleasesAsync(ct);
                    WeakReferenceMessenger.Default.Send(new RepositoryReleaseDataUpdatedMessage(this));
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
        }

        [RelayCommand]
        private void CleanReleases()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                try
                {
                    consoleWriter.AddMessageLine($"Cleaning releases of repository {Name}...", "pink");
                    await Task.Run(repository.CleanReleases, ct);
                    consoleWriter.AddMessageLine($"Releases cleaned", "green");
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Failed to clean releases : {ex.Message}");
                }
            }));
        }

        [RelayCommand]
        private void OpenForm()
        {
            WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(this, RepositoryFormMode.Edit));
        }

        [RelayCommand]
        private void Delete()
        {
            WeakReferenceMessenger.Default.Send(new RepositoryDeletedMessage(this));
        }
    }
}
