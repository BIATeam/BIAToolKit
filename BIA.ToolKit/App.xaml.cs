namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.ViewModels;
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
            services.AddScoped<IFileDialogService, FileDialogService>();
            
            // Application Services with Interfaces
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
            services.AddSingleton<IRepositoryService, RepositoryService>();
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<IProjectCreatorService, ProjectCreatorService>();
            services.AddSingleton<IZipParserService, ZipParserService>();
            
            // Application Services - Text Parsing and Dialog
            services.AddScoped<ITextParsingService, TextParsingService>();
            services.AddScoped<IDialogService, DialogService>();
            
            // Other Application Services
            services.AddSingleton<GenerateFilesService>();
            services.AddSingleton<CSharpParserService>();
            services.AddSingleton<GenerateCrudService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<FileGeneratorService>();
            services.AddSingleton<UpdateService>();
            
            // Messaging
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            
            // ViewModels - Phase 4 MVVM Transformation
            services.AddTransient<Application.ViewModel.MainWindowViewModel>(); // Phase 4 Step 27
            services.AddTransient<Application.ViewModel.CRUDGeneratorViewModel>(); // Phase 4 Step 28
            services.AddTransient<Application.ViewModel.OptionGeneratorViewModel>(); // Phase 4 Step 29
            services.AddTransient<Application.ViewModel.DtoGeneratorViewModel>(); // Phase 4 Step 30
            services.AddTransient<Application.ViewModel.ModifyProjectViewModel>(); // Phase 4 Step 31
            services.AddTransient<Application.ViewModel.VersionAndOptionViewModel>(); // Phase 4 Step 32
            
            // Legacy ViewModels (for backward compatibility)
            services.AddTransient<ModifyProjectViewModel>();
            services.AddTransient<DtoGeneratorViewModel>();
            services.AddTransient<OptionGeneratorViewModel>();
            services.AddTransient<VersionAndOptionViewModel>();
            services.AddTransient<RepositoryFormViewModel>();
            // RepositoryGitViewModel and RepositoryFolderViewModel are created manually
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
            if (mainWindow?.ViewModel is not null)
            {
                await mainWindow.ViewModel.InitializeAsync();
            }
        }
    }
}
