namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private readonly UpdateService updateService;
        private readonly GitService gitService;
        private readonly ConsoleWriter consoleWriter;
        private readonly IMessenger messenger;
        private readonly Action<ExecuteWithWaiterMessage> executeWithWaiterHandler;
        private readonly Action<NewVersionAvailableMessage> newVersionAvailableHandler;
        private readonly Action<SettingsUpdatedMessage> settingsUpdatedHandler;
        private readonly Action<OpenRepositoryFormRequestMessage> openRepositoryFormHandler;

        public MainWindow(RepositoryService repositoryService, GitService gitService, CSharpParserService cSharpParserService, GenerateFilesService genFilesService,
            ProjectCreatorService projectCreatorService, ZipParserService zipParserService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService, UpdateService updateService,
            IMessenger messenger, MainViewModel mainViewModel, ModifyProjectViewModel modifyProjectViewModel,
            CRUDGeneratorViewModel crudGeneratorViewModel, DtoGeneratorViewModel dtoGeneratorViewModel, OptionGeneratorViewModel optionGeneratorViewModel)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.updateService = updateService;
            this.gitService = gitService;
            this.messenger = messenger;

            executeWithWaiterHandler = async (msg) => await ExecuteTaskWithWaiterAsync(msg.Task);
            newVersionAvailableHandler = OnNewVersionAvailable;
            settingsUpdatedHandler = OnSettingsUpdated;
            openRepositoryFormHandler = OnOpenRepositoryFormRequest;

            messenger.Subscribe(executeWithWaiterHandler);

            InitializeComponent();

            CreateVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, messenger);
            ModifyProject.Inject(repositoryService, gitService, consoleWriter, cSharpParserService,
                projectCreatorService, zipParserService, crudService, settingsService, fileGeneratorService, modifyProjectViewModel,
                crudGeneratorViewModel, dtoGeneratorViewModel, optionGeneratorViewModel, messenger);

            this.consoleWriter = (ConsoleWriter)consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            txtFileGenerator_Folder.Text = AppSettings.TmpFolderPath;

            ViewModel = mainViewModel;
            ViewModel.CreateVersionAndOptionVm = CreateVersionAndOption.vm;
            ViewModel.NavigateToSettingsTab = () => Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
            DataContext = ViewModel;
            ViewModel.Initialize();
            Closed += (_, _) =>
            {
                ViewModel.Cleanup();
                messenger.Unsubscribe(executeWithWaiterHandler);
                messenger.Unsubscribe(newVersionAvailableHandler);
                messenger.Unsubscribe(settingsUpdatedHandler);
                messenger.Unsubscribe(openRepositoryFormHandler);
            };

            messenger.Subscribe(newVersionAvailableHandler);
            messenger.Subscribe(settingsUpdatedHandler);
            messenger.Subscribe(openRepositoryFormHandler);
        }

        private void OnOpenRepositoryFormRequest(OpenRepositoryFormRequestMessage message)
        {
            var form = new RepositoryFormUC(message.Repository, gitService, messenger, consoleWriter) { Owner = this };
            if (form.ShowDialog() == true)
            {
                switch (message.Mode)
                {
                    case RepositoryFormMode.Edit:
                        messenger.Send(new RepositoryViewModelChangedMessage { OldRepository = message.Repository, NewRepository = form.ViewModel.Repository });
                        break;
                    case RepositoryFormMode.Create:
                        messenger.Send(new RepositoryViewModelAddedMessage { Repository = form.ViewModel.Repository });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void OnSettingsUpdated(SettingsUpdatedMessage message)
        {
            var settings = message.Settings;
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

        private async void OnNewVersionAvailable(NewVersionAvailableMessage message)
        {
            CheckUpdateButton.IsEnabled = false;
            await OnUpdateAvailable();
        }

        public async Task Init()
        {
            await ExecuteTaskWithWaiterAsync(() => ViewModel.InitAsync(BuildSettingsFromUserPreferences()));
        }

        private BIATKSettings BuildSettingsFromUserPreferences()
        {
            var settings = new BIATKSettings
            {
                UseCompanyFiles = Properties.Settings.Default.UseCompanyFile,
                CreateProjectRootProjectsPath = Properties.Settings.Default.CreateProjectRootFolderText,
                CreateCompanyName = Properties.Settings.Default.CreateCompanyName,
                ModifyProjectRootProjectsPath = Properties.Settings.Default.ModifyProjectRootFolderText,
                AutoUpdate = Properties.Settings.Default.AutoUpdate,

                ToolkitRepositoryConfig = !string.IsNullOrEmpty(Properties.Settings.Default.ToolkitRepository) ?
                    JsonConvert.DeserializeObject<RepositoryUserConfig>(Properties.Settings.Default.ToolkitRepository) :
                    new RepositoryUserConfig()
                    {
                        Name = "BIAToolkit GIT",
                        RepositoryType = RepositoryType.Git,
                        RepositoryGitKind = RepositoryGitKind.Github,
                        Url = "https://github.com/BIATeam/BIAToolKit",
                        GitRepositoryName = "BIAToolKit",
                        Owner = "BIATeam",
                        UseRepository = true
                    },

                TemplateRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.TemplateRepositories) ?
                    JsonConvert.DeserializeObject<System.Collections.Generic.List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories) :
                    [
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
                    ],

                CompanyFilesRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.CompanyFilesRepositories) ?
                    JsonConvert.DeserializeObject<System.Collections.Generic.List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories) : [],
            };
            settings.InitRepositoriesInterfaces();
            return settings;
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
            ViewModel.Settings_RootProjectsPath = FileDialog.BrowseFolder(ViewModel.Settings_RootProjectsPath, "Choose create project root path");
        }

        private void OnTabModifySelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                ViewModel.EnsureValidRepositoriesConfiguration();
            }
        }

        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                ViewModel.EnsureValidRepositoriesConfiguration();
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            messenger.Send(new ExecuteWithWaiterMessage { Task = () => ViewModel.CreateProjectAsync(CreateProjectName.Text) });
        }

        private void btnFileGenerator_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFileGenerator_Folder.Text = dlg.SelectedPath;
            }
        }

        private void btnFileGenerator_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFileGenerator_File.Text = dlg.FileName;
                btnFileGenerator_Generate.IsEnabled = true;
            }
        }

        private void btnFileGenerator_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFileGenerator_Folder.Text))
            {
                MessageBox.Show("Select the folder to save the files", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(txtFileGenerator_File.Text))
            {
                MessageBox.Show("Select the class file", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string resultFolder = ViewModel.GenerateFilesAndGetResultFolder(txtFileGenerator_File.Text, txtFileGenerator_Folder.Text);
            Process.Start(new ProcessStartInfo(resultFolder) { UseShellExecute = true });
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
            consoleWriter.CopyToClipboard();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            consoleWriter.Clear();
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteTaskWithWaiterAsync(ViewModel.CheckForUpdatesAsync);
        }

        private async void ImportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var configFile = FileDialog.BrowseFile(string.Empty, "btksettings");
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
                return;

            await ExecuteTaskWithWaiterAsync(() => ViewModel.ImportConfigAsync(configFile));
        }

        private void ExportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var targetDirectory = FileDialog.BrowseFolder(string.Empty, "Choose export folder target");
            if (string.IsNullOrWhiteSpace(targetDirectory))
                return;

            ViewModel.ExportConfig(Path.Combine(targetDirectory, "user.btksettings"));
        }
    }
}
