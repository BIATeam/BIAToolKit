﻿namespace BIA.ToolKit
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


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel _viewModel = new MainViewModel(Assembly.GetExecutingAssembly().GetName().Version);

        RepositoryService repositoryService;
        GitService gitService;
        ProjectCreatorService projectCreatorService;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly UIEventBroker uiEventBroker;
        private readonly UpdateService updateService;
        GenerateFilesService generateFilesService;
        ConsoleWriter consoleWriter;
        bool isCreateTabInitialized = false;
        bool isModifyTabInitialized = false;

        public MainWindow(RepositoryService repositoryService, GitService gitService, CSharpParserService cSharpParserService, GenerateFilesService genFilesService,
            ProjectCreatorService projectCreatorService, ZipParserService zipParserService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, FeatureSettingService featureSettingService, FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker, UpdateService updateService)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.repositoryService = repositoryService;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;
            this.fileGeneratorService = fileGeneratorService;
            this.uiEventBroker = uiEventBroker;
            this.updateService = updateService;
            this.generateFilesService = genFilesService;

            uiEventBroker.OnActionWithWaiter += async (action) => await ExecuteTaskWithWaiterAsync(action);

            InitializeComponent();

            CreateVersionAndOption.Inject(_viewModel.Settings, this.repositoryService, gitService, consoleWriter, featureSettingService, settingsService, uiEventBroker);
            ModifyProject.Inject(_viewModel.Settings, this.repositoryService, gitService, consoleWriter, cSharpParserService,
                projectCreatorService, zipParserService, crudService, settingsService, featureSettingService, fileGeneratorService, uiEventBroker);

            this.consoleWriter = (ConsoleWriter)consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            _viewModel.Settings.BIATemplateRepository.Name = "BIATemplate";
            _viewModel.Settings.BIATemplateRepository.Versioning = VersioningType.Release;
            _viewModel.Settings.BIATemplateRepository.UrlRelease = Constants.BIATemplateReleaseUrl;
            _viewModel.Settings.BIATemplateRepository.UrlRepo = Constants.BIATemplateRepoUrl;
            _viewModel.Settings.BIATemplateRepository.CompanyName = "TheBIADevCompany";
            _viewModel.Settings.BIATemplateRepository.ProjectName = "BIATemplate";

            _viewModel.Settings.BIATemplateRepository.UseLocalFolder = Properties.Settings.Default.BIATemplateLocalFolder;
            _viewModel.Settings.BIATemplateRepository.LocalFolderPath = Properties.Settings.Default.BIATemplateLocalFolderText;


            _viewModel.Settings.UseCompanyFiles = Properties.Settings.Default.UseCompanyFile;
            _viewModel.Settings.CompanyFiles.Name = "BIACompanyFiles";
            _viewModel.Settings.CompanyFiles.Versioning = VersioningType.Folder;
            _viewModel.Settings.CompanyFiles.CompanyName = "TheBIADevCompany";
            _viewModel.Settings.CompanyFiles.ProjectName = "BIATemplate";
            _viewModel.Settings.CompanyFiles.UseLocalFolder = Properties.Settings.Default.CompanyFilesLocalFolder;
            _viewModel.Settings.CompanyFiles.UrlRepo = Properties.Settings.Default.CompanyFilesGitRepo;
            _viewModel.Settings.CompanyFiles.LocalFolderPath = Properties.Settings.Default.CompanyFilesLocalFolderText;

            _viewModel.Settings.RootProjectsPath = Properties.Settings.Default.CreateProjectRootFolderText;
            _viewModel.Settings.CreateCompanyName = Properties.Settings.Default.CreateCompanyName;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.CustomTemplates))
            {
                _viewModel.Settings.CustomRepoTemplates = JsonSerializer.Deserialize<List<RepositorySettings>>(Properties.Settings.Default.CustomTemplates);
            }

            txtFileGenerator_Folder.Text = Path.GetTempPath() + "BIAToolKit\\";

            DataContext = _viewModel;

            uiEventBroker.OnNewVersionAvailable += UiEventBroker_OnNewVersionAvailable;
        }

        private void UiEventBroker_OnNewVersionAvailable()
        {
            _viewModel.UpdateAvailable = true;
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
            if (!CheckBIATemplate(_viewModel.Settings, inSync))
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
            if (!CheckCompanyFiles(_viewModel.Settings, inSync))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

        public bool CheckBIATemplate(BIATKSettings biaTKsettings, bool inSync)
        {
            if (!repositoryService.CheckRepoFolder(biaTKsettings.BIATemplateRepository, inSync))
            {
                return false;
            }
            foreach (var customRepo in
                biaTKsettings.CustomRepoTemplates)
            {
                if (!repositoryService.CheckRepoFolder(customRepo, inSync))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckCompanyFiles(BIATKSettings biaTKsettings, bool inSync)
        {
            if (biaTKsettings.UseCompanyFiles)
            {
                return repositoryService.CheckRepoFolder(biaTKsettings.CompanyFiles, inSync);
            }
            return true;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BIATemplateLocalFolder = _viewModel.Settings.BIATemplateRepository.UseLocalFolder;
            Properties.Settings.Default.BIATemplateLocalFolderText = _viewModel.Settings.BIATemplateRepository.LocalFolderPath;

            Properties.Settings.Default.UseCompanyFile = _viewModel.Settings.UseCompanyFiles;
            Properties.Settings.Default.CompanyFilesLocalFolder = _viewModel.Settings.CompanyFiles.UseLocalFolder;
            Properties.Settings.Default.CompanyFilesGitRepo = _viewModel.Settings.CompanyFiles.UrlRepo;
            Properties.Settings.Default.CompanyFilesLocalFolderText = _viewModel.Settings.CompanyFiles.LocalFolderPath;

            Properties.Settings.Default.CreateProjectRootFolderText = _viewModel.Settings.RootProjectsPath;
            Properties.Settings.Default.CreateCompanyName = _viewModel.Settings.CreateCompanyName;

            Properties.Settings.Default.CustomTemplates = JsonSerializer.Serialize(_viewModel.Settings.CustomRepoTemplates);

            Properties.Settings.Default.Save();
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
            if (RefreshBIATemplateConfiguration(true))
            {
                await ExecuteTaskWithWaiterAsync(async () =>
                {
                    BIATemplateLocalFolderSync.IsEnabled = false;
                    if (!_viewModel.Settings.BIATemplateRepository.UseLocalFolder)
                    {
                        await repositoryService.CleanVersionFolder(_viewModel.Settings.BIATemplateRepository);
                    }

                    await this.gitService.Synchronize(_viewModel.Settings.BIATemplateRepository);
                    BIATemplateLocalFolderSync.IsEnabled = true;
                });
            }
        }

        private async void BIATemplateLocalCleanRelease_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await repositoryService.CleanVersionFolder(_viewModel.Settings.BIATemplateRepository);
            });
        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshCompanyFilesConfiguration(true))
            {
                await ExecuteTaskWithWaiterAsync(async () =>
                {
                    CompanyFilesLocalFolderSync.IsEnabled = false;
                    await this.gitService.Synchronize(_viewModel.Settings.CompanyFiles);
                    CompanyFilesLocalFolderSync.IsEnabled = true;
                });
            }
        }

        private void BIATemplateLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Settings_BIATemplateRepository_LocalFolderPath = FileDialog.BrowseFolder(_viewModel.Settings.BIATemplateRepository.LocalFolderPath);
        }

        private void CompanyFilesLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Settings_CompanyFiles_LocalFolderPath = FileDialog.BrowseFolder(_viewModel.Settings.CompanyFiles.LocalFolderPath);
        }

        private void CreateProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Settings_RootProjectsPath = FileDialog.BrowseFolder(_viewModel.Settings.RootProjectsPath);
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
            if (string.IsNullOrEmpty(_viewModel.Settings.RootProjectsPath))
            {
                MessageBox.Show("Please select root path.");
                return;
            }
            if (string.IsNullOrEmpty(_viewModel.Settings.CreateCompanyName))
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

            string projectPath = _viewModel.Settings.RootProjectsPath + "\\" + CreateProjectName.Text;
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
                        CompanyName = _viewModel.Settings.CreateCompanyName,
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
            var dialog = new CustomsRepoTemplateUC(gitService, repositoryService, uiEventBroker) { Owner = this };

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog(_viewModel.Settings.CustomRepoTemplates);

            if (result == true)
            {
                _viewModel.Settings.CustomRepoTemplates = dialog.vm.RepositoriesSettings.ToList();
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Debugger.IsAttached)
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
        }

        private void CopyConsoleContentToClipboard_Click(object sender, RoutedEventArgs e)
        {
            this.consoleWriter.CopyToClipboard();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            this.consoleWriter.Clear();
        }
    }
}
