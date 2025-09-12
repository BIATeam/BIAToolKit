namespace BIA.ToolKit
{
    using BIA.ToolKit.Helper;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;
    using BIA.ToolKit.UserControls;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Dialogs;
    using System.Text.Json;
    using BIA.ToolKit.Common;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using System.Reflection;
    using System.Threading;
    using BIA.ToolKit.Domain;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private readonly RepositoryService repositoryService;
        private readonly GitService gitService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly SettingsService settingsService;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly UIEventBroker uiEventBroker;
        private readonly UpdateService updateService;
        private readonly GenerateFilesService generateFilesService;
        private readonly ConsoleWriter consoleWriter;
        private bool isCreateTabInitialized = false;
        private bool isModifyTabInitialized = false;

        public MainWindow(RepositoryService repositoryService, GitService gitService, CSharpParserService cSharpParserService, GenerateFilesService genFilesService,
            ProjectCreatorService projectCreatorService, ZipParserService zipParserService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, FeatureSettingService featureSettingService, FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker, UpdateService updateService)
        {

            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.repositoryService = repositoryService;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;
            this.settingsService = settingsService;
            this.fileGeneratorService = fileGeneratorService;
            this.uiEventBroker = uiEventBroker;
            this.updateService = updateService;
            this.generateFilesService = genFilesService;

            uiEventBroker.OnExecuteActionWithWaiterAsyncRequest += async (action) => await ExecuteTaskWithWaiterAsync(action);

            InitializeComponent();

            CreateVersionAndOption.Inject(this.repositoryService, gitService, consoleWriter, featureSettingService, settingsService, uiEventBroker);
            ModifyProject.Inject(this.repositoryService, gitService, consoleWriter, cSharpParserService,
                projectCreatorService, zipParserService, crudService, settingsService, featureSettingService, fileGeneratorService, uiEventBroker);

            this.consoleWriter = (ConsoleWriter)consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            txtFileGenerator_Folder.Text = Path.GetTempPath() + "BIAToolKit\\";

            ViewModel = new MainViewModel(Assembly.GetExecutingAssembly().GetName().Version, uiEventBroker, settingsService, gitService, consoleWriter);
            DataContext = ViewModel;

            uiEventBroker.OnNewVersionAvailable += UiEventBroker_OnNewVersionAvailable;
            uiEventBroker.OnSettingsUpdated += UiEventBroker_OnSettingsUpdated;
            uiEventBroker.OnRepositoriesUpdated += UiEventBroker_OnRepositoriesUpdated;
            uiEventBroker.OnOpenRepositoryFormRequest += UiEventBroker_OnRepositoryFormOpened;
        }

        private void UiEventBroker_OnRepositoryFormOpened(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            var form = new RepositoryFormUC(repository, gitService, uiEventBroker, consoleWriter) { Owner = this };
            if (form.ShowDialog() == true)
            {
                switch (mode)
                {
                    case RepositoryFormMode.Edit:
                        uiEventBroker.NotifyRepositoryViewModelChanged(repository, form.ViewModel.Repository);
                        break;
                    case RepositoryFormMode.Create:
                        uiEventBroker.NotifyRepositoryViewModelAdded(form.ViewModel.Repository);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void UiEventBroker_OnRepositoriesUpdated()
        {
            UiEventBroker_OnSettingsUpdated(settingsService.Settings);
        }

        public async Task Init()
        {
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await InitSettings();

                if (Properties.Settings.Default.ApplicationUpdated)
                {
                    Properties.Settings.Default.Upgrade();

                    Properties.Settings.Default.ApplicationUpdated = false;
                    Properties.Settings.Default.Save();
                }

                updateService.SetAppVersion(Assembly.GetExecutingAssembly().GetName().Version);

                if (Properties.Settings.Default.AutoUpdate)
                {
                    await updateService.CheckForUpdatesAsync();
                }

                await Task.Run(() => CSharpParserService.RegisterMSBuild(consoleWriter));
            });
        }

        private async Task InitSettings()
        {
            var settings = new BIATKSettings
            {
                UseCompanyFiles = Properties.Settings.Default.UseCompanyFile,
                CreateProjectRootProjectsPath = Properties.Settings.Default.CreateProjectRootFolderText,
                CreateCompanyName = Properties.Settings.Default.CreateCompanyName,
                ModifyProjectRootProjectsPath = Properties.Settings.Default.ModifyProjectRootFolderText,
                AutoUpdate = Properties.Settings.Default.AutoUpdate,

                ToolkitRepository = !string.IsNullOrEmpty(Properties.Settings.Default.ToolkitRepository) ?
                    ConvertRepository(JsonConvert.DeserializeObject<RepositoryUserConfig>(Properties.Settings.Default.ToolkitRepository)) :
                    RepositoryGit.CreateWithReleaseTypeGit(
                        name: "BIAToolkit GIT",
                        repositoryGitKind: RepositoryGitKind.Github,
                        url: "https://github.com/BIATeam/BIAToolKit",
                        gitRepositoryName: "BIAToolKit",
                        owner: "BIATeam",
                        useRepository: true
                    ),

                TemplateRepositories = !string.IsNullOrEmpty(Properties.Settings.Default.TemplateRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories)
                    .Select(ConvertRepository)
                    .ToList() :
                    [
                        RepositoryGit.CreateWithReleaseTypeGit(
                            name: "BIATemplate GIT",
                            repositoryGitKind: RepositoryGitKind.Github,
                            url: "https://github.com/BIATeam/BIADemo",
                            gitRepositoryName: "BIATemplate",
                            owner: "BIATeam",
                            companyName: "TheBIADevCompany",
                            projectName: "BIATemplate",
                            useRepository: true
                        ),
                    ],

                CompanyFilesRepositories = !string.IsNullOrEmpty(Properties.Settings.Default.CompanyFilesRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories)
                    .Select(ConvertRepository)
                    .ToList() :
                    []
            };

            var fillReleasesTasks = settings.TemplateRepositories
                .Concat(settings.CompanyFilesRepositories)
                .Where(r => r.UseRepository)
                .Select(async (r) =>
                {
                    try
                    {
                        consoleWriter.AddMessageLine($"Getting releases data for repository {r.Name}...", "pink");
                        await r.FillReleasesAsync();
                        consoleWriter.AddMessageLine($"Releases data got successfully for repository {r.Name}", "green");
                    }
                    catch (Exception ex)
                    {
                        consoleWriter.AddMessageLine($"Error while getting releases data for repository {r.Name} : {ex.Message}", "red");
                    }
                });
            await Task.WhenAll(fillReleasesTasks);

            settingsService.Init(settings);
        }

        private IRepository ConvertRepository(RepositoryUserConfig repositoryUserConfig)
        {
            return repositoryUserConfig.RepositoryType switch
            {
                RepositoryType.Git => repositoryUserConfig.ReleaseType switch
                {
                    ReleaseType.Git => RepositoryGit.CreateWithReleaseTypeGit(
                        name: repositoryUserConfig.Name,
                        repositoryGitKind: repositoryUserConfig.RepositoryGitKind,
                        url: repositoryUserConfig.Url,
                        urlRelease: repositoryUserConfig.UrlRelease,
                        gitRepositoryName: repositoryUserConfig.GitRepositoryName,
                        owner: repositoryUserConfig.Owner,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    ReleaseType.Folder => RepositoryGit.CreateWithReleaseTypeFolder(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        releasesFolderRegexPattern: repositoryUserConfig.ReleasesFolderRegexPattern,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    ReleaseType.Tag => RepositoryGit.CreateWithReleaseTypeTag(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    _ => throw new NotImplementedException(),
                },
                RepositoryType.Folder => new RepositoryFolder(
                    name: repositoryUserConfig.Name,
                    path: repositoryUserConfig.Path,
                    releasesFolderRegexPattern: repositoryUserConfig.ReleasesFolderRegexPattern,
                    companyName: repositoryUserConfig.CompanyName,
                    projectName: repositoryUserConfig.ProjectName,
                    useRepository: repositoryUserConfig.UseRepository),
                _ => throw new NotImplementedException(),
            };
        }

        private void UiEventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            Properties.Settings.Default.UseCompanyFile = settings.UseCompanyFiles;
            Properties.Settings.Default.CreateProjectRootFolderText = settings.CreateProjectRootProjectsPath;
            Properties.Settings.Default.ModifyProjectRootFolderText = settings.ModifyProjectRootProjectsPath;
            Properties.Settings.Default.CreateCompanyName = settings.CreateCompanyName;
            Properties.Settings.Default.AutoUpdate = settings.AutoUpdate;
            Properties.Settings.Default.ToolkitRepository = JsonConvert.SerializeObject(settings.ToolkitRepository);
            Properties.Settings.Default.TemplateRepositories = JsonConvert.SerializeObject(settings.TemplateRepositories);
            Properties.Settings.Default.CompanyFilesRepositories = JsonConvert.SerializeObject(settings.CompanyFilesRepositories);
            Properties.Settings.Default.Save();
        }

        private async void UiEventBroker_OnNewVersionAvailable()
        {
            ViewModel.UpdateAvailable = true;
            CheckUpdateButton.IsEnabled = false;
            await OnUpdateAvailable();
        }

        public bool CheckRepositoriesConfiguration()
        {
            var templatesConfigurationValid = CheckTemplateRepositoriesConfiguration();
            var companyFilesConfigurationValid = CheckCompanyFilesRepositoriesConfiguration();
            return templatesConfigurationValid && companyFilesConfigurationValid;
        }

        public bool CheckTemplateRepositoriesConfiguration()
        {
            if (!CheckTemplateRepositories(settingsService.Settings))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

        public bool CheckCompanyFilesRepositoriesConfiguration()
        {
            if (!CheckCompanyFilesRepositories(settingsService.Settings))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

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

        private readonly SemaphoreSlim semaphore = new(1, 1);
        private async Task ExecuteTaskWithWaiterAsync(Func<Task> task)
        {
            await semaphore.WaitAsync();
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    Waiter.Visibility = Visibility.Visible;
                    await task().ConfigureAwait(true);
                }
                finally
                {
                    Waiter.Visibility = Visibility.Hidden;
                }
            }).Task.Unwrap();
            semaphore.Release();
        }

        private void CreateProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Settings_RootProjectsPath = FileDialog.BrowseFolder(ViewModel.Settings_RootProjectsPath);
        }

        private void OnTabModifySelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                if (CheckRepositoriesConfiguration())
                {
                    ModifyProject.RefreshConfiguration();
                }
            }
        }

        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                if (CheckRepositoriesConfiguration())
                {
                    CreateVersionAndOption.RefreshConfiguration();
                }
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            _ = Create_Run();
        }

        private async Task Create_Run()
        {
            if (string.IsNullOrEmpty(settingsService.Settings.CreateProjectRootProjectsPath))
            {
                MessageBox.Show("Please select root path.");
                return;
            }
            if (string.IsNullOrEmpty(settingsService.Settings.CreateCompanyName))
            {
                MessageBox.Show("Please select company name.");
                return;
            }
            if (string.IsNullOrEmpty(CreateProjectName.Text))
            {
                MessageBox.Show("Please select project name.");
                return;
            }
            if (CreateVersionAndOption.vm.WorkTemplate == null)
            {
                MessageBox.Show("Please select framework version.");
                return;
            }

            string projectPath = settingsService.Settings.CreateProjectRootProjectsPath + "\\" + CreateProjectName.Text;
            if (Directory.Exists(projectPath) && !FileDialog.IsDirectoryEmpty(projectPath))
            {
                MessageBox.Show("The project path is not empty : " + projectPath);
                return;
            }

            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await this.projectCreatorService.Create(
                    true,
                    projectPath,
                    new Domain.Model.ProjectParameters
                    {
                        CompanyName = settingsService.Settings.CreateCompanyName,
                        ProjectName = CreateProjectName.Text,
                        VersionAndOption = CreateVersionAndOption.vm.VersionAndOption,
                        AngularFronts = new List<string> { Constants.FolderAngular }
                    });
            });
        }

        private void btnFileGenerator_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFileGenerator_Folder.Text = dlg.SelectedPath;
            }
        }

        private void btnFileGenerator_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox 
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Open document 
                string filename = dlg.FileName;
                txtFileGenerator_File.Text = filename;
                btnFileGenerator_Generate.IsEnabled = true;
            }
        }

        private void btnFileGenerator_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFileGenerator_Folder.Text))
            {
                if (!string.IsNullOrEmpty(txtFileGenerator_File.Text))
                {
                    string resultFolder = string.Empty;
                    generateFilesService.GenerateFiles(txtFileGenerator_File.Text, txtFileGenerator_Folder.Text, ref resultFolder);
                    ProcessStartInfo startInfo = new ProcessStartInfo(resultFolder)
                    {
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);

                }
                else
                {
                    MessageBox.Show("Select the class file", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            else
            {
                MessageBox.Show("Select the folder to save the files", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await OnUpdateAvailable();
        }

        private async Task OnUpdateAvailable()
        {
            try
            {
                var result = MessageBox.Show(
                                    $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?",
                                    "Update available",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await ExecuteTaskWithWaiterAsync(updateService.DownloadUpdateAsync);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failure : {ex.Message}", "Update failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyConsoleContentToClipboard_Click(object sender, RoutedEventArgs e)
        {
            this.consoleWriter.CopyToClipboard();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            this.consoleWriter.Clear();
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteTaskWithWaiterAsync(updateService.CheckForUpdatesAsync);
        }
    }
}
