namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.Reflection;
    using System.Windows;

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
            services.AddSingleton<ISettingsPersistence, UserPreferencesSettingsPersistence>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<GitService>();
            services.AddSingleton<ProjectCreatorService>();
            services.AddSingleton<CSharpParserService>();
            services.AddSingleton<ZipParserService>();
            services.AddSingleton<GenerateCrudService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<FileGeneratorService>();
            services.AddSingleton<UpdateService>();
            services.AddSingleton<TemplateVersionService>();
            services.AddSingleton<FeatureSettingService>();
            services.AddSingleton<Application.Services.DtoMappingService>();
            services.AddSingleton<Application.Services.EntityResolutionService>();
            services.AddSingleton<Application.Services.GenerationHistoryService>();
            services.AddSingleton<Application.Services.RegenerateFeatures.RegenerateFeaturesDiscoveryService>();
            services.AddSingleton<Application.Services.RegenerateFeatures.FeatureMigrationGeneratorService>();
            services.AddSingleton<Application.Services.RegenerateFeatures.RegenerationOrchestrationService>();
            services.AddSingleton<ProjectViewModel>();
            services.AddLogging();

            // ViewModels
            services.AddTransient<LogDetailViewModel>();
            services.AddTransient<VersionAndOptionViewModel>();
            services.AddTransient<ModifyProjectViewModel>();
            services.AddTransient<OptionGeneratorViewModel>();
            services.AddTransient<DtoGeneratorViewModel>();
            services.AddTransient<CRUDGeneratorViewModel>();
            services.AddTransient<RegenerateFeaturesViewModel>();
            services.AddTransient<MainViewModel>(sp => new MainViewModel(
                Assembly.GetExecutingAssembly().GetName().Version,
                sp.GetRequiredService<SettingsService>(),
                sp.GetRequiredService<GitService>(),
                sp.GetRequiredService<IConsoleWriter>(),
                sp.GetRequiredService<RepositoryService>(),
                sp.GetRequiredService<ProjectCreatorService>(),
                sp.GetRequiredService<UpdateService>(),
                sp.GetRequiredService<CSharpParserService>(),
                sp.GetRequiredService<IDialogService>()));

            // Infrastructure
            services.AddSingleton<IDialogService, DialogService>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        public static T GetService<T>() where T : class
        {
            return ((App)Current).host.Services.GetRequiredService<T>();
        }
        private async void OnStartup(object sender, StartupEventArgs e)
        {
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
