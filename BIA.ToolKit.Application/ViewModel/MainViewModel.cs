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
    using Octokit;

    public class MainViewModel : ObservableObject
    {
        private readonly Version applicationVersion;
        private readonly UIEventBroker eventBroker;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private bool firstTimeSettingsUpdated = true;
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;

        public MainViewModel(Version applicationVersion, UIEventBroker eventBroker, SettingsService settingsService, GitService gitService, IConsoleWriter consoleWriter)
        {
            this.applicationVersion = applicationVersion;
            this.eventBroker = eventBroker;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            eventBroker.OnSettingsUpdated += EventBroker_OnSettingsUpdated;
            eventBroker.OnRepositoryViewModelChanged += EventBroker_OnRepositoryChanged;
            eventBroker.OnRepositoryViewModelDeleted += EventBroker_OnRepositoryViewModelDeleted;
            eventBroker.OnRepositoryViewModelAdded += EventBroker_OnRepositoryViewModelAdded;
        }

        private void EventBroker_OnRepositoryViewModelAdded(RepositoryViewModel repository)
        {
            if (waitAddTemplateRepository)
            {
                TemplateRepositories.Add(repository);
            }

            if (waitAddCompanyFilesRepository)
            {
                CompanyFilesRepositories.Add(repository);
            }

            waitAddTemplateRepository = false;
            waitAddCompanyFilesRepository = false;
        }

        private void EventBroker_OnRepositoryViewModelDeleted(RepositoryViewModel repository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == repository)
                {
                    TemplateRepositories.RemoveAt(i);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == repository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                }
            }
        }

        private void EventBroker_OnRepositoryChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
        {
            for (int i = 0; i < TemplateRepositories.Count; i++)
            {
                if (TemplateRepositories[i] == oldRepository)
                {
                    TemplateRepositories.RemoveAt(i);
                    TemplateRepositories.Insert(i, newRepository);
                }
            }

            for (int i = 0; i < CompanyFilesRepositories.Count; i++)
            {
                if (CompanyFilesRepositories[i] == oldRepository)
                {
                    CompanyFilesRepositories.RemoveAt(i);
                    CompanyFilesRepositories.Insert(i, newRepository);
                }
            }

            if (ToolkitRepository == oldRepository)
            {
                ToolkitRepository = newRepository;
                settingsService.SetToolkitRepository(ToolkitRepository.Model);
            }
        }

        private void CompanyFilesRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetCompanyFilesRepositories(CompanyFilesRepositories.Select(x => x.Model).ToList());
        }

        private void TemplateRepositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settingsService.SetTemplateRepositories(TemplateRepositories.Select(x => x.Model).ToList());
        }

        public ICommand OpenToolkitRepositorySettingsCommand => new RelayCommand((_) => eventBroker.RequestOpenRepositoryForm(ToolkitRepository, RepositoryFormMode.Edit));

        public ICommand AddTemplateRepositoryCommand => new RelayCommand((_) => AddTemplateRepository());

        public ICommand AddCompanyFilesRepositoryCommand => new RelayCommand((_) => AddCompanyFilesRepository());

        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            eventBroker.RequestOpenRepositoryForm(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, eventBroker, consoleWriter), RepositoryFormMode.Create);
        }

        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            eventBroker.RequestOpenRepositoryForm(new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, eventBroker, consoleWriter), RepositoryFormMode.Create);
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
                ToolkitRepository.IsVisibleCompanyName = false;
                ToolkitRepository.IsVisibleProjectName = false;

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
