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
    using BIA.ToolKit.Application.ViewModel;
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;
    using BIA.ToolKit.UserControls;
    using BIA.ToolKit.ViewModels;
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
    using BIA.ToolKit.Common.Helpers;
    using System.Configuration;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Application.Messages;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; private set; }

        private readonly ConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;
        private readonly GitService gitService;
        private readonly CSharpParserService cSharpParserService;
        private readonly SettingsService settingsService;
        private readonly UpdateService updateService;
        private readonly ZipParserService zipParserService;
        private readonly GenerateCrudService crudService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly GenerateFilesService generateFilesService;
        private readonly IMessenger messenger;

        public MainWindow(
            MainWindowViewModel mainWindowViewModel,
            RepositoryService repositoryService, 
            GitService gitService, 
            CSharpParserService cSharpParserService, 
            GenerateFilesService genFilesService,
            ProjectCreatorService projectCreatorService, 
            ZipParserService zipParserService, 
            GenerateCrudService crudService, 
            SettingsService settingsService,
            IConsoleWriter consoleWriter, 
            FileGeneratorService fileGeneratorService, 
            IMessenger messenger,
            UpdateService updateService,
            IFileDialogService fileDialogService)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.repositoryService = repositoryService;
            this.gitService = gitService;
            this.cSharpParserService = cSharpParserService;
            this.settingsService = settingsService;
            this.updateService = updateService;
            this.messenger = messenger;
            this.zipParserService = zipParserService;
            this.crudService = crudService;
            this.projectCreatorService = projectCreatorService;
            this.generateFilesService = genFilesService;
            this.consoleWriter = (ConsoleWriter)consoleWriter;

            // IMessenger subscriptions for message handling
            messenger.Register<ExecuteActionWithWaiterMessage>(this, async (r, m) => await ExecuteTaskWithWaiterAsync(m.Action));
            messenger.Register<NewVersionAvailableMessage>(this, (r, m) => UiEventBroker_OnNewVersionAvailable());
            messenger.Register<SettingsUpdatedMessage>(this, (r, m) => UiEventBroker_OnSettingsUpdated(m.Settings));
            messenger.Register<RepositoriesUpdatedMessage>(this, (r, m) => UiEventBroker_OnRepositoriesUpdated());
            messenger.Register<OpenRepositoryFormRequestMessage>(this, (r, m) => UiEventBroker_OnRepositoryFormOpened(m.Repository, m.Mode));

            CreateVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, messenger);
            ModifyProject.Inject(repositoryService, gitService, consoleWriter, cSharpParserService,
                projectCreatorService, zipParserService, crudService, settingsService, fileGeneratorService, messenger,
                new Infrastructure.Services.FileDialogService());

            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            txtFileGenerator_Folder.Text = Path.GetTempPath() + "BIAToolKit\\";

            // Use MainWindowViewModel as DataContext
            ViewModel = mainWindowViewModel;
            DataContext = ViewModel;
        }

        private void UiEventBroker_OnRepositoryFormOpened(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            var form = new RepositoryFormUC(repository, gitService, messenger, consoleWriter) { Owner = this };
            if (form.ShowDialog() == true)
            {
                switch (mode)
                {
                    case RepositoryFormMode.Edit:
                        messenger.Send(new RepositoryViewModelChangedMessage(repository, form.ViewModel.Repository));
                        break;
                    case RepositoryFormMode.Create:
                        messenger.Send(new RepositoryViewModelAddedMessage(form.ViewModel.Repository));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void UiEventBroker_OnRepositoriesUpdated()
        {
            messenger.Send(new SettingsUpdatedMessage(settingsService.Settings));
        }

        public async Task Init()
        {
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                await InitSettings();

                updateService.SetAppVersion(Assembly.GetExecutingAssembly().GetName().Version);

                if (Properties.Settings.Default.AutoUpdate)
                {
                    await updateService.CheckForUpdatesAsync();
                }

                await Task.Run(() => cSharpParserService.RegisterMSBuild(consoleWriter));
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
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories) :
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
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories) : [],
            };
            settings.InitRepositoriesInterfaces();
            await GetReleasesData(settings);

            settingsService.Init(settings);
        }

        private async Task GetReleasesData(BIATKSettings settings, bool syncBefore = false)
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
                        if(r.UseDownloadedReleases)
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
            // UpdateAvailable property and update command are in MainWindowViewModel
            // This is a notification that a new version is available
            // The Commands in MainWindowViewModel handle the update logic
        }

        public bool EnsureValidRepositoriesConfiguration()
        {
            // Validation logic is handled by MainWindowViewModel
            // For backward compatibility, just return true
            return true;
        }

        public bool CheckTemplateRepositoriesConfiguration()
        {
            // Validation is handled by MainWindowViewModel
            return true;
        }

        public bool CheckCompanyFilesRepositoriesConfiguration()
        {
            // Validation is handled by MainWindowViewModel
            return true;
        }

        // Legacy methods - kept for backward compatibility
        public bool CheckTemplateRepositories(IBIATKSettings biaTKsettings)
        {
            // Legacy method - not used with new MVVM pattern
            return true;
        }

        public bool CheckCompanyFilesRepositories(IBIATKSettings biaTKsettings)
        {
            // Legacy method - not used with new MVVM pattern
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

        /// <summary>
        /// Called when the Create Project tab is selected to ensure repositories are properly configured
        /// </summary>
        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                // Phase 6 Step 39: Delegate to ViewModel Method
                ViewModel?.OnCreateProjectTabSelected();
            }
        }

        /// <summary>
        /// Called when the Modify Project tab is selected to ensure repositories are properly configured
        /// </summary>
        private void OnTabModifySelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                // Phase 6 Step 39: Delegate to ViewModel Method
                ViewModel?.OnModifyProjectTabSelected();
            }
        }
    }
}
