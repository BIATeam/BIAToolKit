namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Services;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
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
            services.AddSingleton<BiaFrameworkFileGeneratorService>();
            services.AddSingleton<UpdateService>();
            services.AddLogging();
        }
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            if (ToolKit.Properties.Settings.Default.ApplicationUpdated)
            {
                MigratePreviousUserData();

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
                await updateService.CheckForUpdatesAsync(autoUpdate);
            }
        }

        private static void MigratePreviousUserData()
        {
            var currentAppDataPath = System.Windows.Forms.Application.LocalUserAppDataPath;
            var appDataDirectories = new DirectoryInfo(Path.GetDirectoryName(currentAppDataPath))
                .GetDirectories()
                .OrderByDescending(d => d.CreationTime)
                .ToList();

            if (appDataDirectories.Count < 2)
                return;

            var previousVersionAppDataPath = appDataDirectories[1].FullName;
            var previousVersionAppDataFiles = Directory.GetFiles(previousVersionAppDataPath, "*", SearchOption.AllDirectories).ToList();
            foreach (string sourceFile in previousVersionAppDataFiles)
            {
                try
                {
                    var destinationFile = sourceFile.Replace(previousVersionAppDataPath, currentAppDataPath);
                    if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                    File.Copy(sourceFile, destinationFile, false);
                }
                finally { }
            }

            for (int i = 1; i < appDataDirectories.Count; i++)
            {
                try
                {
                    Directory.Delete(appDataDirectories[i].FullName, true);
                }
                finally { }
            }
        }
    }
}
