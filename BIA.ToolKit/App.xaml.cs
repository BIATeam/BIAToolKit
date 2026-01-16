namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Domain.Services;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Infrastructure.Services;
    using BIA.ToolKit.UserControls;
    using CommunityToolkit.Mvvm.Messaging;
    using Microsoft.Extensions.DependencyInjection;
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
        private ServiceProvider serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Infrastructure Services
            services.AddSingleton<IFileSystemService, FileSystemService>();
            
            // Application Services with Interfaces
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
            services.AddSingleton<IRepositoryService, RepositoryService>();
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<IProjectCreatorService, ProjectCreatorService>();
            services.AddSingleton<IZipParserService, ZipParserService>();
            
            // Other Application Services
            services.AddSingleton<GenerateFilesService>();
            services.AddSingleton<CSharpParserService>();
            services.AddSingleton<GenerateCrudService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<FileGeneratorService>();
            services.AddSingleton<UpdateService>();
            
            // Messaging
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<UIEventBroker>(); // Keep for now during migration
            
            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ModifyProjectViewModel>();
            services.AddTransient<DtoGeneratorViewModel>();
            services.AddTransient<OptionGeneratorViewModel>();
            services.AddTransient<VersionAndOptionViewModel>();
            services.AddTransient<RepositoryFormViewModel>();
            // RepositoryGitViewModel and RepositoryFolderViewModel are created manually in MainViewModel
            services.AddTransient<RepositoriesSettingsVM>();
            services.AddTransient<RepositorySettingsVM>();
            
            // UserControls
            services.AddTransient<CRUDGeneratorUC>();
            services.AddTransient<DtoGeneratorUC>();
            services.AddTransient<ModifyProjectUC>();
            services.AddTransient<OptionGeneratorUC>();
            services.AddTransient<VersionAndOptionUserControl>();
            services.AddTransient<RepositoryResumeUC>();
            services.AddTransient<LabeledField>();
            
            // Dialogs
            services.AddTransient<RepositoryFormUC>();
            services.AddTransient<CustomRepoTemplateUC>();
            services.AddTransient<CustomsRepoTemplateUC>();
            services.AddTransient<LogDetailUC>();
            
            // Main Window
            services.AddSingleton<MainWindow>();
            
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
            await mainWindow.Init();
        }
    }
}
