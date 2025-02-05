﻿namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Services;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Configuration;
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
            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.Show();

            CSharpParserService.RegisterMSBuild(serviceProvider.GetRequiredService<IConsoleWriter>());

            var autoUpdateSetting = ConfigurationManager.AppSettings["AutoUpdate"];
            if (bool.TryParse(autoUpdateSetting, out bool autoUpdate) && autoUpdate)
            {
                var updateService = serviceProvider.GetService<UpdateService>();
                await updateService.CheckForUpdatesAsync();
            }
        }
    }
}
