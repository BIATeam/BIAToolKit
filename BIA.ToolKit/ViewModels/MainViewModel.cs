namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using Octokit;

    public partial class MainViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly Version applicationVersion;
        private readonly IMessenger messenger;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private bool firstTimeSettingsUpdated = true;
        private bool waitAddTemplateRepository;
        private bool waitAddCompanyFilesRepository;

        public MainViewModel(Version applicationVersion, IMessenger messenger, SettingsService settingsService, GitService gitService, IConsoleWriter consoleWriter)
        {
            this.applicationVersion = applicationVersion;
            this.messenger = messenger;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            
            messenger.Register<SettingsUpdatedMessage>(this, (r, m) => EventBroker_OnSettingsUpdated(m.Settings));
            messenger.Register<RepositoryViewModelChangedMessage>(this, (r, m) => EventBroker_OnRepositoryChanged(m.OldRepository, m.NewRepository));
            messenger.Register<RepositoryViewModelDeletedMessage>(this, (r, m) => EventBroker_OnRepositoryViewModelDeleted(m.Repository));
            messenger.Register<RepositoryViewModelAddedMessage>(this, (r, m) => EventBroker_OnRepositoryViewModelAdded(m.Repository));
            
            OpenToolkitRepositorySettingsCommand = new RelayCommand(OpenToolkitRepositorySettings);
            AddTemplateRepositoryCommand = new RelayCommand(AddTemplateRepository);
            AddCompanyFilesRepositoryCommand = new RelayCommand(AddCompanyFilesRepository);
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

            if(repository.Model.RepositoryType == Domain.RepositoryType.Git && repository.Model is IRepositoryGit repositoryGit)
            {
                messenger.Send(new ExecuteActionWithWaiterMessage(async () => await gitService.Synchronize(repositoryGit)));
            }
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

            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                consoleWriter.AddMessageLine($"Deleting repository {repository.Name}...", "pink");
                await Task.Run(repository.Model.Clean);
                consoleWriter.AddMessageLine("Repository deleted", "green");
            }));
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

        private void OpenToolkitRepositorySettings()
        {
            messenger.Send(new OpenRepositoryFormRequestMessage(ToolkitRepository, RepositoryFormMode.Edit));
        }

        public IRelayCommand OpenToolkitRepositorySettingsCommand { get; }

        public IRelayCommand AddTemplateRepositoryCommand { get; }

        public IRelayCommand AddCompanyFilesRepositoryCommand { get; }

        private void AddTemplateRepository()
        {
            waitAddTemplateRepository = true;
            waitAddCompanyFilesRepository = false;
            var repo = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, messenger, consoleWriter);
            messenger.Send(new OpenRepositoryFormRequestMessage(repo, RepositoryFormMode.Create));
        }

        private void AddCompanyFilesRepository()
        {
            waitAddCompanyFilesRepository = true;
            waitAddTemplateRepository = false;
            var repo = new RepositoryGitViewModel(RepositoryGit.CreateEmpty(), gitService, messenger, consoleWriter);
            messenger.Send(new OpenRepositoryFormRequestMessage(repo, RepositoryFormMode.Create));
        }

        public ObservableCollection<RepositoryViewModel> TemplateRepositories { get; } = [];
        public ObservableCollection<RepositoryViewModel> CompanyFilesRepositories { get; } = [];

        private RepositoryViewModel toolkitRepository;
        public RepositoryViewModel ToolkitRepository
        {
            get => toolkitRepository;
            set => SetProperty(ref toolkitRepository, value);
        }

        public void UpdateRepositories(IBIATKSettings settings)
        {
            TemplateRepositories.CollectionChanged -= TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged -= CompanyFilesRepositories_CollectionChanged;

            TemplateRepositories.Clear();
            foreach (var repository in settings.TemplateRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    var viewModel = new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter) { CanBeVersionXYZ = true };
                    TemplateRepositories.Add(viewModel);
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    TemplateRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter));
                }
            }

            CompanyFilesRepositories.Clear();
            foreach (var repository in settings.CompanyFilesRepositories)
            {
                if (repository is RepositoryGit repositoryGit)
                {
                    CompanyFilesRepositories.Add(new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter));
                }

                if (repository is RepositoryFolder repositoryFolder)
                {
                    CompanyFilesRepositories.Add(new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter));
                }
            }

            ToolkitRepository = settings.ToolkitRepository switch
            {
                RepositoryGit repositoryGit => new RepositoryGitViewModel(repositoryGit, gitService, messenger, consoleWriter),
                RepositoryFolder repositoryFolder => new RepositoryFolderViewModel(repositoryFolder, gitService, messenger, consoleWriter),
                _ => throw new NotImplementedException()
            };
            ToolkitRepository.IsVisibleCompanyName = false;
            ToolkitRepository.IsVisibleProjectName = false;

            TemplateRepositories.CollectionChanged += TemplateRepositories_CollectionChanged;
            CompanyFilesRepositories.CollectionChanged += CompanyFilesRepositories_CollectionChanged;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            if (firstTimeSettingsUpdated)
            {
                UpdateRepositories(settings);
                firstTimeSettingsUpdated = false;
            }

            OnPropertyChanged(nameof(Settings_RootProjectsPath));
            OnPropertyChanged(nameof(Settings_CreateCompanyName));
            OnPropertyChanged(nameof(Settings_UseCompanyFiles));
            OnPropertyChanged(nameof(Settings_AutoUpdate));
            OnPropertyChanged(nameof(ToolkitRepository));
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

        [ObservableProperty]
        private bool _updateAvailable;
    }
}
