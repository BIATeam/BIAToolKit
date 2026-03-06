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
    using BIA.ToolKit.Application.ViewModel.Interfaces;
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
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker, IMessenger messenger)
        {
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.settingsService = settingsService;
            this.uiEventBroker = uiEventBroker;
            this.consoleWriter = consoleWriter;

            vm = new VersionAndOptionViewModel(messenger, repositoryService, consoleWriter, uiEventBroker, settingsService);
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();

            uiEventBroker.OnRepositoryViewModelReleaseDataUpdated += UiEventBroker_OnRepositoryViewModelReleaseDataUpdated;
        }

        private void UiEventBroker_OnRepositoryViewModelReleaseDataUpdated(RepositoryViewModel repository)
        {
            vm.RefreshConfiguration();
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
                uiEventBroker.OnOriginFeatureSettingsChanged -= UiEventBroker_OnOriginFeatureSettingsChanged;
                uiEventBroker.OnOriginFeatureSettingsChanged += UiEventBroker_OnOriginFeatureSettingsChanged;
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

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await this.FillVersionFolderPathAsync();
                this.LoadfeatureSetting();
                this.LoadVersionAndOption(false, false);
                if (OriginFeatureSettings is null)
                {
                    uiEventBroker.NotifyOriginFeatureSettingsChanged(vm.FeatureSettings.Select(x => x.FeatureSetting).ToList());
                }
            });
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.LoadVersionAndOption(false, false);
        }
    }
}
