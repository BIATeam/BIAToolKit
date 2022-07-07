namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Application.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

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
            services.AddSingleton<IConsoleWriter, ConsoleWriter> ();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<Configuration>();
            services.AddSingleton<GitService>();
            services.AddSingleton<ProjectCreatorService>();
            services.AddSingleton<GenerateFilesService>();
            services.AddSingleton<CSharpParserService>();
        }
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }
    }
}
