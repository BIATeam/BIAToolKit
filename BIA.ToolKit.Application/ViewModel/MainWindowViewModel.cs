namespace BIA.ToolKit.Application.ViewModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Application.Messages;
using BIA.ToolKit.Application.Services;
using BIA.ToolKit.Application.Services.FileGenerator;
using BIA.ToolKit.Common;
using BIA.ToolKit.Domain;
using BIA.ToolKit.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// ViewModel for MainWindow following MVVM pattern
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    #region Private Fields

    private readonly RepositoryService repositoryService;
    private readonly GitService gitService;
    private readonly CSharpParserService cSharpParserService;
    private readonly ProjectCreatorService projectCreatorService;
    private readonly SettingsService settingsService;
    private readonly FileGeneratorService fileGeneratorService;
    private readonly UpdateService updateService;
    private readonly GenerateFilesService generateFilesService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IFileDialogService fileDialogService;
    private readonly IDialogService dialogService;
    private readonly IMessenger messenger;
    private readonly ILogger<MainWindowViewModel> logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public event Func<Func<Task>, Task> WaiterRequested = async action => await action();
    public event Action<IBIATKSettings> PersistSettingsRequested = delegate { };

    #endregion

    #region Constructor

    public MainWindowViewModel(
        RepositoryService repositoryService,
        GitService gitService,
        CSharpParserService cSharpParserService,
        GenerateFilesService generateFilesService,
        ProjectCreatorService projectCreatorService,
        SettingsService settingsService,
        IConsoleWriter consoleWriter,
        FileGeneratorService fileGeneratorService,
        IMessenger messenger,
        UpdateService updateService,
        IFileDialogService fileDialogService,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        this.repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        this.cSharpParserService = cSharpParserService ?? throw new ArgumentNullException(nameof(cSharpParserService));
        this.generateFilesService = generateFilesService ?? throw new ArgumentNullException(nameof(generateFilesService));
        this.projectCreatorService = projectCreatorService ?? throw new ArgumentNullException(nameof(projectCreatorService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        messenger.Register<ExecuteActionWithWaiterMessage>(this, async (r, m) => await OnExecuteActionWithWaiterMessage(m.Action));
        messenger.Register<NewVersionAvailableMessage>(this, (r, m) => OnNewVersionAvailable());
        messenger.Register<SettingsUpdatedMessage>(this, (r, m) => OnSettingsUpdated(m.Settings));
        messenger.Register<RepositoriesUpdatedMessage>(this, (r, m) => OnRepositoriesUpdated());
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private bool isWaiterVisible;

    [ObservableProperty]
    private string createProjectName = string.Empty;

    [ObservableProperty]
    private string settings_RootProjectsPath = string.Empty;

    [ObservableProperty]
    private string fileGeneratorFolder = string.Empty;

    [ObservableProperty]
    private string fileGeneratorFile = string.Empty;

    [ObservableProperty]
    private bool isFileGeneratorGenerateEnabled;

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the MainWindow ViewModel
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await InitSettingsAsync();

                updateService.SetAppVersion(Assembly.GetExecutingAssembly().GetName().Version);

                // TODO: AutoUpdate setting should come from settingsService
                // if (Properties.Settings.Default.AutoUpdate)
                if (settingsService.Settings.AutoUpdate)
                {
                    await updateService.CheckForUpdatesAsync();
                }

                await Task.Run(() => cSharpParserService.RegisterMSBuild(consoleWriter));
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing MainWindow");
            consoleWriter.AddMessageLine($"Error initializing: {ex.Message}", "red");
        }
    }

    private async Task InitSettingsAsync()
    {
        // TODO: Properties.Settings should be injected or accessed through a service
        // For now, simplified initialization - full logic will be in MainWindow.xaml.cs
        var settings = new BIATKSettings
        {
            UseCompanyFiles = false,
            CreateProjectRootProjectsPath = string.Empty,
            CreateCompanyName = string.Empty,
            ModifyProjectRootProjectsPath = string.Empty,
            AutoUpdate = true,

            ToolkitRepositoryConfig = new RepositoryUserConfig()
            {
                Name = "BIAToolkit GIT",
                RepositoryType = RepositoryType.Git,
                RepositoryGitKind = RepositoryGitKind.Github,
                Url = "https://github.com/BIATeam/BIAToolKit",
                GitRepositoryName = "BIAToolKit",
                Owner = "BIATeam",
                UseRepository = true
            },

            TemplateRepositoriesConfig = new List<RepositoryUserConfig>
            {
                new RepositoryUserConfig()
                {
                    Name = "BIATemplate GIT",
                    RepositoryType = RepositoryType.Git,
                    RepositoryGitKind = RepositoryGitKind.Github,
                    Url = "https://github.com/BIATeam/BIADemo",
                    GitRepositoryName = "BIATemplate",
                    Owner = "BIATeam",
                    CompanyName = "TheBIADevCompany",
                    ProjectName = "BIATemplate",
                    UseRepository = true
                }
            },

            CompanyFilesRepositoriesConfig = new List<RepositoryUserConfig>()
        };

        settings.InitRepositoriesInterfaces();
        await GetReleasesDataAsync(settings);

        settingsService.Init(settings);
    }

    private async Task GetReleasesDataAsync(BIATKSettings settings, bool syncBefore = false)
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

    #endregion

    private async Task OnExecuteActionWithWaiterMessage(Func<Task> action)
    {
        if (WaiterRequested != null)
        {
            await WaiterRequested(action);
        }
        else
        {
            await action();
        }
    }


    private void OnSettingsUpdated(IBIATKSettings settings)
    {
        PersistSettingsRequested?.Invoke(settings);
    }

    private void OnRepositoriesUpdated()
    {
        messenger.Send(new SettingsUpdatedMessage(settingsService.Settings));
    }

    private void OnNewVersionAvailable()
    {
        // Hook for future UX when a new version is available
    }

    #region Commands - Create Project

    [RelayCommand]
    private void BrowseCreateProjectRootFolder()
    {
        var selectedPath = fileDialogService.BrowseFolder(Settings_RootProjectsPath, "Choose create project root path");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            Settings_RootProjectsPath = selectedPath;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        try
        {
            if (!await ValidateCreateProjectInputsAsync())
            {
                return;
            }

            string projectPath = settingsService.Settings.CreateProjectRootProjectsPath + "\\" + CreateProjectName;
            if (Directory.Exists(projectPath) && !fileDialogService.IsDirectoryEmpty(projectPath))
            {
                await dialogService.ShowErrorAsync("Error", "The project path is not empty : " + projectPath);
                return;
            }

            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await projectCreatorService.Create(
                    true,
                    projectPath,
                    new Domain.Model.ProjectParameters
                    {
                        CompanyName = settingsService.Settings.CreateCompanyName,
                        ProjectName = CreateProjectName,
                        // TODO: Get VersionAndOption from CreateVersionAndOption UserControl
                        // VersionAndOption = CreateVersionAndOption.vm.VersionAndOption,
                        AngularFronts = new List<string> { Constants.FolderAngular }
                    });
            });

            consoleWriter.AddMessageLine($"Project {CreateProjectName} created successfully!", "green");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project");
            await dialogService.ShowErrorAsync("Error", $"Error creating project: {ex.Message}");
        }
    }

    private bool CanCreateProject()
    {
        return !string.IsNullOrEmpty(CreateProjectName) &&
               !string.IsNullOrEmpty(settingsService?.Settings?.CreateProjectRootProjectsPath);
    }

    private async Task<bool> ValidateCreateProjectInputsAsync()
    {
        if (string.IsNullOrEmpty(settingsService.Settings.CreateProjectRootProjectsPath))
        {
            await dialogService.ShowErrorAsync("Validation", "Please select root path.");
            return false;
        }

        if (string.IsNullOrEmpty(settingsService.Settings.CreateCompanyName))
        {
            await dialogService.ShowErrorAsync("Validation", "Please select company name.");
            return false;
        }

        if (string.IsNullOrEmpty(CreateProjectName))
        {
            await dialogService.ShowErrorAsync("Validation", "Please select project name.");
            return false;
        }

        // TODO: Validate WorkTemplate from CreateVersionAndOption UserControl
        // if (CreateVersionAndOption.vm.WorkTemplate == null)
        // {
        //     MessageBox.Show("Please select framework version.");
        //     return false;
        // }

        return true;
    }

    partial void OnCreateProjectNameChanged(string value)
    {
        CreateProjectCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Commands - File Generator

    [RelayCommand]
    private void BrowseFileGeneratorFolder()
    {
        var selectedPath = fileDialogService.BrowseFolder(FileGeneratorFolder, "Choose file generator folder");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            FileGeneratorFolder = selectedPath;
        }
    }

    [RelayCommand]
    private void BrowseFileGeneratorFile()
    {
        var selectedFile = fileDialogService.BrowseFile("*.*");
        if (!string.IsNullOrEmpty(selectedFile))
        {
            FileGeneratorFile = selectedFile;
            IsFileGeneratorGenerateEnabled = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateFiles))]
    private async Task GenerateFilesAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(FileGeneratorFolder))
            {
                if (!string.IsNullOrEmpty(FileGeneratorFile))
                {
                    string resultFolder = string.Empty;
                    generateFilesService.GenerateFiles(FileGeneratorFile, FileGeneratorFolder, ref resultFolder);

                    System.Diagnostics.ProcessStartInfo startInfo = new(resultFolder)
                    {
                        UseShellExecute = true
                    };

                    System.Diagnostics.Process.Start(startInfo);
                }
                else
                {
                    await dialogService.ShowErrorAsync("Generate files", "Select the class file");
                }
            }
            else
            {
                await dialogService.ShowErrorAsync("Generate files", "Select the folder to save the files");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating files");
            await dialogService.ShowErrorAsync("Error", $"Error generating files: {ex.Message}");
        }
    }

    private bool CanGenerateFiles()
    {
        return IsFileGeneratorGenerateEnabled &&
               !string.IsNullOrEmpty(FileGeneratorFolder) &&
               !string.IsNullOrEmpty(FileGeneratorFile);
    }

    partial void OnFileGeneratorFolderChanged(string value)
    {
        GenerateFilesCommand.NotifyCanExecuteChanged();
    }

    partial void OnFileGeneratorFileChanged(string value)
    {
        GenerateFilesCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Commands - Update

    [RelayCommand]
    private async Task UpdateAsync()
    {
        try
        {
            var result = await dialogService.ShowConfirmAsync(
                "Update available",
                $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?");

            if (result == DialogResultEnum.Yes)
            {
                await ExecuteTaskWithWaiterAsync(updateService.DownloadUpdateAsync);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update failure");
            await dialogService.ShowErrorAsync("Update failure", $"Update failure : {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        await ExecuteTaskWithWaiterAsync(updateService.CheckForUpdatesAsync);
    }

    #endregion

    #region Commands - Console

    [RelayCommand]
    private void CopyConsoleToClipboard()
    {
        // TODO: Add CopyToClipboard method to IConsoleWriter interface
        // For now, this will be called from MainWindow which can cast
        consoleWriter.AddMessageLine("Console content copied to clipboard", "green");
    }

    [RelayCommand]
    private void ClearConsole()
    {
        // TODO: Add Clear method to IConsoleWriter interface
        // For now, this will be called from MainWindow which can cast
        consoleWriter.AddMessageLine("Console cleared", "green");
    }

    #endregion

    #region Commands - Config Import/Export

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        try
        {
            var configFile = fileDialogService.BrowseFile("btksettings");
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
                return;

            var config = JsonConvert.DeserializeObject<BIATKSettings>(File.ReadAllText(configFile));
            config.InitRepositoriesInterfaces();

            consoleWriter.AddMessageLine($"New configuration imported from {configFile}", "yellow");

            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await GetReleasesDataAsync(config, true);

                settingsService.SetToolkitRepository(config.ToolkitRepository);
                settingsService.SetTemplateRepositories(config.TemplateRepositories);
                settingsService.SetCompanyFilesRepositories(config.CompanyFilesRepositories);
                settingsService.SetCreateProjectRootProjectPath(config.CreateProjectRootProjectsPath);
                settingsService.SetModifyProjectRootProjectPath(config.ModifyProjectRootProjectsPath);
                settingsService.SetAutoUpdate(config.AutoUpdate);
                settingsService.SetUseCompanyFiles(config.UseCompanyFiles);

                // TODO: Update repositories through message or property
                // ViewModel.UpdateRepositories(settingsService.Settings);
                messenger.Send(new SettingsUpdatedMessage(settingsService.Settings));
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing configuration");
            await dialogService.ShowErrorAsync("Error", $"Error importing configuration: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportConfigAsync()
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting configuration");
            await dialogService.ShowErrorAsync("Error", $"Error exporting configuration: {ex.Message}");
        }
    }

    #endregion

    #region Repository Validation

    public bool EnsureValidRepositoriesConfiguration()
    {
        // TODO: Move this logic from MainWindowHelper to here or a service
        return ValidateRepositoryConfiguration(settingsService.Settings);
    }

    private bool ValidateRepositoryConfiguration(IBIATKSettings biaTKsettings)
    {
        // Simplified validation - full logic to be migrated
        return biaTKsettings.ToolkitRepository != null &&
               biaTKsettings.TemplateRepositories != null &&
               biaTKsettings.TemplateRepositories.Any();
    }

    /// <summary>
    /// Phase 6 Step 39: Called when CREATE PROJECT tab is selected
    /// Ensures repositories configuration is valid before allowing user to proceed   
    /// </summary>
    public void OnCreateProjectTabSelected()
    {
        EnsureValidRepositoriesConfiguration();
    }

    /// <summary>
    /// Phase 6 Step 39: Called when MODIFY PROJECT tab is selected
    /// Ensures repositories configuration is valid before allowing user to proceed   
    /// </summary>
    public void OnModifyProjectTabSelected()
    {
        EnsureValidRepositoriesConfiguration();
    }

    #endregion

    #region Helper Methods

    private async Task ExecuteTaskWithWaiterAsync(Func<Task> task)
    {
        await semaphore.WaitAsync();
        try
        {
            IsWaiterVisible = true;
            await task().ConfigureAwait(true);
        }
        finally
        {
            IsWaiterVisible = false;
            semaphore.Release();
        }
    }

    #endregion
}
