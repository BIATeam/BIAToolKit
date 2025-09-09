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

            uiEventBroker.OnActionWithWaiter += async (action) => await ExecuteTaskWithWaiterAsync(action);

            InitializeComponent();

            CreateVersionAndOption.Inject(this.repositoryService, gitService, consoleWriter, featureSettingService, settingsService, uiEventBroker);
            ModifyProject.Inject(this.repositoryService, gitService, consoleWriter, cSharpParserService,
                projectCreatorService, zipParserService, crudService, settingsService, featureSettingService, fileGeneratorService, uiEventBroker);

            this.consoleWriter = (ConsoleWriter)consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            txtFileGenerator_Folder.Text = Path.GetTempPath() + "BIAToolKit\\";

            ViewModel = new MainViewModel(Assembly.GetExecutingAssembly().GetName().Version, uiEventBroker, settingsService);
            DataContext = ViewModel;

            uiEventBroker.OnNewVersionAvailable += UiEventBroker_OnNewVersionAvailable;
            uiEventBroker.OnSettingsUpdated += UiEventBroker_OnSettingsUpdated;
        }

        public async Task Init()
        {
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                InitSettings();

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

        private void InitSettings()
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
                    new RepositoryGit(
                        name: "BIAToolkit GIT",
                        url: "https://github.com/BIATeam/BIAToolKit",
                        gitRepositoryName: "BIAToolKit",
                        owner: "BIATeam",
                        useLocalClonedFolder: false
                    ),
                //new RepositoryFolder(
                //        name: "BIAToolkit Folder",
                //        path: "\\\\share.bia.safran\\BIAToolKit\\Releases\\BiaToolkit"
                //    ),

                TemplateRepositories = !string.IsNullOrEmpty(Properties.Settings.Default.TemplateRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories)
                    .Select(ConvertRepository)
                    .ToList() :
                    [
                        new RepositoryGit(
                            name: "BIATemplate GIT",
                            url: "https://github.com/BIATeam/BIADemo",
                            gitRepositoryName: "BIAToolKit",
                            owner: "BIATeam",
                            useLocalClonedFolder: false,
                            companyName: "TheBIADevCompany",
                            projectName: "BIATemplate"
                        )
                    ],

                CompanyFilesRepositories = !string.IsNullOrEmpty(Properties.Settings.Default.CompanyFilesRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories)
                    .Select(ConvertRepository)
                    .ToList() :
                    []
            };

            settingsService.Init(settings);
        }

        private IRepository ConvertRepository(RepositoryUserConfig repositoryUserConfig)
        {
            return repositoryUserConfig.RepositoryType switch
            {
                RepositoryType.Git => repositoryUserConfig.ReleaseType switch
                {
                    ReleaseType.Git => new RepositoryGit(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        gitRepositoryName: repositoryUserConfig.GitRepositoryName,
                        owner: repositoryUserConfig.Owner,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath
                    ),
                    ReleaseType.Folder => new RepositoryGit(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        releasesFolderRegexPattern: repositoryUserConfig.ReleasesFolderRegexPattern,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath
                    ),
                    _ => throw new NotImplementedException(),
                },
                RepositoryType.Folder => new RepositoryFolder(
                    name: repositoryUserConfig.Name,
                    path: repositoryUserConfig.LocalPath,
                    companyName: repositoryUserConfig.CompanyName,
                    projectName: repositoryUserConfig.ProjectName),
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

        public bool RefreshConfiguration()
        {
            if (RefreshBIATemplateConfiguration(false))
            {
                return RefreshCompanyFilesConfiguration(false);
            }
            return false;
        }

        public bool RefreshBIATemplateConfiguration(bool inSync)
        {
            Configurationchange();
            if (!CheckBIATemplate(settingsService.Settings, inSync))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }
        public void ConfigurationChange(object sender, RoutedEventArgs e)
        {
            Configurationchange();
        }

        private void Configurationchange()
        {
            isCreateTabInitialized = false;
            isModifyTabInitialized = false;
        }

        public bool RefreshCompanyFilesConfiguration(bool inSync)
        {
            Configurationchange();
            if (!CheckCompanyFiles(settingsService.Settings, inSync))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

        public bool CheckBIATemplate(IBIATKSettings biaTKsettings, bool inSync)
        {
            foreach (var repository in biaTKsettings.TemplateRepositories)
            {
                if (!repositoryService.CheckRepoFolder(repository, inSync))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckCompanyFiles(IBIATKSettings biaTKsettings, bool inSync)
        {
            if (biaTKsettings.UseCompanyFiles)
            {
                foreach (var repository in biaTKsettings.CompanyFilesRepositories)
                {
                    if (!repositoryService.CheckRepoFolder(repository, inSync))
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

        private async void BIATemplateLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            //if (RefreshBIATemplateConfiguration(true))
            //{
            //    await ExecuteTaskWithWaiterAsync(async () =>
            //    {
            //        BIATemplateLocalFolderSync.IsEnabled = false;
            //        if (!settingsService.Settings.BIATemplateRepository.UseLocalFolder)
            //        {
            //            await repositoryService.CleanRepository(settingsService.Settings.BIATemplateRepository);
            //        }

            //        await this.gitService.Synchronize(settingsService.Settings.BIATemplateRepository);
            //        BIATemplateLocalFolderSync.IsEnabled = true;
            //    });
            //}
        }

        private async void BIATemplateCleanReleases_Click(object sender, RoutedEventArgs e)
        {
            //await ExecuteTaskWithWaiterAsync(async () =>
            //{
            //    await repositoryService.CleanReleases(settingsService.Settings.BIATemplateRepository);
            //});
        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            //if (RefreshCompanyFilesConfiguration(true))
            //{
            //    await ExecuteTaskWithWaiterAsync(async () =>
            //    {
            //        CompanyFilesLocalFolderSync.IsEnabled = false;
            //        if (!settingsService.Settings.CompanyFilesRepository.UseLocalFolder)
            //        {
            //            await repositoryService.CleanRepository(settingsService.Settings.CompanyFilesRepository);
            //        }

            //        await this.gitService.Synchronize(settingsService.Settings.CompanyFilesRepository);
            //        CompanyFilesLocalFolderSync.IsEnabled = true;
            //    });
            //}
        }

        private void BIATemplateLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.Settings_BIATemplateRepository_LocalFolderPath = FileDialog.BrowseFolder(ViewModel.Settings_BIATemplateRepository_LocalFolderPath);
        }

        private void CompanyFilesLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.Settings_CompanyFilesRepository_LocalFolderPath = FileDialog.BrowseFolder(ViewModel.Settings_CompanyFilesRepository_LocalFolderPath);
        }

        private void CreateProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Settings_RootProjectsPath = FileDialog.BrowseFolder(ViewModel.Settings_RootProjectsPath);
        }

        private void OnTabModifySelected(object sender, RoutedEventArgs e)
        {
            var tab = sender as TabItem;
            if (tab != null)
            {
                if (!isModifyTabInitialized)
                {
                    if (RefreshConfiguration())
                    {
                        ModifyProject.RefreshConfiguration();
                        isModifyTabInitialized = true;
                    }
                }
            }
        }

        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {
            var tab = sender as TabItem;
            if (tab != null)
            {
                if (!isCreateTabInitialized)
                {
                    if (RefreshConfiguration())
                    {
                        CreateVersionAndOption.refreshConfig();
                        isCreateTabInitialized = true;
                    }
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

        private void CustomRepoTemplate_Click(object sender, RoutedEventArgs e)
        {
            //var dialog = new CustomsRepoTemplateUC(gitService, repositoryService, uiEventBroker) { Owner = this };
            //if (dialog.ShowDialog(settingsService.Settings.CustomRepoTemplates) == true)
            //{
            //    settingsService.SetCustomRepositories([.. dialog.vm.RepositoriesSettings.Cast<RepositorySettings>()]);
            //}
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

        private void EditLocalReleaseRepositorySettings_Click(object sender, RoutedEventArgs e)
        {
            //var dialog = new LocalReleaseRepositorySettingsUC() { Owner = this };
            //if (dialog.ShowDialog(settingsService.Settings) == true)
            //{
            //    //settingsService.SetUseLocalReleaseRepository(dialog.ViewModel.UseLocalReleaseRepository);
            //    //settingsService.SetLocalReleaseRepositoryPath(dialog.ViewModel.LocalReleaseRepositoryPath);
            //}
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteTaskWithWaiterAsync(updateService.CheckForUpdatesAsync);
        }
    }
}
