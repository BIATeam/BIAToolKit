namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using Windows.UI.Input;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        GitService gitService;
        RepositoryService repositoryService;
        FeatureSettingService featureSettingService;
        private SettingsService settingsService;
        private string currentProjectPath;
        private UIEventBroker uiEventBroker;

        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            vm = (VersionAndOptionViewModel)base.DataContext;
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, FeatureSettingService featureSettingService,
                    SettingsService settingsService, UIEventBroker uiEventBroker)
        {
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.featureSettingService = featureSettingService;
            this.settingsService = settingsService;
            this.uiEventBroker = uiEventBroker;
            vm.Inject(repositoryService, consoleWriter);

            uiEventBroker.OnSettingsUpdated += UiEventBroker_OnSettingsUpdated;
        }

        private void UiEventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            vm.SettingsUseCompanyFiles = settings.UseCompanyFiles;
            RefreshConfiguration();
        }

        public void SelectVersion(string version)
        {
            vm.WorkTemplate = vm.WorkTemplates.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion)
        {
            this.currentProjectPath = path;
            this.LoadfeatureSetting();

            this.LoadVersionAndOption(mapCompanyFileVersion);
        }

        private void LoadVersionAndOption(bool mapCompanyFileVersion)
        {
            if (!string.IsNullOrWhiteSpace(this.currentProjectPath))
            {
                string projectGenerationFile = Path.Combine(this.currentProjectPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
                if (File.Exists(projectGenerationFile))
                {
                    VersionAndOptionDto versionAndOptionDto = CommonTools.DeserializeJsonFile<VersionAndOptionDto>(projectGenerationFile);
                    VersionAndOptionMapper.DtoToModel(versionAndOptionDto, vm, mapCompanyFileVersion);
                }
            }
        }

        private void LoadfeatureSetting()
        {
            List<FeatureSetting> featureSettings = this.featureSettingService.Get(vm.WorkTemplate?.VersionFolderPath);
            List<FeatureSetting> projectFeatureSettings = this.featureSettingService.Get(this.currentProjectPath);

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

            featureSettings = featureSettings ?? new List<FeatureSetting>();
            vm.FeatureSettings = new ObservableCollection<FeatureSetting>(featureSettings);
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

        public void RefreshConfiguration()
        {
            var listCompanyFiles = new List<WorkRepository>();
            var listWorkTemplates = new List<WorkRepository>();


            foreach (var repository in settingsService.Settings.TemplateRepositories)
            {
                AddTemplatesVersion(listWorkTemplates, repository);
            }

            vm.WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
            if (listWorkTemplates.Count >= 1)
            {
                vm.WorkTemplate = listWorkTemplates[^1];
            }

            vm.SettingsUseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            vm.UseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            if (settingsService.Settings.UseCompanyFiles)
            {
                foreach (var repository in settingsService.Settings.CompanyFilesRepositories)
                {
                    AddTemplatesVersion(listCompanyFiles, repository);
                }
                vm.WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (vm.WorkCompanyFiles.Count >= 1)
                {
                    vm.WorkCompanyFile = vm.GetWorkCompanyFile(vm.WorkTemplate.Version);
                }
            }
        }

        private void AddTemplatesVersion(List<WorkRepository> WorkTemplates, IRepository repository)
        {
            foreach(var release in repository.Releases)
            {
                WorkTemplates.Add(new WorkRepository(repository, release.Name));
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await this.FillVersionFolderPathAsync();
                this.LoadfeatureSetting();
                this.LoadVersionAndOption(false);
            });
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.LoadVersionAndOption(false);
        }
    }
}
