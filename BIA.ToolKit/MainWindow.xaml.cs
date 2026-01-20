namespace BIA.ToolKit
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.UserControls;
    using BIA.ToolKit.ViewModels;
    using CommunityToolkit.Mvvm.Messaging;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; private set; }

        private readonly ConsoleWriter consoleWriter;
        private readonly GitService gitService;
        private readonly IMessenger messenger;
        private readonly IFileDialogService fileDialogService;

        public MainWindow(
            MainWindowViewModel mainWindowViewModel,
            GitService gitService,
            IConsoleWriter consoleWriter,
            IMessenger messenger,
            IFileDialogService fileDialogService,
            ModifyProjectUC modifyProjectUC,
            VersionAndOptionUserControl createVersionAndOptionUC)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            this.gitService = gitService;
            this.messenger = messenger;
            this.fileDialogService = fileDialogService;
            this.consoleWriter = (ConsoleWriter)consoleWriter;

            CreateVersionAndOptionHost.Content = createVersionAndOptionUC;
            ModifyProjectHost.Content = modifyProjectUC;

            this.consoleWriter.InitOutput(OutputText, OutputTextViewer, this);

            // Use MainWindowViewModel as DataContext
            ViewModel = mainWindowViewModel;
            DataContext = ViewModel;

            ViewModel.WaiterRequested += ExecuteTaskWithWaiterAsync;
            ViewModel.PersistSettingsRequested += UiEventBroker_OnSettingsUpdated;

            messenger.Register<OpenRepositoryFormRequestMessage>(this, (r, m) => UiEventBroker_OnRepositoryFormOpened(m.Repository, m.Mode));
        }

        private void UiEventBroker_OnRepositoryFormOpened(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            var form = new RepositoryFormUC(repository, gitService, messenger, consoleWriter, fileDialogService) { Owner = this };
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

        private void UiEventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            Properties.Settings.Default.UseCompanyFile = settings.UseCompanyFiles;
            Properties.Settings.Default.CreateProjectRootFolderText = settings.CreateProjectRootProjectsPath;
            Properties.Settings.Default.ModifyProjectRootFolderText = settings.ModifyProjectRootProjectsPath;
            Properties.Settings.Default.CreateCompanyName = settings.CreateCompanyName;
            Properties.Settings.Default.AutoUpdate = settings.AutoUpdate;
            Properties.Settings.Default.ToolkitRepository = Newtonsoft.Json.JsonConvert.SerializeObject(settings.ToolkitRepository);
            Properties.Settings.Default.TemplateRepositories = Newtonsoft.Json.JsonConvert.SerializeObject(settings.TemplateRepositories);
            Properties.Settings.Default.CompanyFilesRepositories = Newtonsoft.Json.JsonConvert.SerializeObject(settings.CompanyFilesRepositories);
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
