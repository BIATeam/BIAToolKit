namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;

    public partial class MainViewModel : ObservableObject, IDisposable,
        IRecipient<SettingsUpdatedMessage>,
        IRecipient<RepositoryChangedMessage>,
        IRecipient<RepositoryDeletedMessage>,
        IRecipient<RepositoryAddedMessage>,
        IRecipient<RepositoriesUpdatedMessage>,
        IRecipient<ExecuteActionWithWaiterMessage>,
        IRecipient<NewVersionAvailableMessage>
    {
        private readonly Version applicationVersion;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly UpdateService updateService;
        private readonly CSharpParserService cSharpParserService;
        private readonly IDialogService dialogService;
        private bool firstTimeSettingsUpdated = true;
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;
        private bool disposed;

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public MainViewModel(
            Version applicationVersion,
            SettingsService settingsService,
            GitService gitService,
            IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            ProjectCreatorService projectCreatorService,
            UpdateService updateService,
            CSharpParserService cSharpParserService,
            IDialogService dialogService)
        {
            this.applicationVersion = applicationVersion;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            this.projectCreatorService = projectCreatorService;
            this.updateService = updateService;
            this.cSharpParserService = cSharpParserService;
            this.dialogService = dialogService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        // --- Message handlers ---

        public void Receive(SettingsUpdatedMessage message) => OnSettingsUpdated(message.Settings);
        public void Receive(RepositoryChangedMessage message) => OnRepositoryChanged(message.OldRepository, message.NewRepository);
        public void Receive(RepositoryDeletedMessage message) => OnRepositoryDeleted(message.Repository);
        public void Receive(RepositoryAddedMessage message) => OnRepositoryAdded(message.Repository);
        public void Receive(RepositoriesUpdatedMessage message) => OnRepositoriesUpdated();
        public async void Receive(ExecuteActionWithWaiterMessage message) => await ExecuteWithBusyAsync(message.Action);
        public async void Receive(NewVersionAvailableMessage message) => await OnNewVersionAvailable();

        // --- IsBusy / Waiter ---

        [ObservableProperty]
        private bool isBusy;

        private CancellationTokenSource currentTokenSource;

        public async Task ExecuteWithBusyAsync(Func<CancellationToken, Task> task)
        {
            await semaphore.WaitAsync();
            currentTokenSource = new CancellationTokenSource();
            try
            {
                IsBusy = true;
                await task(currentTokenSource.Token);
            }
            finally
            {
                IsBusy = false;
                currentTokenSource?.Dispose();
                currentTokenSource = null;
                semaphore.Release();
            }
        }

        [RelayCommand]
        private void StopAction()
        {
            currentTokenSource?.Cancel();
        }

        // --- Initialization ---

        /// <summary>
        /// Called by the host (App/MainWindow) after the window is shown.
        /// Receives the raw settings read from Properties.Settings.Default.
        /// </summary>
        public async Task InitAsync(BIATKSettings settings)
        {
            await ExecuteWithBusyAsync(async (ct) =>
            {
                settings.InitRepositoriesInterfaces();
                await GetReleasesData(settings, ct: ct);
                settingsService.Init(settings);

                updateService.SetAppVersion(applicationVersion);

                if (settings.AutoUpdate)
                {
                    await updateService.CheckForUpdatesAsync(ct);
                }

                await Task.Run(() => cSharpParserService.RegisterMSBuild(consoleWriter), ct);
            });
        }

        public async Task GetReleasesData(BIATKSettings settings, bool syncBefore = false, CancellationToken ct = default)
        {
            IEnumerable<Task> fillReleasesTasks = settings.TemplateRepositories
                .Concat(settings.CompanyFilesRepositories)
                .Where(r => r.UseRepository)
                .Select(async (r) =>
                {
                    if (syncBefore)
                    {
                        try
                        {
                            if (r is IRepositoryGit repoGit)
                            {
                                consoleWriter.AddMessageLine($"Synchronizing repository {r.Name}...", "pink");
                                await gitService.Synchronize(repoGit, ct);
                                consoleWriter.AddMessageLine($"Synchronized successfully of repository {r.Name}", "green");
                            }
                        }
                        catch (Exception ex)
                        {
                            consoleWriter.AddMessageLine($"Error while synchronizing repository {r.Name} : {ex.Message}", "red");
                        }
                    }

                    try
                    {
                        consoleWriter.AddMessageLine($"Getting releases data for repository {r.Name}...", "pink");
                        await r.FillReleasesAsync(ct);
                        consoleWriter.AddMessageLine($"Releases data got successfully for repository {r.Name}", "green");
                        if (r.UseDownloadedReleases)
                        {
                            consoleWriter.AddMessageLine($"WARNING: Releases data got from downloaded releases for repository {r.Name}", "orange");
                        }
                    }
                    catch (Exception ex)
                    {
                        consoleWriter.AddMessageLine($"Error while getting releases data for repository {r.Name} : {ex.Message}", "red");
                    }
                });
            await Task.WhenAll(fillReleasesTasks);
        }

        // --- Validation ---

        public bool EnsureValidRepositoriesConfiguration()
        {
            bool templatesValid = CheckTemplateRepositories(settingsService.Settings);
            bool companyFilesValid = CheckCompanyFilesRepositories(settingsService.Settings);

            if (!templatesValid || !companyFilesValid)
            {
                WeakReferenceMessenger.Default.Send(new NavigateToConfigTabMessage());
            }

            return templatesValid && companyFilesValid;
        }

        public bool CheckTemplateRepositories(IBIATKSettings biaTKsettings)
        {
            if (!biaTKsettings.TemplateRepositories.Where(r => r.UseRepository).Any())
            {
                consoleWriter.AddMessageLine("You must use at least one Templates repository", "red");
                return false;
            }

            foreach (IRepository repository in biaTKsettings.TemplateRepositories.Where(r => r.UseRepository))
            {
                if (!repositoryService.CheckRepoFolder(repository))
                {
                    return false;
                }
            }

            IRepository repositoryVersionXYZ = biaTKsettings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
            if (repositoryVersionXYZ is not null)
            {
                if (!repositoryService.CheckRepoFolder(repositoryVersionXYZ))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckCompanyFilesRepositories(IBIATKSettings biaTKsettings)
        {
            if (biaTKsettings.UseCompanyFiles)
            {
                if (!biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository).Any())
                {
                    consoleWriter.AddMessageLine("You must use at least one Company Files repository", "red");
                    return false;
                }

                foreach (IRepository repository in biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository))
                {
                    if (!repositoryService.CheckRepoFolder(repository))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // --- Repository management ---

        private void OnRepositoryAdded(RepositoryViewModel repository)
        {
            if (waitAddTemplateRepository)
            {
                TemplateRepositories.Add(repository);
            }

            if (waitAddCompanyFilesRepository)
            {
                CompanyFilesRepositories.Add(repository);
            }

            waitAddTemplateRepository = false;
            waitAddCompanyFilesRepository = false;

            if (repository.Model.RepositoryType == RepositoryType.Git && repository.Model is IRepositoryGit repositoryGit)
            {
                WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await gitService.Synchronize(repositoryGit, ct)));
            }
        }

        private void OnRepositoryDeleted(RepositoryViewModel repository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == repository)
                {
                    TemplateRepositories.RemoveAt(i);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == repository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                }
            }

            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                consoleWriter.AddMessageLine($"Deleting repository {repository.Name}...", "pink");
                await Task.Run(repository.Model.Clean, ct);
                consoleWriter.AddMessageLine("Repository deleted", "green");
            }));
        }

        private void OnRepositoryChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == oldRepository)
                {
                    TemplateRepositories.RemoveAt(i);
                    TemplateRepositories.Insert(i, newRepository);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == oldRepository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                    CompanyFilesRepositories.Insert(i, newRepository);
                }
            }

            if (ToolkitRepository == oldRepository)
            {
                ToolkitRepository = newRepository;
                settingsService.SetToolkitRepository(ToolkitRepository.Model);
            }
        }

        private void OnRepositoriesUpdated()
        {
            WeakReferenceMessenger.Default.Send(new SettingsUpdatedMessage(settingsService.Settings));
        }

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories([.. CompanyFilesRepositories.Select(x => x.Model)]);
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories([.. TemplateRepositories.Select(x => x.Model)]);
        }

        [RelayCommand]
        private void OpenToolkitRepositorySettings() => WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(ToolkitRepository, RepositoryFormMode.Edit));

        [RelayCommand]
        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, consoleWriter), RepositoryFormMode.Create));
        }

        [RelayCommand]
        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            WeakReferenceMessenger.Default.Send(new OpenRepositoryFormMessage(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, consoleWriter), RepositoryFormMode.Create));
        }

        public ObservableCollection<RepositoryViewModel> TemplateRepositories { get; } = [];
        public ObservableCollection<RepositoryViewModel> CompanyFilesRepositories { get; } = [];

        private RepositoryViewModel toolkitRepository;
        public RepositoryViewModel ToolkitRepository
        {
            get => toolkitRepository;
            set
            {
                toolkitRepository = value;
                OnPropertyChanged(nameof(ToolkitRepository));
            }
        }

        public void UpdateRepositories(IBIATKSettings settings)
        {
            TemplateRepositories.CollectionChanged -= TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged -= CompanyFilesRepositories_CollectionChanged;

            TemplateRepositories.Clear();
            foreach (IRepository repository in settings.TemplateRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    var viewModel = new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter);
                    TemplateRepositories.Add(viewModel);
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter));
                }
            }

            CompanyFilesRepositories.Clear();
            foreach (IRepository repository in settings.CompanyFilesRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter));
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter));
                }
            }

            ToolkitRepository = settings.ToolkitRepository switch
            {
                RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, consoleWriter),
                RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, consoleWriter),
                _ => throw new NotImplementedException()
            };
            ToolkitRepository.IsVisibleCompanyName = false;
            ToolkitRepository.IsVisibleProjectName = false;

            TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
        }

        // --- Settings ---

        private void OnSettingsUpdated(IBIATKSettings settings)
        {
            if (firstTimeSettingsUpdated)
            {
                UpdateRepositories(settings);
                firstTimeSettingsUpdated = false;
            }

            OnPropertyChanged(nameof(Settings_RootProjectsPath));
            OnPropertyChanged(nameof(Settings_CreateCompanyName));
            OnPropertyChanged(nameof(Settings_UseCompanyFiles));
            OnPropertyChanged(nameof(Settings_AutoUpdate));
            OnPropertyChanged(nameof(ToolkitRepository));
        }

        public string Settings_RootProjectsPath
        {
            get { return settingsService.Settings.CreateProjectRootProjectsPath; }
            set
            {
                if (settingsService.Settings.CreateProjectRootProjectsPath != value)
                {
                    settingsService.SetCreateProjectRootProjectPath(value);
                }
            }
        }

        public string Settings_CreateCompanyName
        {
            get { return settingsService.Settings.CreateCompanyName; }
            set
            {
                if (settingsService.Settings.CreateCompanyName != value)
                {
                    settingsService.SetCreateCompanyName(value);
                }
            }
        }

        public bool Settings_UseCompanyFiles
        {
            get { return settingsService.Settings.UseCompanyFiles; }
            set
            {
                if (settingsService.Settings.UseCompanyFiles != value)
                {
                    settingsService.SetUseCompanyFiles(value);
                }
            }
        }

        public bool Settings_AutoUpdate
        {
            get => settingsService.Settings.AutoUpdate;
            set
            {
                if (settingsService.Settings.AutoUpdate != value)
                {
                    settingsService.SetAutoUpdate(value);
                }
            }
        }

        public string ApplicationVersion => $"V{applicationVersion.Major}.{applicationVersion.Minor}.{applicationVersion.Build}";

        [ObservableProperty]
        private bool updateAvailable;

        // --- Create project ---

        [ObservableProperty]
        private string createProjectName;

        /// <summary>
        /// The VersionAndOptionViewModel for the Create tab, set by the view.
        /// </summary>
        public VersionAndOptionViewModel CreateVersionAndOptionVM { get; set; }

        [RelayCommand]
        private async Task CreateProject()
        {
            if (string.IsNullOrEmpty(settingsService.Settings.CreateProjectRootProjectsPath))
            {
                dialogService.ShowMessage("Please select root path.");
                return;
            }
            if (string.IsNullOrEmpty(settingsService.Settings.CreateCompanyName))
            {
                dialogService.ShowMessage("Please select company name.");
                return;
            }
            if (string.IsNullOrEmpty(CreateProjectName))
            {
                dialogService.ShowMessage("Please select project name.");
                return;
            }
            if (CreateVersionAndOptionVM?.WorkTemplate == null)
            {
                dialogService.ShowMessage("Please select framework version.");
                return;
            }

            string projectPath = settingsService.Settings.CreateProjectRootProjectsPath + "\\" + CreateProjectName;
            if (Directory.Exists(projectPath) && !IsDirectoryEmpty(projectPath))
            {
                dialogService.ShowMessage("The project path is not empty : " + projectPath);
                return;
            }

            await ExecuteWithBusyAsync(async (ct) =>
            {
                await projectCreatorService.Create(
                    true,
                    projectPath,
                    new ProjectParameters
                    {
                        CompanyName = settingsService.Settings.CreateCompanyName,
                        ProjectName = CreateProjectName,
                        VersionAndOption = CreateVersionAndOptionVM.VersionAndOption,
                        AngularFronts = new List<string> { Constants.FolderAngular }
                    },
                    ct: ct);
            });
        }

        private static bool IsDirectoryEmpty(string path)
        {
            string[] files = Directory.GetFiles(path);
            if (files.Length != 0) return false;
            List<string> dirs = [.. Directory.GetDirectories(path)];
            if (dirs.Where(d => !d.EndsWith("\\.git")).Any()) return false;
            return true;
        }

        // --- Update ---

        private async Task OnNewVersionAvailable()
        {
            UpdateAvailable = true;
            IsCheckUpdateEnabled = false;
            await PromptAndDownloadUpdate();
        }

        [ObservableProperty]
        private bool isCheckUpdateEnabled = true;

        [RelayCommand]
        private async Task CheckForUpdate()
        {
            await ExecuteWithBusyAsync(async (ct) => await updateService.CheckForUpdatesAsync(ct));
        }

        [RelayCommand]
        private async Task PromptAndDownloadUpdate()
        {
            try
            {
                bool confirmed = dialogService.Confirm(
                    $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?",
                    "Update available");

                if (confirmed)
                {
                    await ExecuteWithBusyAsync(async (ct) => await updateService.DownloadUpdateAsync(ct));
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage($"Update failure : {ex.Message}", "Update failure");
            }
        }

        // --- Console ---

        [RelayCommand]
        private void ClearConsole() => consoleWriter.Clear();

        [RelayCommand]
        private void CopyConsoleToClipboard() => consoleWriter.CopyToClipboard();

        // --- Browse ---

        [RelayCommand]
        private void BrowseCreateRootFolder()
        {
            Settings_RootProjectsPath = dialogService.BrowseFolder(Settings_RootProjectsPath, "Choose create project root path");
        }

        // --- Import / Export config ---

        [RelayCommand]
        private async Task ImportConfig()
        {
            string configFile = dialogService.BrowseFile(string.Empty, "btksettings");
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
                return;

            BIATKSettings config = JsonConvert.DeserializeObject<BIATKSettings>(File.ReadAllText(configFile));
            config.InitRepositoriesInterfaces();

            consoleWriter.AddMessageLine($"New configuration imported from {configFile}", "yellow");

            await ExecuteWithBusyAsync(async (ct) =>
            {
                await GetReleasesData(config, true, ct);

                settingsService.SetToolkitRepository(config.ToolkitRepository);
                settingsService.SetTemplateRepositories(config.TemplateRepositories);
                settingsService.SetCompanyFilesRepositories(config.CompanyFilesRepositories);
                settingsService.SetCreateProjectRootProjectPath(config.CreateProjectRootProjectsPath);
                settingsService.SetModifyProjectRootProjectPath(config.ModifyProjectRootProjectsPath);
                settingsService.SetAutoUpdate(config.AutoUpdate);
                settingsService.SetUseCompanyFiles(config.UseCompanyFiles);

                UpdateRepositories(settingsService.Settings);
            });
        }

        [RelayCommand]
        private void ExportConfig()
        {
            string targetDirectory = dialogService.BrowseFolder(string.Empty, "Choose export folder target");
            if (string.IsNullOrWhiteSpace(targetDirectory))
                return;

            string targetFile = Path.Combine(targetDirectory, "user.btksettings");
            string settings = JsonConvert.SerializeObject(settingsService.Settings);
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            File.AppendAllText(targetFile, settings);

            consoleWriter.AddMessageLine($"Configuration exported in {targetFile}", "yellow");
        }

        // --- Tab selection ---

        [RelayCommand]
        private void OnTabSelected() => EnsureValidRepositoriesConfiguration();
    }
}
