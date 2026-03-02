namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using Newtonsoft.Json;

    public partial class MainViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly Version applicationVersion;
        private readonly IMessenger messenger;
        private readonly SettingsService settingsService;
        private readonly IGitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private readonly MainWindowHelper mainWindowHelper;
        private readonly UpdateService updateService;
        private readonly GenerateFilesService generateFilesService;
        private readonly IProjectCreatorService projectCreatorService;
        private readonly IFileDialogService fileDialogService;
        private bool firstTimeSettingsUpdated = true;
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;

        /// <summary>
        /// Reference to the VersionAndOptionViewModel used for project creation.
        /// Set by MainWindow after creating the CreateVersionAndOptionHost content.
        /// </summary>
        public VersionAndOptionViewModel CreateVersionAndOptionViewModel { get; set; }

        public MainViewModel(
            Version applicationVersion,
            IMessenger messenger,
            SettingsService settingsService,
            IGitService gitService,
            IConsoleWriter consoleWriter,
            MainWindowHelper mainWindowHelper,
            UpdateService updateService,
            GenerateFilesService generateFilesService,
            IProjectCreatorService projectCreatorService,
            IFileDialogService fileDialogService)
        {
            this.applicationVersion = applicationVersion;
            this.messenger = messenger;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.mainWindowHelper = mainWindowHelper;
            this.updateService = updateService;
            this.generateFilesService = generateFilesService;
            this.projectCreatorService = projectCreatorService;
            this.fileDialogService = fileDialogService;

            FileGeneratorFolder = Path.Combine(Path.GetTempPath(), "BIAToolKit\\");
            
            messenger.Register<SettingsUpdatedMessage>(this, (r, m) => EventBroker_OnSettingsUpdated(m.Settings));
            messenger.Register<RepositoryViewModelChangedMessage>(this, (r, m) => EventBroker_OnRepositoryChanged(m.OldRepository, m.NewRepository));
            messenger.Register<RepositoryViewModelDeletedMessage>(this, (r, m) => EventBroker_OnRepositoryViewModelDeleted(m.Repository));
            messenger.Register<RepositoryViewModelAddedMessage>(this, (r, m) => EventBroker_OnRepositoryViewModelAdded(m.Repository));
            messenger.Register<NewVersionAvailableMessage>(this, (r, m) => UpdateAvailable = true);
            
            OpenToolkitRepositorySettingsCommand = new RelayCommand(OpenToolkitRepositorySettings);
            AddTemplateRepositoryCommand = new RelayCommand(AddTemplateRepository);
            AddCompanyFilesRepositoryCommand = new RelayCommand(AddCompanyFilesRepository);
            ClearConsoleCommand = new RelayCommand(() => consoleWriter.Clear());
            CopyConsoleToClipboardCommand = new RelayCommand(() => consoleWriter.CopyToClipboard());
            CheckForUpdatesCommand = new AsyncRelayCommand(async () => await ExecuteWithWaiter(updateService.CheckForUpdatesAsync));
            UpdateCommand = new AsyncRelayCommand(OnUpdateAvailableAsync);
            ImportConfigCommand = new AsyncRelayCommand(ImportConfigAsync);
            ExportConfigCommand = new RelayCommand(ExportConfig);
            BrowseCreateProjectRootFolderCommand = new RelayCommand(BrowseCreateProjectRootFolder);
            CreateProjectCommand = new AsyncRelayCommand(async () => await ExecuteWithWaiter(CreateProjectAsync));
            BrowseFileGeneratorFolderCommand = new RelayCommand(BrowseFileGeneratorFolder);
            BrowseFileGeneratorFileCommand = new RelayCommand(BrowseFileGeneratorFile);
            GenerateFilesCommand = new RelayCommand(GenerateFiles);
        }

        public async Task InitializeAsync()
        {
            await mainWindowHelper.InitializeApplicationAsync(applicationVersion);
        }

        public void OnCreateProjectTabSelected()
        {
            // Validate template repositories configuration before allowing Create Project operations
            if (!mainWindowHelper.ValidateTemplateRepositories(settingsService.Settings))
            {
                consoleWriter.AddMessageLine("Create Project tab cannot be accessed: Template repositories are not properly configured", "red");
            }
        }

        public void OnModifyProjectTabSelected()
        {
            // Validate all repositories configuration before allowing Modify Project operations
            if (!mainWindowHelper.ValidateRepositoriesConfiguration(settingsService.Settings))
            {
                consoleWriter.AddMessageLine("Modify Project tab cannot be accessed: Repository configuration is not valid", "red");
            }
        }

        private void EventBroker_OnRepositoryViewModelAdded(RepositoryViewModel repository)
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

            if(repository.Model.RepositoryType == Domain.RepositoryType.Git && repository.Model is IRepositoryGit repositoryGit)
            {
                messenger.Send(new ExecuteActionWithWaiterMessage(async () => await gitService.Synchronize(repositoryGit)));
            }
        }

        private void EventBroker_OnRepositoryViewModelDeleted(RepositoryViewModel repository)
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

            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                consoleWriter.AddMessageLine($"Deleting repository {repository.Name}...", "pink");
                await Task.Run(repository.Model.Clean);
                consoleWriter.AddMessageLine("Repository deleted", "green");
            }));
        }

        private void EventBroker_OnRepositoryChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
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

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        private void OpenToolkitRepositorySettings()
        {
            messenger.Send(new OpenRepositoryFormRequestMessage(ToolkitRepository, RepositoryFormMode.Edit));
        }

        public IRelayCommand OpenToolkitRepositorySettingsCommand { get; }

        public IRelayCommand AddTemplateRepositoryCommand { get; }

        public IRelayCommand AddCompanyFilesRepositoryCommand { get; }

        public IRelayCommand ClearConsoleCommand { get; }

        public IRelayCommand CopyConsoleToClipboardCommand { get; }

        public IAsyncRelayCommand CheckForUpdatesCommand { get; }

        public IAsyncRelayCommand UpdateCommand { get; }

        public IAsyncRelayCommand ImportConfigCommand { get; }

        public IRelayCommand ExportConfigCommand { get; }

        public IRelayCommand BrowseCreateProjectRootFolderCommand { get; }

        public IAsyncRelayCommand CreateProjectCommand { get; }

        public IRelayCommand BrowseFileGeneratorFolderCommand { get; }

        public IRelayCommand BrowseFileGeneratorFileCommand { get; }

        public IRelayCommand GenerateFilesCommand { get; }

        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            var repo = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, messenger, consoleWriter);
            messenger.Send(new OpenRepositoryFormRequestMessage(repo, RepositoryFormMode.Create));
        }

        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            var repo = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, messenger, consoleWriter);
            messenger.Send(new OpenRepositoryFormRequestMessage(repo, RepositoryFormMode.Create));
        }

        public ObservableCollection<RepositoryViewModel> TemplateRepositories { get; } = [];
        public ObservableCollection<RepositoryViewModel> CompanyFilesRepositories { get; } = [];

        private RepositoryViewModel toolkitRepository;
        public RepositoryViewModel ToolkitRepository
        {
            get => toolkitRepository;
            set => SetProperty(ref toolkitRepository, value);
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
                    var viewModel = new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter) { CanBeVersionXYZ = true };
                    TemplateRepositories.Add(viewModel);
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter));
                }
            }

            CompanyFilesRepositories.Clear();
            foreach (var repository in settings.CompanyFilesRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter));
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter));
                }
            }

            ToolkitRepository = settings.ToolkitRepository switch
            {
                RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter),
                RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter),
                _ => throw new NotImplementedException()
            };
            ToolkitRepository.IsVisibleCompanyName = false;
            ToolkitRepository.IsVisibleProjectName = false;

            TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
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
        private bool _updateAvailable;

        [ObservableProperty]
        private string _createProjectName = string.Empty;

        [ObservableProperty]
        private string _fileGeneratorFolder;

        [ObservableProperty]
        private string _fileGeneratorFile;

        [ObservableProperty]
        private bool _isFileGeneratorGenerateEnabled;

        private Task ExecuteWithWaiter(Func<Task> action)
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(action));
            return Task.CompletedTask;
        }

        private async Task OnUpdateAvailableAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?",
                    "Update available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await ExecuteWithWaiter(updateService.DownloadUpdateAsync);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failure : {ex.Message}", "Update failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ImportConfigAsync()
        {
            var configFile = fileDialogService.BrowseFile("btksettings");
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
                return;

            var config = JsonConvert.DeserializeObject<BIATKSettings>(File.ReadAllText(configFile));
            config.InitRepositoriesInterfaces();

            consoleWriter.AddMessageLine($"New configuration imported from {configFile}", "yellow");

            await ExecuteWithWaiter(async () =>
            {
                await mainWindowHelper.FetchReleaseDataAsync(config, syncBefore: true);

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

        private void ExportConfig()
        {
            var targetDirectory = fileDialogService.BrowseFolder(string.Empty, "Choose export folder target");
            if (string.IsNullOrWhiteSpace(targetDirectory))
                return;

            var targetFile = Path.Combine(targetDirectory, "user.btksettings");
            var settings = JsonConvert.SerializeObject(settingsService.Settings);
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            File.AppendAllText(targetFile, settings);

            consoleWriter.AddMessageLine($"Configuration exported in {targetFile}", "yellow");
        }

        private void BrowseCreateProjectRootFolder()
        {
            var folder = fileDialogService.BrowseFolder(Settings_RootProjectsPath, "Select projects parent folder");
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Settings_RootProjectsPath = folder;
            }
        }

        private async Task CreateProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(Settings_RootProjectsPath))
            {
                MessageBox.Show("Please select root path.", "Create Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(settingsService.Settings.CreateCompanyName))
            {
                MessageBox.Show("Please select company name.", "Create Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CreateProjectName))
            {
                MessageBox.Show("Please select project name.", "Create Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CreateVersionAndOptionViewModel?.VersionAndOption?.WorkTemplate == null)
            {
                MessageBox.Show("Please select framework version.", "Create Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var projectPath = Path.Combine(Settings_RootProjectsPath, CreateProjectName);
            if (Directory.Exists(projectPath) && !fileDialogService.IsDirectoryEmpty(projectPath))
            {
                MessageBox.Show("The project path is not empty : " + projectPath, "Create Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var projectParameters = new ProjectParameters
            {
                CompanyName = settingsService.Settings.CreateCompanyName,
                ProjectName = CreateProjectName,
                VersionAndOption = CreateVersionAndOptionViewModel.VersionAndOption,
                AngularFronts = new System.Collections.Generic.List<string> { Constants.FolderAngular },
            };

            await projectCreatorService.Create(true, projectPath, projectParameters);
        }

        private void BrowseFileGeneratorFolder()
        {
            var folder = fileDialogService.BrowseFolder(FileGeneratorFolder, "Select folder to save generated files");
            if (!string.IsNullOrWhiteSpace(folder))
            {
                FileGeneratorFolder = folder;
            }
        }

        private void BrowseFileGeneratorFile()
        {
            var file = fileDialogService.BrowseFile("cs");
            if (!string.IsNullOrWhiteSpace(file))
            {
                FileGeneratorFile = file;
                IsFileGeneratorGenerateEnabled = !string.IsNullOrWhiteSpace(FileGeneratorFolder);
            }
        }

        private void GenerateFiles()
        {
            if (string.IsNullOrEmpty(FileGeneratorFolder))
            {
                MessageBox.Show("Select the folder to save the files", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(FileGeneratorFile))
            {
                MessageBox.Show("Select the class file", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string resultFolder = string.Empty;
            generateFilesService.GenerateFiles(FileGeneratorFile, FileGeneratorFolder, ref resultFolder);
            if (!string.IsNullOrEmpty(resultFolder))
            {
                Process.Start(new ProcessStartInfo(resultFolder) { UseShellExecute = true });
            }
        }
    }
}
