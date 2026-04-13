namespace BIA.ToolKit
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.Messaging;
    using MaterialDesignThemes.Wpf;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Code-behind is limited to view wiring (DataContext, console output control,
    /// navigation handling). All business logic lives in MainViewModel.
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

            SourceInitialized += (s, e) =>
            {
                var handle = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            };

            consoleWriterInstance = (ConsoleWriter)consoleWriter;
            consoleWriterInstance.InitOutput(OutputText, OutputTextViewer, this, App.GetService<IDialogService>());

            var createVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            CreateVersionAndOption.DataContext = createVersionAndOptionVM;

            // Wire shared ProjectSelector to the singleton ProjectViewModel
            var projectViewModel = App.GetService<ProjectViewModel>();
            SharedProjectSelector.DataContext = projectViewModel;

            ViewModel = App.GetService<MainViewModel>();
            ViewModel.CreateVersionAndOptionVM = createVersionAndOptionVM;
            DataContext = ViewModel;

            // Navigate to Configuration page when repos are invalid
            WeakReferenceMessenger.Default.Register<NavigateToConfigTabMessage>(this, (r, m) => Dispatcher.BeginInvoke((Action)(() => NavConfig.IsChecked = true)));
            WeakReferenceMessenger.Default.Register<OpenDefaultTeamSettingsMessage>(this, (r, m) => OnOpenDefaultTeamSettings(m.ViewModel));

            // Auto-expand output panel when a busy operation starts
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Trigger initial navigation after window is loaded
            Loaded += (_, _) => OnNavChanged(null, null);

            // Read IsDarkTheme directly from persisted settings because
            // SettingsService.Load() hasn't been called yet at this point.
            ApplyTheme(Properties.Settings.Default.IsDarkTheme);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy) && ViewModel.IsBusy)
            {
                ExpandOutputPanel();
            }
        }

        #region Navigation

        private void OnNavChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            // Hide all pages
            PageConfig.Visibility = Visibility.Collapsed;
            PageCreate.Visibility = Visibility.Collapsed;
            ModifyProject.Visibility = Visibility.Collapsed;
            GenerateProject.Visibility = Visibility.Collapsed;

            // Show selected page
            if (NavConfig.IsChecked == true)
                PageConfig.Visibility = Visibility.Visible;
            else if (NavCreation.IsChecked == true)
                PageCreate.Visibility = Visibility.Visible;
            else if (NavMigration.IsChecked == true)
                ModifyProject.Visibility = Visibility.Visible;
            else if (NavGeneration.IsChecked == true)
                GenerateProject.Visibility = Visibility.Visible;

            // Show shared ProjectSelector for Migration/Generation only
            SharedProjectSelector.Visibility = (NavMigration.IsChecked == true || NavGeneration.IsChecked == true)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Validate repository configuration when leaving Config
            // Guard: ViewModel.InitAsync() may not have run yet at first Loaded
            if (NavConfig.IsChecked != true && ViewModel?.IsInitialized == true)
                ViewModel.EnsureValidRepositoriesConfiguration();
        }

        #endregion

        #region Output Panel

        private void ToggleOutputPanel(object sender, MouseButtonEventArgs e)
        {
            if (OutputTextViewer.Visibility == Visibility.Visible)
                CollapseOutputPanel();
            else
                ExpandOutputPanel();
        }

        private void ExpandOutputPanel()
        {
            OutputTextViewer.Visibility = Visibility.Visible;
            ChevronRotation.Angle = 90;
        }

        private void CollapseOutputPanel()
        {
            OutputTextViewer.Visibility = Visibility.Collapsed;
            ChevronRotation.Angle = 0;
        }

        #endregion

        private void OnOpenDefaultTeamSettings(object viewModel)
        {
            if (viewModel is not VersionAndOptionViewModel vm)
                return;

            var window = new Dialogs.DefaultTeamSettingsWindow(
                vm.DefaultTeamName, vm.DefaultTeamNamePlural, vm.DefaultTeamDomainName)
                { Owner = this };

            if (window.ShowDialog() == true)
            {
                vm.DefaultTeamName = window.ViewModel.DefaultTeamName;
                vm.DefaultTeamNamePlural = window.ViewModel.DefaultTeamNamePlural;
                vm.DefaultTeamDomainName = window.ViewModel.DefaultTeamDomainName;
            }
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
                theme.SetPrimaryColor(Color.FromRgb(0x8B, 0xC3, 0x4A)); // MaterialDesign LightGreen 500
                theme.SetSecondaryColor(Color.FromRgb(0xCD, 0xDC, 0x39)); // MaterialDesign Lime 500
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

        #region WinAPI — constrain maximized window to work area (exclude taskbar)

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                var monitor = MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    GetMonitorInfo(monitor, ref monitorInfo);
                    var work = monitorInfo.rcWork;
                    var monitorRect = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = work.Left - monitorRect.Left;
                    mmi.ptMaxPosition.Y = work.Top - monitorRect.Top;
                    mmi.ptMaxSize.X = work.Right - work.Left;
                    mmi.ptMaxSize.Y = work.Bottom - work.Top;
                }
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMaxTrackSize;
            public POINT ptMinTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        #endregion
    }
}
