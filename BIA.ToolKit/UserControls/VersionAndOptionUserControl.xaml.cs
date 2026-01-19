namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Mapper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        GitService gitService;
        RepositoryService repositoryService;
        private SettingsService settingsService;
        private string currentProjectPath;
        private IMessenger messenger;
        private IConsoleWriter consoleWriter;
        private List<FeatureSetting> OriginFeatureSettings;
        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            vm = (VersionAndOptionViewModel)base.DataContext;
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, IMessenger messenger)
        {
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.settingsService = settingsService;
            this.messenger = messenger;
            this.consoleWriter = consoleWriter;
            vm.Inject(repositoryService, consoleWriter, messenger);

            messenger.Register<SettingsUpdatedMessage>(this, (r, m) => UiEventBroker_OnSettingsUpdated(m.Settings));
            messenger.Register<RepositoryViewModelReleaseDataUpdatedMessage>(this, (r, m) => UiEventBroker_OnRepositoryViewModelReleaseDataUpdated(m.Repository));
        }

        private void UiEventBroker_OnRepositoryViewModelReleaseDataUpdated(RepositoryViewModel repository)
        {
            RefreshConfiguration();
        }

        private void UiEventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            RefreshConfiguration();
            vm.SettingsUseCompanyFiles = settings.UseCompanyFiles;
        }

        public void SelectVersion(string version)
        {
            vm.WorkTemplate = vm.WorkTemplates.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            this.currentProjectPath = path;
            this.LoadfeatureSetting();

            if (originFeatureSettings != null)
            {
                messenger.Unregister<OriginFeatureSettingsChangedMessage>(this);
                messenger.Register<OriginFeatureSettingsChangedMessage>(this, (r, m) => UiEventBroker_OnOriginFeatureSettingsChanged(m.FeatureSettings));
                OriginFeatureSettings = new List<FeatureSetting>(originFeatureSettings);
            }

            this.LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        private void UiEventBroker_OnOriginFeatureSettingsChanged(List<FeatureSetting> featureSettings)
        {
            OriginFeatureSettings = featureSettings;
            LoadVersionAndOption(false, false);
        }

        public void LoadVersionAndOption(bool mapCompanyFileVersion, bool mapFrameworkVersion)
        {
            if (string.IsNullOrWhiteSpace(this.currentProjectPath))
                return;

            string projectGenerationFile = Path.Combine(this.currentProjectPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
            if (!File.Exists(projectGenerationFile))
                return;

            try
            {
                VersionAndOptionDto versionAndOptionDto = CommonTools.DeserializeJsonFile<VersionAndOptionDto>(projectGenerationFile);
                VersionAndOptionMapper.DtoToModel(versionAndOptionDto, vm, mapCompanyFileVersion, mapFrameworkVersion, OriginFeatureSettings);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error when reading {projectGenerationFile} : {ex.Message}", "red");
            }
        }

        private void LoadfeatureSetting()
        {
            List<FeatureSetting> featureSettings = FeatureSettingHelper.Get(vm.WorkTemplate?.VersionFolderPath);
            List<FeatureSetting> projectFeatureSettings = FeatureSettingHelper.Get(this.currentProjectPath);

            if (featureSettings?.Any() == true && projectFeatureSettings?.Any() == true)
            {
                foreach (FeatureSetting featureSetting in featureSettings)
                {
                    FeatureSetting projectFeatureSetting = projectFeatureSettings.Find(x => x.Id == featureSetting.Id);

                    if (projectFeatureSetting != null)
                    {
                        featureSetting.IsSelected = projectFeatureSetting.IsSelected;
                    }
                }
            }

            featureSettings = featureSettings ?? [];
            var featureSettingViewModels = new ObservableCollection<FeatureSettingViewModel>(featureSettings.Select(x => new FeatureSettingViewModel(x)));
            foreach (var featureSettingViewModel in featureSettingViewModels)
            {
                if (featureSettingViewModel.FeatureSetting.DisabledFeatures.Count != 0)
                {
                    featureSettingViewModel.DisabledFeatures = string.Join(", ", featureSettings
                        .Where(x => featureSettingViewModel.FeatureSetting.DisabledFeatures.Contains(x.Id))
                        .Select(x => x.DisplayName));
                }
            }

            vm.FeatureSettings = featureSettingViewModels;
        }

        public async Task FillVersionFolderPathAsync()
        {
            if (vm?.WorkTemplate?.Repository != null)
            {
                if (vm.WorkTemplate.Version == "VX.Y.Z")
                {
                    vm.WorkTemplate.VersionFolderPath = vm.WorkTemplate.Repository.LocalPath;
                }
                else
                {
                    vm.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(vm.WorkTemplate.Repository, vm.WorkTemplate.Version);
                }
            }
        }

        private void RefreshConfiguration()
        {
            var listWorkTemplates = LoadRepositoriesFromSettings(settingsService.Settings.TemplateRepositories);

            var hasVersionXYZ = false;
            var repositoryVersionXYZ = settingsService.Settings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
            if (repositoryVersionXYZ is not null)
            {
                listWorkTemplates.Add(new WorkRepository(repositoryVersionXYZ, "VX.Y.Z"));
                hasVersionXYZ = true;
            }

            vm.WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
            if (listWorkTemplates.Count >= 1)
            {
                vm.WorkTemplate = hasVersionXYZ && listWorkTemplates.Count >= 2 ? listWorkTemplates[^2] : listWorkTemplates[^1];
            }

            bool useCompanyFiles = settingsService.Settings.UseCompanyFiles;
            vm.SettingsUseCompanyFiles = useCompanyFiles;
            vm.UseCompanyFiles = useCompanyFiles;
            
            if (useCompanyFiles)
            {
                var listCompanyFiles = LoadRepositoriesFromSettings(settingsService.Settings.CompanyFilesRepositories);
                vm.WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (vm.WorkCompanyFiles.Count >= 1 && vm.WorkTemplate is not null)
                {
                    vm.WorkCompanyFile = vm.GetWorkCompanyFile(vm.WorkTemplate.Version);
                }
            }
        }

        private List<WorkRepository> LoadRepositoriesFromSettings(IEnumerable<IRepository> repositories)
        {
            var workRepositories = new List<WorkRepository>();
            foreach (var repository in repositories.Where(r => r.UseRepository))
            {
                AddTemplatesVersion(workRepositories, repository);
            }
            return workRepositories;
        }

        private void AddTemplatesVersion(List<WorkRepository> WorkTemplates, IRepository repository)
        {
            foreach (var release in repository.Releases)
            {
                WorkTemplates.Add(new WorkRepository(repository, release.Name));
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                await this.FillVersionFolderPathAsync();
                this.LoadfeatureSetting();
                this.LoadVersionAndOption(false, false);
                if (OriginFeatureSettings is null)
                {
                    messenger.Send(new OriginFeatureSettingsChangedMessage(vm.FeatureSettings.Select(x => x.FeatureSetting).ToList()));
                }
            }));
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.LoadVersionAndOption(false, false);
        }
    }
}
