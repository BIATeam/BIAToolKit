namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
            services.AddSingleton<UIEventBroker>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<GitService>();
            services.AddSingleton<ProjectCreatorService>();
            services.AddSingleton<GenerateFilesService>();
            services.AddSingleton<CSharpParserService>();
            services.AddSingleton<ZipParserService>();
            services.AddSingleton<GenerateCrudService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<FeatureSettingService>();
            services.AddSingleton<FileGeneratorService>();
            services.AddSingleton<UpdateService>();
            services.AddLogging();
        }
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            if (ToolKit.Properties.Settings.Default.ApplicationUpdated)
            {
                ToolKit.Properties.Settings.Default.Upgrade();

                ToolKit.Properties.Settings.Default.ApplicationUpdated = false;
                ToolKit.Properties.Settings.Default.Save();
            }

            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.Show();

            CSharpParserService.RegisterMSBuild(serviceProvider.GetRequiredService<IConsoleWriter>());

            var autoUpdateSetting = ConfigurationManager.AppSettings["AutoUpdate"];
            if (bool.TryParse(autoUpdateSetting, out bool autoUpdate))
            {
                var updateService = serviceProvider.GetService<UpdateService>();
                updateService.SetAppVersion(Assembly.GetExecutingAssembly().GetName().Version);
                if (await updateService.CheckForUpdatesAsync(autoUpdate))
                {
                    var result = MessageBox.Show(
                        $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?",
                        "Update available",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await updateService.DownloadUpdateAsync();
                    }
                }
            }
        }
    }
}
