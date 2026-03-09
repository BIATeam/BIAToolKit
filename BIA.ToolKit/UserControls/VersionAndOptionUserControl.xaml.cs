namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        private UIEventBroker uiEventBroker;
        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker, IMessenger messenger)
        {
            this.uiEventBroker = uiEventBroker;

            vm = new VersionAndOptionViewModel(messenger, repositoryService, consoleWriter, uiEventBroker, settingsService);
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();

            uiEventBroker.OnRepositoryViewModelReleaseDataUpdated += UiEventBroker_OnRepositoryViewModelReleaseDataUpdated;
        }

        private void UiEventBroker_OnRepositoryViewModelReleaseDataUpdated(RepositoryViewModel repository)
        {
            vm.RefreshConfiguration();
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await vm.FillVersionFolderPathAsync();
                vm.LoadfeatureSetting();
                vm.LoadVersionAndOption(false, false);
                vm.NotifyOriginFeatureSettingsChanged();
            });
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.LoadVersionAndOption(false, false);
        }
    }
}
