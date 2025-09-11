namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;

    public class MainViewModel : ObservableObject
    {
        private readonly Version applicationVersion;
        private readonly UIEventBroker eventBroker;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private bool firstTimeSettingsUpdated = true;

        public MainViewModel(Version applicationVersion, UIEventBroker eventBroker, SettingsService settingsService, GitService gitService, IConsoleWriter consoleWriter)
        {
            this.applicationVersion = applicationVersion;
            this.eventBroker = eventBroker;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            eventBroker.OnSettingsUpdated += EventBroker_OnSettingsUpdated;
        }

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        public ObservableCollection<RepositoryViewModel> TemplateRepositories { get; } = [];
        public ObservableCollection<RepositoryViewModel> CompanyFilesRepositories { get; } = [];

        private RepositoryViewModel toolkitRepository;
        public RepositoryViewModel ToolkitRepository
        {
            get => toolkitRepository;
            set
            {
                toolkitRepository = value;
                RaisePropertyChanged(nameof(ToolkitRepository));
            }
        }

        public ICommand AddTemplateRepositoryCommand => new RelayCommand((_) => AddTemplateRepository());

        public ICommand AddCompanyFilesRepositoryCommand => new RelayCommand((_) => AddCompanyFilesRepository());

        public ICommand EditToolkitRepositoryCommand => new RelayCommand<RepositoryFolderViewModel>(EditToolkitRepository);

        public ICommand EditTemplateRepositoryCommand => new RelayCommand<RepositoryFolderViewModel>(EditTemplateRepository);

        public ICommand EditCompanyFilesRepositoryCommand => new RelayCommand<RepositoryFolderViewModel>(EditCompanyFilesRepository);

        public ICommand RemoveTemplateRepositoryCommand => new RelayCommand<RepositoryFolderViewModel>(RemoveTemplateRepository);

        public ICommand RemoveCompanyFilesRepositoryCommand => new RelayCommand<RepositoryFolderViewModel>(RemoveCompanyFilesRepository);

        private void AddTemplateRepository()
        {
            var repository = CreateRepository();
            if (repository is null)
                return;

            TemplateRepositories.Add(repository);
            
        }

        private void AddCompanyFilesRepository()
        {
            var repository = CreateRepository();
            if (repository is null)
                return;

            CompanyFilesRepositories.Add(repository);
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private RepositoryViewModel CreateRepository()
        {
            return default;
        }

        private void RemoveTemplateRepository(RepositoryFolderViewModel repository)
        {
            TemplateRepositories.Remove(repository);
            settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        private void RemoveCompanyFilesRepository(RepositoryFolderViewModel repository)
        {
            TemplateRepositories.Remove(repository);
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void EditTemplateRepository(RepositoryViewModel repository)
        {
            if (EditRepository(repository))
                settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        private void EditCompanyFilesRepository(RepositoryViewModel repository)
        {
            if (EditRepository(repository))
                settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void EditToolkitRepository(RepositoryViewModel repository)
        {
            if(EditRepository(repository))
                settingsService.SetToolkitRepository(ToolkitRepository.Model);
        }

        private bool EditRepository(RepositoryViewModel repository)
        {
            return true;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            if (firstTimeSettingsUpdated)
            {
                foreach (var repository in settings.TemplateRepositories)
                {
                    if (repository is RepositoryGit repositoryGit)
                    {
                        TemplateRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, eventBroker, consoleWriter));
                    }

                    if (repository is RepositoryFolder repositoryFolder)
                    {
                        TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, eventBroker, consoleWriter));
                    }
                }

                foreach (var repository in settings.CompanyFilesRepositories)
                {
                    if (repository is RepositoryGit repositoryGit)
                    {
                        CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, eventBroker, consoleWriter));
                    }

                    if (repository is RepositoryFolder repositoryFolder)
                    {
                        CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, eventBroker, consoleWriter));
                    }
                }

                ToolkitRepository = settings.ToolkitRepository switch
                {
                    RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, eventBroker, consoleWriter),
                    RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, eventBroker, consoleWriter),
                    _ => throw new NotImplementedException()
                };

                firstTimeSettingsUpdated = false;
                TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
                CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
            }

            RaisePropertyChanged(nameof(Settings_RootProjectsPath));
            RaisePropertyChanged(nameof(Settings_CreateCompanyName));
            RaisePropertyChanged(nameof(Settings_UseCompanyFiles));
            RaisePropertyChanged(nameof(Settings_AutoUpdate));
            RaisePropertyChanged(nameof(ToolkitRepository));
        }

        public string Settings_RootProjectsPath
        {
            get { return settingsService.Settings.CreateProjectRootProjectsPath; }
            set 
            {
                if (settingsService.Settings.CreateProjectRootProjectsPath != value)
                {
                    settingsService.SetCreateProjectRootProjectPath(value);
                }
            }
        }

        public string Settings_CreateCompanyName
        {
            get { return settingsService.Settings.CreateCompanyName; }
            set
            {
                if (settingsService.Settings.CreateCompanyName != value)
                {
                    settingsService.SetCreateCompanyName(value);
                }
            }
        }

        public bool Settings_UseCompanyFiles
        {
            get { return settingsService.Settings.UseCompanyFiles; }
            set
            {
                if (settingsService.Settings.UseCompanyFiles != value)
                {
                    settingsService.SetUseCompanyFiles(value);
                }
            }
        }

        public bool Settings_AutoUpdate
        {
            get => settingsService.Settings.AutoUpdate;
            set
            {
                if (settingsService.Settings.AutoUpdate != value)
                {
                    settingsService.SetAutoUpdate(value);
                }
            }
        }

        public string ApplicationVersion => $"V{applicationVersion.Major}.{applicationVersion.Minor}.{applicationVersion.Build}";

        private bool _updateAvailable;
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                _updateAvailable = value;
                RaisePropertyChanged(nameof(UpdateAvailable));
            }
        }
    }
}
