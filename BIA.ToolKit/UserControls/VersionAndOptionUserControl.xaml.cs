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
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        GitService gitService;
        RepositoryService repositoryService;
        private SettingsService settingsService;
        private string currentProjectPath;
        private UIEventBroker uiEventBroker;
        private IConsoleWriter consoleWriter;
        private List<FeatureSetting> OriginFeatureSettings;
        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            vm = (VersionAndOptionViewModel)base.DataContext;
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker)
        {
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.settingsService = settingsService;
            this.uiEventBroker = uiEventBroker;
            this.consoleWriter = consoleWriter;
            vm.Inject(repositoryService, consoleWriter, uiEventBroker);

            uiEventBroker.OnSettingsUpdated += UiEventBroker_OnSettingsUpdated;
            uiEventBroker.OnRepositoryViewModelReleaseDataUpdated += UiEventBroker_OnRepositoryViewModelReleaseDataUpdated;
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
            currentProjectPath = path;
            LoadfeatureSetting();

            if (originFeatureSettings != null)
            {
                uiEventBroker.OnOriginFeatureSettingsChanged -= UiEventBroker_OnOriginFeatureSettingsChanged;
                uiEventBroker.OnOriginFeatureSettingsChanged += UiEventBroker_OnOriginFeatureSettingsChanged;
                OriginFeatureSettings = [.. originFeatureSettings];
            }

            LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        private void UiEventBroker_OnOriginFeatureSettingsChanged(List<FeatureSetting> featureSettings)
        {
            OriginFeatureSettings = featureSettings;
            LoadVersionAndOption(false, false);
        }

        public void LoadVersionAndOption(bool mapCompanyFileVersion, bool mapFrameworkVersion)
        {
            if (string.IsNullOrWhiteSpace(currentProjectPath))
                return;

            string projectGenerationFile = Path.Combine(currentProjectPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
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
            List<FeatureSetting> projectFeatureSettings = FeatureSettingHelper.Get(currentProjectPath);

            if (featureSettings?.Count > 0 && projectFeatureSettings?.Count > 0)
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

            featureSettings ??= [];
            var featureSettingViewModels = new ObservableCollection<FeatureSettingViewModel>(featureSettings.Select(x => new FeatureSettingViewModel(x)));
            foreach (FeatureSettingViewModel featureSettingViewModel in featureSettingViewModels)
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
                    vm.WorkTemplate.VersionFolderPath = await repositoryService.PrepareVersionFolder(vm.WorkTemplate.Repository, vm.WorkTemplate.Version);
                }
            }
        }

        private void RefreshConfiguration()
        {
            var listCompanyFiles = new List<WorkRepository>();
            var listWorkTemplates = new List<WorkRepository>();

            foreach (IRepository repository in settingsService.Settings.TemplateRepositories.Where(r => r.UseRepository))
            {
                AddTemplatesVersion(listWorkTemplates, repository);
            }

            bool hasVersionXYZ = false;
            IRepository repositoryVersionXYZ = settingsService.Settings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
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

            vm.SettingsUseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            vm.UseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            if (settingsService.Settings.UseCompanyFiles)
            {
                foreach (IRepository repository in settingsService.Settings.CompanyFilesRepositories.Where(r => r.UseRepository))
                {
                    AddTemplatesVersion(listCompanyFiles, repository);
                }
                vm.WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (vm.WorkCompanyFiles.Count >= 1 && vm.WorkTemplate is not null)
                {
                    vm.WorkCompanyFile = vm.GetWorkCompanyFile(vm.WorkTemplate.Version);
                }
            }
        }

        private static void AddTemplatesVersion(List<WorkRepository> WorkTemplates, IRepository repository)
        {
            foreach (Release release in repository.Releases)
            {
                WorkTemplates.Add(new WorkRepository(repository, release.Name));
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await FillVersionFolderPathAsync();
                LoadfeatureSetting();
                LoadVersionAndOption(false, false);
                if (OriginFeatureSettings is null)
                {
                    uiEventBroker.NotifyOriginFeatureSettingsChanged([.. vm.FeatureSettings.Select(x => x.FeatureSetting)]);
                }
            });
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadVersionAndOption(false, false);
        }
    }
}
