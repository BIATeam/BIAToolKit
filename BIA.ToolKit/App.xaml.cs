namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.ViewModels;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost host;

        public App()
        {
            host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
            services.AddSingleton<UIEventBroker>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<GitService>();
            services.AddSingleton<ProjectCreatorService>();
            services.AddSingleton<GenerateFilesService>();
            services.AddSingleton<CSharpParserService>();
            services.AddSingleton<ZipParserService>();
            services.AddSingleton<GenerateCrudService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<FileGeneratorService>();
            services.AddSingleton<UpdateService>();
            services.AddLogging();

            // ViewModels
            services.AddTransient<LogDetailViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
            services.AddTransient<LogDetailUC>();
        }

        public static T GetService<T>() where T : class
        {
            return ((App)Current).host.Services.GetRequiredService<T>();
        }
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            if (ToolKit.Properties.Settings.Default.ApplicationUpdated)
            {
                ToolKit.Properties.Settings.Default.Upgrade();

                ToolKit.Properties.Settings.Default.ApplicationUpdated = false;
                ToolKit.Properties.Settings.Default.Save();
            }

            await host.StartAsync();

            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            await mainWindow.Init();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (host)
            {
                await host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
