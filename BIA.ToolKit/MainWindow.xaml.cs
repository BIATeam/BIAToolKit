namespace BIA.ToolKit
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.Messaging;
    using MaterialDesignThemes.Wpf;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Code-behind is limited to view wiring (DataContext, console output control,
    /// tab-navigation message handling). All business logic lives in MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private readonly ConsoleWriter consoleWriterInstance;

        public MainWindow(IConsoleWriter consoleWriter)
        {
            AppSettings.AppFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.LocalUserAppDataPath));
            AppSettings.TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";

            InitializeComponent();

            consoleWriterInstance = (ConsoleWriter)consoleWriter;
            consoleWriterInstance.InitOutput(OutputText, OutputTextViewer, this, App.GetService<IDialogService>());

            var createVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            CreateVersionAndOption.DataContext = createVersionAndOptionVM;

            ViewModel = App.GetService<MainViewModel>();
            ViewModel.CreateVersionAndOptionVM = createVersionAndOptionVM;
            DataContext = ViewModel;

            WeakReferenceMessenger.Default.Register<NavigateToConfigTabMessage>(this, (r, m) => Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0)));

            // Read IsDarkTheme directly from persisted settings because
            // SettingsService.Load() hasn't been called yet at this point.
            ApplyTheme(Properties.Settings.Default.IsDarkTheme);
        }

        public System.Threading.Tasks.Task Init() => ViewModel.InitAsync();

        private void ThemeToggleClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Settings_IsDarkTheme = !ViewModel.Settings_IsDarkTheme;
            ApplyTheme(ViewModel.Settings_IsDarkTheme);
        }

        private void ApplyTheme(bool isDark)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

            if (isDark)
            {
                theme.SetPrimaryColor(Color.FromRgb(0x90, 0xEE, 0x90));
                theme.SetSecondaryColor(Color.FromRgb(0x00, 0xFF, 0x00));
            }
            else
            {
                theme.SetPrimaryColor(Color.FromRgb(0x43, 0xA0, 0x47)); // Green 600 — visible toggle on white
                theme.SetSecondaryColor(Color.FromRgb(0x00, 0x89, 0x7B));
            }

            paletteHelper.SetTheme(theme);

            if (consoleWriterInstance != null)
            {
                bool themeChanged = consoleWriterInstance.IsDarkTheme != isDark;
                consoleWriterInstance.IsDarkTheme = isDark;
                if (themeChanged)
                    consoleWriterInstance.ReRenderMessages();
            }

            if (ThemeIcon != null)
                ThemeIcon.Kind = isDark ? PackIconKind.WeatherNight : PackIconKind.WeatherSunny;

            UpdateMaximizeIcon();
        }

        private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeIcon();
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();

        private void UpdateMaximizeIcon()
        {
            if (MaximizeIcon != null)
            {
                MaximizeIcon.Kind = WindowState == WindowState.Maximized
                    ? PackIconKind.WindowRestore
                    : PackIconKind.WindowMaximize;
            }
        }

        private void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TabItem)
            {
                ViewModel.EnsureValidRepositoriesConfiguration();
            }
        }
    }
}
