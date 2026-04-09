namespace BIA.ToolKit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.Messaging;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly ConsoleWriter consoleWriter;

        public MainWindow(SettingsService settingsService, GitService gitService, IConsoleWriter consoleWriter)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.settingsService = settingsService;
            this.gitService = gitService;

            InitializeComponent();

            this.consoleWriter = (ConsoleWriter)consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this, App.GetService<IDialogService>());

            var createVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            CreateVersionAndOption.DataContext = createVersionAndOptionVM;

            ViewModel = App.GetService<MainViewModel>();
            ViewModel.CreateVersionAndOptionVM = createVersionAndOptionVM;
            DataContext = ViewModel;

            WeakReferenceMessenger.Default.Register<SettingsUpdatedMessage>(this, (r, m) => HandleSettingsUpdated(m.Settings));
            WeakReferenceMessenger.Default.Register<OpenRepositoryFormMessage>(this, (r, m) => HandleRepositoryFormOpened(m.Repository, m.Mode));
            WeakReferenceMessenger.Default.Register<NavigateToConfigTabMessage>(this, (r, m) => Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0)));
        }

        public async System.Threading.Tasks.Task Init()
        {
            BIATKSettings settings = BuildSettingsFromUserPreferences();
            await ViewModel.InitAsync(settings);
        }

        private static BIATKSettings BuildSettingsFromUserPreferences()
        {
            return new BIATKSettings
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
                        RepositoryType = Domain.RepositoryType.Git,
                        RepositoryGitKind = Domain.RepositoryGitKind.Github,
                        Url = "https://github.com/BIATeam/BIAToolKit",
                        GitRepositoryName = "BIAToolKit",
                        Owner = "BIATeam",
                        UseRepository = true
                    },

                TemplateRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.TemplateRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories) :
                    [
                        new RepositoryUserConfig()
                        {
                            Name = "BIATemplate GIT",
                            RepositoryType = Domain.RepositoryType.Git,
                            RepositoryGitKind = Domain.RepositoryGitKind.Github,
                            Url = "https://github.com/BIATeam/BIADemo",
                            GitRepositoryName = "BIATemplate",
                            Owner = "BIATeam",
                            CompanyName = "TheBIADevCompany",
                            ProjectName = "BIATemplate",
                            UseRepository = true
                        }
                    ],

                CompanyFilesRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.CompanyFilesRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories) : [],
            };
        }

        private void HandleSettingsUpdated(IBIATKSettings settings)
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

        private void HandleRepositoryFormOpened(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            var form = new RepositoryFormUC(repository, gitService, consoleWriter) { Owner = this };
            if (form.ShowDialog() == true)
            {
                switch (mode)
                {
                    case RepositoryFormMode.Edit:
                        WeakReferenceMessenger.Default.Send(new RepositoryChangedMessage(repository, form.ViewModel.Repository));
                        break;
                    case RepositoryFormMode.Create:
                        WeakReferenceMessenger.Default.Send(new RepositoryAddedMessage(form.ViewModel.Repository));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                ViewModel.EnsureValidRepositoriesConfiguration();
            }
        }

        // --- File Generator tab (hidden, legacy) ---

        private void BtnFileGenerator_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFileGenerator_Folder.Text = dlg.SelectedPath;
            }
        }

        private void BtnFileGenerator_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string filename = dlg.FileName;
                txtFileGenerator_File.Text = filename;
                btnFileGenerator_Generate.IsEnabled = true;
            }
        }

        private void BtnFileGenerator_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFileGenerator_Folder.Text))
            {
                if (!string.IsNullOrEmpty(txtFileGenerator_File.Text))
                {
                    string resultFolder = string.Empty;
                    var generateFilesService = App.GetService<GenerateFilesService>();
                    generateFilesService.GenerateFiles(txtFileGenerator_File.Text, txtFileGenerator_Folder.Text, ref resultFolder);
                    var startInfo = new System.Diagnostics.ProcessStartInfo(resultFolder)
                    {
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
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
    }
}
