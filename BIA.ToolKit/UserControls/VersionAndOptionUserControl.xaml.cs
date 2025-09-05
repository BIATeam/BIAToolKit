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
            if (vm?.WorkTemplate?.RepositorySettings != null)
            {
                if (vm.WorkTemplate.Version == "VX.Y.Z")
                {
                    vm.WorkTemplate.VersionFolderPath = vm.WorkTemplate.RepositorySettings.RootFolderPath;
                }
                else
                {
                    vm.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(vm.WorkTemplate.RepositorySettings, vm.WorkTemplate.Version);
                }
            }
        }

        public void refreshConfig()
        {
            var listCompanyFiles = new List<WorkRepository>();
            var listWorkTemplates = new List<WorkRepository>();


            if (settingsService.Settings.CustomRepoTemplates?.Count > 0)
            {
                foreach (var repositorySettings in settingsService.Settings.CustomRepoTemplates)
                {
                    AddTemplatesVersion(listWorkTemplates, repositorySettings);
                }
            }

            if (Directory.Exists(settingsService.Settings.BIATemplateRepository.RootFolderPath))
            {
                AddTemplatesVersion(listWorkTemplates, settingsService.Settings.BIATemplateRepository);
                listWorkTemplates.Add(new WorkRepository(settingsService.Settings.BIATemplateRepository, "VX.Y.Z"));
            }

            vm.WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
            if (listWorkTemplates.Count >= 2)
            {
                vm.WorkTemplate = listWorkTemplates[listWorkTemplates.Count - 2];
            }

            vm.SettingsUseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            vm.UseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            if (settingsService.Settings.UseCompanyFiles)
            {
                AddTemplatesVersion(listCompanyFiles, settingsService.Settings.CompanyFilesRepository);
                vm.WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (vm.WorkCompanyFiles.Count >= 1)
                {
                    vm.WorkCompanyFile = vm.GetWorkCompanyFile(vm.WorkTemplate.Version);
                }
            }
        }

        private void AddTemplatesVersion(List<WorkRepository> WorkTemplates, IRepositorySettings repositorySettings)
        {
            if (repositorySettings.Versioning == VersioningType.Folder)
            {
                if (Directory.Exists(repositorySettings.RootFolderPath))
                {
                    DirectoryInfo di = new DirectoryInfo(repositorySettings.RootFolderPath);
                    //  an array representing the files in the current directory.
                    DirectoryInfo[] versionDirectories = di.GetDirectories("V*.*.*", SearchOption.TopDirectoryOnly);
                    // Print out the names of the files in the current directory.
                    foreach (DirectoryInfo dir in versionDirectories)
                    {
                        //Add and select the last added
                        WorkTemplates.Add(new WorkRepository(repositorySettings, dir.Name));
                    }
                }
            }
            else
            {
                List<string> versions = gitService.GetTags(repositorySettings.RootFolderPath).OrderBy(q => q).ToList();

                foreach (string version in versions)
                {
                    WorkTemplates.Add(new WorkRepository(repositorySettings, version));
                }
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(async () =>
            {
                //vm.UseCompanyFiles = vm.WorkTemplate?.RepositorySettings?.Name == "BIATemplate";
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
