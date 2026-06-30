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
    using BIA.ToolKit.Domain.Model;
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

        // V2.15.0 — sync status surfaced on the new RepositoryCardUC.
        [ObservableProperty]
        private RepositorySyncStatus syncStatus = RepositorySyncStatus.Idle;

        [ObservableProperty]
        private int versionCount;

        [ObservableProperty]
        private string latestVersion;

        [ObservableProperty]
        private string lastSyncError;

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
                    else if (IsFolderRepository)
                    {
                        // Folder repos cache each version locally; drop that cache so the next
                        // PrepareVersionFolder re-copies fresh content from the source, then
                        // re-list the available versions.
                        if (RefreshFolderCache())
                        {
                            await repository.FillReleasesAsync(ct);
                            WeakReferenceMessenger.Default.Send(new RepositoryReleaseDataUpdatedMessage(this));
                            RefreshVersionInfo();
                            consoleWriter.AddMessageLine("Folder repository synchronized", "green");
                        }
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                }
            }));
        }

        /// <summary>
        /// For a Folder repository, clears the locally cached copy of each version so that the
        /// next <c>PrepareVersionFolder</c> re-copies the (possibly updated) content from the
        /// source. No-op for non-Folder repositories. Returns <c>false</c> when the source
        /// folder is unreachable so callers can skip re-listing (and avoid wiping the cache
        /// with no way to repopulate it).
        /// </summary>
        private bool RefreshFolderCache()
        {
            if (!IsFolderRepository)
                return true;

            if (!Directory.Exists(Model.LocalPath))
            {
                consoleWriter.AddMessageLine($"Source folder {Model.LocalPath} not found — cannot refresh cache", "red");
                return false;
            }

            repository.CleanReleases();
            return true;
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
                SyncStatus = RepositorySyncStatus.Syncing;
                LastSyncError = null;
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    RefreshFolderCache();
                    await repository.FillReleasesAsync(ct);
                    WeakReferenceMessenger.Default.Send(new RepositoryReleaseDataUpdatedMessage(this));
                    consoleWriter.AddMessageLine("Releases data got successfully", "green");
                    if (repository.UseDownloadedReleases)
                    {
                        consoleWriter.AddMessageLine($"WARNING: Releases data got from downloaded releases", "orange");
                    }
                    RefreshVersionInfo();
                    SyncStatus = RepositorySyncStatus.Idle;
                }
                catch (OperationCanceledException)
                {
                    LastSyncError = "Cancelled";
                    SyncStatus = RepositorySyncStatus.Failed;
                    throw;
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                    LastSyncError = ex.Message;
                    SyncStatus = RepositorySyncStatus.Failed;
                }
            }));
        }

        /// <summary>
        /// Computes <see cref="VersionCount"/> and <see cref="LatestVersion"/> from
        /// the repository's current <c>Releases</c> collection. Releases whose name
        /// matches a leading <c>V</c> followed by a semver are preferred when picking
        /// the latest; otherwise the first release in the collection is used.
        /// </summary>
        private void RefreshVersionInfo()
        {
            var releases = repository.Releases ?? [];
            VersionCount = releases.Count;
            if (releases.Count == 0)
            {
                LatestVersion = null;
                return;
            }

            // Prefer semver-named releases when ordering; fall back to
            // alphabetical descending if no parseable name is present.
            var parsed = releases
                .Select(r => (r.Name, Parsed: TryParseVersion(r.Name)))
                .Where(x => x.Parsed != null)
                .OrderByDescending(x => x.Parsed)
                .ToList();

            LatestVersion = parsed.Count > 0
                ? parsed[0].Name
                : releases.OrderByDescending(r => r.Name).First().Name;
        }

        private static Version TryParseVersion(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string trimmed = name.StartsWith('V') || name.StartsWith('v') ? name[1..] : name;
            return Version.TryParse(trimmed, out Version v) ? v : null;
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
