namespace BIA.ToolKit
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Code-behind is limited to view wiring (DataContext, console output control,
    /// tab-navigation message handling). All business logic lives in MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        public MainWindow(IConsoleWriter consoleWriter)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            InitializeComponent();

            ((ConsoleWriter)consoleWriter).InitOutput(OutputText, OutputTextViewer, this, App.GetService<IDialogService>());

            var createVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            CreateVersionAndOption.DataContext = createVersionAndOptionVM;

            ViewModel = App.GetService<MainViewModel>();
            ViewModel.CreateVersionAndOptionVM = createVersionAndOptionVM;
            DataContext = ViewModel;

            WeakReferenceMessenger.Default.Register<NavigateToConfigTabMessage>(this, (r, m) => Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0)));
        }

        public System.Threading.Tasks.Task Init() => ViewModel.InitAsync();

        private void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                ViewModel.EnsureValidRepositoriesConfiguration();
            }
        }
    }
}
