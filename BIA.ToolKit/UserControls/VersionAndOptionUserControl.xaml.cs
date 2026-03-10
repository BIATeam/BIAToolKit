namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        private IMessenger messenger;
        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, IMessenger messenger)
        {
            this.messenger = messenger;

            vm = new VersionAndOptionViewModel(messenger, repositoryService, consoleWriter, settingsService);
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();

            messenger.Subscribe<RepositoryViewModelReleaseDataUpdatedMessage>(OnRepositoryViewModelReleaseDataUpdated);
        }

        private void OnRepositoryViewModelReleaseDataUpdated(RepositoryViewModelReleaseDataUpdatedMessage message)
        {
            vm.RefreshConfiguration();
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            messenger.Send(new ExecuteWithWaiterMessage
            {
                Task = async () =>
                {
                    await vm.FillVersionFolderPathAsync();
                    vm.LoadfeatureSetting();
                    vm.LoadVersionAndOption(false, false);
                    vm.NotifyOriginFeatureSettingsChanged();
                }
            });
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.LoadVersionAndOption(false, false);
        }
    }
}
