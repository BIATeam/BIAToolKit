namespace BIA.ToolKit.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;
    using Octokit;

    public class MainViewModel : ViewModelBase
    {
        private readonly Version applicationVersion;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private readonly UpdateService updateService;
        private readonly CSharpParserService cSharpParserService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly GenerateFilesService generateFilesService;
        private readonly RepositoryService repositoryService;
        private bool firstTimeSettingsUpdated = true;
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;

        public MainViewModel(Version applicationVersion, IMessenger messenger, SettingsService settingsService, GitService gitService, IConsoleWriter consoleWriter,
            UpdateService updateService, CSharpParserService cSharpParserService, ProjectCreatorService projectCreatorService, GenerateFilesService generateFilesService, RepositoryService repositoryService)
            : base(messenger)
        {
            this.applicationVersion = applicationVersion;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.updateService = updateService;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            this.generateFilesService = generateFilesService;
            this.repositoryService = repositoryService;
        }

        /// <summary>Gets or sets the VersionAndOption ViewModel used for project creation.</summary>
        public VersionAndOptionViewModel CreateVersionAndOptionVm { get; set; }

        /// <summary>Gets or sets an action invoked to navigate to the Settings tab when repository configuration is invalid.</summary>
        public Action NavigateToSettingsTab { get; set; }

        /// <inheritdoc/>
        public override void Initialize()
        {
            Messenger.Subscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
            Messenger.Subscribe<NewVersionAvailableMessage>(OnNewVersionAvailable);
            Messenger.Subscribe<RepositoriesUpdatedMessage>(OnRepositoriesUpdated);
            Messenger.Subscribe<RepositoryViewModelChangedMessage>(OnRepositoryViewModelChanged);
            Messenger.Subscribe<RepositoryViewModelDeletedMessage>(OnRepositoryViewModelDeleted);
            Messenger.Subscribe<RepositoryViewModelAddedMessage>(OnRepositoryViewModelAdded);
        }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            Messenger.Unsubscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
            Messenger.Unsubscribe<NewVersionAvailableMessage>(OnNewVersionAvailable);
            Messenger.Unsubscribe<RepositoriesUpdatedMessage>(OnRepositoriesUpdated);
            Messenger.Unsubscribe<RepositoryViewModelChangedMessage>(OnRepositoryViewModelChanged);
            Messenger.Unsubscribe<RepositoryViewModelDeletedMessage>(OnRepositoryViewModelDeleted);
            Messenger.Unsubscribe<RepositoryViewModelAddedMessage>(OnRepositoryViewModelAdded);
        }

        private void OnSettingsUpdated(SettingsUpdatedMessage message)
        {
            if (firstTimeSettingsUpdated)
            {
                UpdateRepositories(message.Settings);
                firstTimeSettingsUpdated = false;
            }

            RaisePropertyChanged(nameof(Settings_RootProjectsPath));
            RaisePropertyChanged(nameof(Settings_CreateCompanyName));
            RaisePropertyChanged(nameof(Settings_UseCompanyFiles));
            RaisePropertyChanged(nameof(Settings_AutoUpdate));
            RaisePropertyChanged(nameof(ToolkitRepository));
        }

        private void OnNewVersionAvailable(NewVersionAvailableMessage message)
        {
            UpdateAvailable = true;
        }

        private void OnRepositoriesUpdated(RepositoriesUpdatedMessage message)
        {
            settingsService.NotifySettingsUpdated();
        }

        private void OnRepositoryViewModelAdded(RepositoryViewModelAddedMessage message)
        {
            var repository = message.Repository;
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

            if (repository.Model.RepositoryType == Domain.RepositoryType.Git && repository.Model is IRepositoryGit repositoryGit)
            {
                Messenger.Send(new ExecuteWithWaiterMessage { Task = async () => await gitService.Synchronize(repositoryGit) });
            }
        }

        private void OnRepositoryViewModelDeleted(RepositoryViewModelDeletedMessage message)
        {
            var repository = message.Repository;
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

            Messenger.Send(new ExecuteWithWaiterMessage
            {
                Task = async () =>
                {
                    consoleWriter.AddMessageLine($"Deleting repository {repository.Name}...", "pink");
                    await Task.Run(repository.Model.Clean);
                    consoleWriter.AddMessageLine("Repository deleted", "green");
                }
            });
        }

        private void OnRepositoryViewModelChanged(RepositoryViewModelChangedMessage message)
        {
            var (oldRepository, newRepository) = (message.OldRepository, message.NewRepository);
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

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        public ICommand OpenToolkitRepositorySettingsCommand => new RelayCommand((_) => Messenger.Send(new OpenRepositoryFormRequestMessage { Repository = ToolkitRepository, Mode = RepositoryFormMode.Edit }));

        public ICommand AddTemplateRepositoryCommand => new RelayCommand((_) => AddTemplateRepository());

        public ICommand AddCompanyFilesRepositoryCommand => new RelayCommand((_) => AddCompanyFilesRepository());

        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            Messenger.Send(new OpenRepositoryFormRequestMessage { Repository = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, Messenger, consoleWriter), Mode = RepositoryFormMode.Create });
        }

        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            Messenger.Send(new OpenRepositoryFormRequestMessage { Repository = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, Messenger, consoleWriter), Mode = RepositoryFormMode.Create });
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
                RaisePropertyChanged(nameof(ToolkitRepository));
            }
        }

        public void UpdateRepositories(IBIATKSettings settings)
        {
            TemplateRepositories.CollectionChanged -= TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged -= CompanyFilesRepositories_CollectionChanged;

            TemplateRepositories.Clear();
            foreach (var repository in settings.TemplateRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    var viewModel = new RepositoryGitViewModel(repositoryGit, gitService, Messenger, consoleWriter) { CanBeVersionXYZ = true };
                    TemplateRepositories.Add(viewModel);
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, Messenger, consoleWriter));
                }
            }

            CompanyFilesRepositories.Clear();
            foreach (var repository in settings.CompanyFilesRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, Messenger, consoleWriter));
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, Messenger, consoleWriter));
                }
            }

            ToolkitRepository = settings.ToolkitRepository switch
            {
                RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, Messenger, consoleWriter),
                RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, Messenger, consoleWriter),
                _ => throw new NotImplementedException()
            };
            ToolkitRepository.IsVisibleCompanyName = false;
            ToolkitRepository.IsVisibleProjectName = false;

            TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
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

        private bool _updateAvailable;
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                _updateAvailable = value;
                RaisePropertyChanged(nameof(UpdateAvailable));
            }
        }

        /// <summary>Initializes the application: loads releases data, inits settings, checks for updates and registers MSBuild.</summary>
        public async Task InitAsync(BIATKSettings settings)
        {
            await GetReleasesData(settings);
            settingsService.Init(settings);
            updateService.SetAppVersion(applicationVersion);
            if (settingsService.Settings.AutoUpdate)
            {
                await updateService.CheckForUpdatesAsync();
            }
            await Task.Run(() => cSharpParserService.RegisterMSBuild(consoleWriter));
        }

        /// <summary>Gets releases data for all active repositories, optionally synchronizing git repos first.</summary>
        public async Task GetReleasesData(BIATKSettings settings, bool syncBefore = false)
        {
            var fillReleasesTasks = settings.TemplateRepositories
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
                                await gitService.Synchronize(repoGit);
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
                        await r.FillReleasesAsync();
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

        /// <summary>Validates that at least one usable template repository exists and its folder is accessible.</summary>
        public bool CheckTemplateRepositories(IBIATKSettings biaTKsettings)
        {
            if (!biaTKsettings.TemplateRepositories.Where(r => r.UseRepository).Any())
            {
                consoleWriter.AddMessageLine("You must use at least one Templates repository", "red");
                return false;
            }

            foreach (var repository in biaTKsettings.TemplateRepositories.Where(r => r.UseRepository))
            {
                if (!repositoryService.CheckRepoFolder(repository))
                {
                    return false;
                }
            }

            var repositoryVersionXYZ = biaTKsettings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
            if (repositoryVersionXYZ is not null)
            {
                if (!repositoryService.CheckRepoFolder(repositoryVersionXYZ))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates that at least one usable company files repository exists and its folder is accessible (if company files are enabled).</summary>
        public bool CheckCompanyFilesRepositories(IBIATKSettings biaTKsettings)
        {
            if (biaTKsettings.UseCompanyFiles)
            {
                if (!biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository).Any())
                {
                    consoleWriter.AddMessageLine("You must use at least one Company Files repository", "red");
                    return false;
                }

                foreach (var repository in biaTKsettings.CompanyFilesRepositories.Where(r => r.UseRepository))
                {
                    if (!repositoryService.CheckRepoFolder(repository))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Checks both template and company files repository configurations; navigates to Settings tab if invalid.</summary>
        public bool EnsureValidRepositoriesConfiguration()
        {
            var templatesValid = CheckTemplateRepositories(settingsService.Settings);
            var companyFilesValid = CheckCompanyFilesRepositories(settingsService.Settings);
            if (!templatesValid || !companyFilesValid)
            {
                NavigateToSettingsTab?.Invoke();
            }
            return templatesValid && companyFilesValid;
        }

        /// <summary>Creates a new project from the Create tab.</summary>
        public async Task CreateProjectAsync(string projectName)
        {
            if (string.IsNullOrEmpty(settingsService.Settings.CreateProjectRootProjectsPath))
            {
                consoleWriter.AddMessageLine("Please select root path.", "red");
                return;
            }
            if (string.IsNullOrEmpty(settingsService.Settings.CreateCompanyName))
            {
                consoleWriter.AddMessageLine("Please select company name.", "red");
                return;
            }
            if (string.IsNullOrEmpty(projectName))
            {
                consoleWriter.AddMessageLine("Please select project name.", "red");
                return;
            }
            if (CreateVersionAndOptionVm?.WorkTemplate == null)
            {
                consoleWriter.AddMessageLine("Please select framework version.", "red");
                return;
            }

            string projectPath = Path.Combine(settingsService.Settings.CreateProjectRootProjectsPath, projectName);
            if (Directory.Exists(projectPath) && !DirectoryHelper.IsDirectoryEmpty(projectPath))
            {
                consoleWriter.AddMessageLine("The project path is not empty : " + projectPath, "red");
                return;
            }

            await projectCreatorService.Create(
                true,
                projectPath,
                new ProjectParameters
                {
                    CompanyName = settingsService.Settings.CreateCompanyName,
                    ProjectName = projectName,
                    VersionAndOption = CreateVersionAndOptionVm.VersionAndOption,
                    AngularFronts = new List<string> { Constants.FolderAngular }
                });
        }

        /// <summary>Imports application configuration from a .btksettings file.</summary>
        public async Task ImportConfigAsync(string configFile)
        {
            var config = JsonConvert.DeserializeObject<BIATKSettings>(File.ReadAllText(configFile));
            config.InitRepositoriesInterfaces();

            consoleWriter.AddMessageLine($"New configuration imported from {configFile}", "yellow");

            await GetReleasesData(config, true);

            settingsService.SetToolkitRepository(config.ToolkitRepository);
            settingsService.SetTemplateRepositories(config.TemplateRepositories);
            settingsService.SetCompanyFilesRepositories(config.CompanyFilesRepositories);
            settingsService.SetCreateProjectRootProjectPath(config.CreateProjectRootProjectsPath);
            settingsService.SetModifyProjectRootProjectPath(config.ModifyProjectRootProjectsPath);
            settingsService.SetAutoUpdate(config.AutoUpdate);
            settingsService.SetUseCompanyFiles(config.UseCompanyFiles);

            UpdateRepositories(settingsService.Settings);
        }

        /// <summary>Exports the current application configuration to the specified file path.</summary>
        public void ExportConfig(string targetFile)
        {
            var settings = JsonConvert.SerializeObject(settingsService.Settings);
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            File.AppendAllText(targetFile, settings);

            consoleWriter.AddMessageLine($"Configuration exported in {targetFile}", "yellow");
        }

        /// <summary>Generates files from a template and returns the output folder path.</summary>
        public string GenerateFilesAndGetResultFolder(string templateFile, string outputFolder)
        {
            string resultFolder = string.Empty;
            generateFilesService.GenerateFiles(templateFile, outputFolder, ref resultFolder);
            return resultFolder;
        }

        /// <summary>Triggers an on-demand check for application updates.</summary>
        public Task CheckForUpdatesAsync() => updateService.CheckForUpdatesAsync();
    }
}
