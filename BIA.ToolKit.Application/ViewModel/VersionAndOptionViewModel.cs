namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public partial class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }
        private readonly RepositoryService repositoryService;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;
        private readonly UIEventBroker eventBroker;

        private bool hasFeature = false;
        private bool areFeatureInitialized = false;
        private string currentProjectPath;
        private List<FeatureSetting> OriginFeatureSettings;

        public VersionAndOptionViewModel(
            RepositoryService repositoryService,
            SettingsService settingsService,
            GitService gitService,
            IConsoleWriter consoleWriter,
            UIEventBroker eventBroker)
        {
            this.repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.eventBroker = eventBroker ?? throw new ArgumentNullException(nameof(eventBroker));

            VersionAndOption = new VersionAndOption();

            // Subscribe to event broker events
            eventBroker.OnSettingsUpdated += OnSettingsUpdated;
            eventBroker.OnRepositoryViewModelReleaseDataUpdated += OnRepositoryViewModelReleaseDataUpdated;
        }

        public ObservableCollection<WorkRepository> WorkTemplates
        {
            get { return VersionAndOption.WorkTemplates; }
            set
            {
                if (VersionAndOption.WorkTemplates != value)
                {
                    VersionAndOption.WorkTemplates = value;
                    OnPropertyChanged(nameof(WorkTemplates));
                }
            }
        }

        public WorkRepository WorkTemplate
        {
            get { return VersionAndOption.WorkTemplate; }
            set
            {
                if (VersionAndOption.WorkTemplate != value)
                {
                    VersionAndOption.WorkTemplate = value;
                    AreFeatureInitialized = false;
                    foreach (var workCompanyFile in WorkCompanyFiles)
                    {
                        if (WorkTemplate?.Version == workCompanyFile.Version)
                        {
                            WorkCompanyFile = workCompanyFile;
                        }
                    }
                    OnPropertyChanged(nameof(WorkTemplate));
                }
            }
        }

        public ObservableCollection<string> Profiles
        {
            get { return VersionAndOption.Profiles; }
            set
            {
                if (VersionAndOption.Profiles != value)
                {
                    VersionAndOption.Profiles = value;
                    OnPropertyChanged(nameof(Profiles));
                }
            }
        }

        public string Profile
        {
            get { return VersionAndOption.Profile; }
            set
            {
                if (VersionAndOption.Profile != value)
                {
                    VersionAndOption.Profile = value;
                    OnPropertyChanged(nameof(Profile));
                }
            }
        }

        public ObservableCollection<WorkRepository> WorkCompanyFiles
        {
            get { return VersionAndOption.WorkCompanyFiles; }
            set
            {
                if (VersionAndOption.WorkCompanyFiles != value)
                {
                    VersionAndOption.WorkCompanyFiles = value;
                    OnPropertyChanged(nameof(WorkCompanyFiles));
                }
            }
        }

        public WorkRepository GetWorkCompanyFile(string version)
        {
            var result = WorkCompanyFiles.FirstOrDefault(x => x.Version == version);
            if (result != null)
                return result;

            return WorkCompanyFiles.LastOrDefault();
        }

        public WorkRepository WorkCompanyFile
        {
            get { return VersionAndOption.WorkCompanyFile; }
            set
            {
                if (VersionAndOption.WorkCompanyFile != value)
                {
                    VersionAndOption.WorkCompanyFile = value;

                    if (WorkCompanyFile != null)
                    {
                        eventBroker.RequestExecuteActionWithWaiter(async () =>
                        {
                            try
                            {
                                WorkCompanyFile.VersionFolderPath = await repositoryService.PrepareVersionFolder(WorkCompanyFile.Repository, WorkCompanyFile.Version);
                                string fileName = WorkCompanyFile.VersionFolderPath + "\\biaCompanyFiles.json";

                                string jsonString = File.ReadAllText(fileName);

                                CFSettings cfSetting = JsonSerializer.Deserialize<CFSettings>(jsonString);

                                var listProfiles = new List<string>();
                                foreach (string profile in cfSetting.Profiles)
                                {
                                    listProfiles.Add(profile);
                                    Profile = profile;
                                }
                                Profiles = new ObservableCollection<string>(listProfiles);

                                var options = new List<CFOption>();
                                foreach (CFOption option in cfSetting.Options)
                                {
                                    option.IsChecked = (!(option?.Default == 0));
                                    options.Add(option);
                                }
                                Options = new ObservableCollection<CFOption>(options);

                            }
                            catch (Exception ex)
                            {
                                consoleWriter.AddMessageLine(ex.Message, "Red");
                            }
                        });
                    }
                    OnPropertyChanged(nameof(WorkCompanyFile));
                }
            }
        }

        private bool settingsUseCompanyFiles;
        public bool SettingsUseCompanyFiles
        {
            get { return settingsUseCompanyFiles; }
            set
            {
                settingsUseCompanyFiles = value;
                OnPropertyChanged(nameof(SettingsUseCompanyFiles));
                OnPropertyChanged(nameof(SettingsNotUseCompanyFiles));
            }
        }

        public bool SettingsNotUseCompanyFiles => !SettingsUseCompanyFiles;


        public bool UseCompanyFiles
        {
            get { return VersionAndOption.UseCompanyFiles; }
            set
            {
                if (VersionAndOption.UseCompanyFiles != value)
                {
                    VersionAndOption.UseCompanyFiles = value;
                    OnPropertyChanged(nameof(UseCompanyFiles));
                    OnPropertyChanged(nameof(NotUseCompanyFiles));
                }
            }
        }

        public bool NotUseCompanyFiles
        {
            get { return !(VersionAndOption.UseCompanyFiles); }
        }

        public ObservableCollection<CFOption> Options
        {
            get { return VersionAndOption.Options; }
            set
            {
                if (VersionAndOption.Options != value)
                {
                    VersionAndOption.Options = value;
                    OnPropertyChanged(nameof(Options));
                }
            }
        }

        public void CheckOptions(List<string> checkedOptions)
        {
            var options = new List<CFOption>();
            foreach (CFOption option in Options)
            {
                option.IsChecked = (checkedOptions?.Any(o => string.Equals(o, option.Key)) == true);
                options.Add(option);
            }
            Options = new ObservableCollection<CFOption>(options);
        }

        private ObservableCollection<FeatureSettingViewModel> featureSettings = [];
        public ObservableCollection<FeatureSettingViewModel> FeatureSettings
        {
            get { return featureSettings; }
            set
            {
                if (featureSettings != value)
                {
                    featureSettings = value ?? [];
                    HasFeature = featureSettings.Any();
                    AreFeatureInitialized = true;
                    OnPropertyChanged(nameof(FeatureSettings));
                    VersionAndOption.FeatureSettings = featureSettings.Select(x => x.FeatureSetting).ToList();
                }
            }
        }

        public void SetFeaturesSelection(List<string> projectGenerationTags, List<string> projectGenerationExcludedFolders, List<FeatureSetting> originFeatureSettings)
        {
            foreach (var feature in FeatureSettings.Select(x => x.FeatureSetting))
            {
                var isFeatureTagUsedInProjectGeneration = projectGenerationTags.Any(feature.Tags.Contains);
                var isFeatureExcludedFoldersInProjectGeneration = projectGenerationExcludedFolders.Any(feature.FoldersToExcludes.Contains);
                var isSelected = isFeatureTagUsedInProjectGeneration || isFeatureExcludedFoldersInProjectGeneration;

                if (!isSelected && originFeatureSettings is not null)
                {
                    var originFeature = originFeatureSettings.FirstOrDefault(x => x.Id == feature.Id);
                    isSelected = originFeature is null && feature.IsSelected;
                }

                feature.IsSelected = isSelected;
            }
        }

        public bool HasFeature
        {
            get { return hasFeature; }
            set
            {
                if (hasFeature != value)
                {
                    hasFeature = value;
                    OnPropertyChanged(nameof(HasFeature));
                    OnPropertyChanged(nameof(AreFeatureVisible));
                    OnPropertyChanged(nameof(IsVisibileNoFeature));
                }
            }
        }

        public bool AreFeatureInitialized
        {
            get { return areFeatureInitialized; }
            set
            {
                if (areFeatureInitialized != value)
                {
                    areFeatureInitialized = value;
                    OnPropertyChanged(nameof(AreFeatureInitialized));
                    OnPropertyChanged(nameof(AreFeatureVisible));
                    OnPropertyChanged(nameof(AreFeatureLoading));
                    OnPropertyChanged(nameof(IsVisibileNoFeature));
                }
            }
        }

        public bool AreFeatureVisible
        {
            get { return hasFeature && areFeatureInitialized; }
        }

        public bool AreFeatureLoading
        {
            get { return !areFeatureInitialized; }
        }

        public bool IsVisibileNoFeature => !AreFeatureVisible;

        [RelayCommand]
        private void OnFeatureSettingSelectionChanged()
        {
            var notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (var notSelectedFeature in notSelectedFeatures)
            {
                FeatureSettings.Single(x => x.FeatureSetting.Id == notSelectedFeature.Id).IsSelected = false;
            }
        }

        // Event broker handlers
        private void OnRepositoryViewModelReleaseDataUpdated(RepositoryViewModel repository)
        {
            RefreshConfiguration();
        }

        private void OnSettingsUpdated(IBIATKSettings settings)
        {
            RefreshConfiguration();
            SettingsUseCompanyFiles = settings.UseCompanyFiles;
        }

        // Business Logic methods extracted from code-behind
        public void RefreshConfiguration()
        {
            var listCompanyFiles = new List<WorkRepository>();
            var listWorkTemplates = new List<WorkRepository>();

            foreach (var repository in settingsService.Settings.TemplateRepositories.Where(r => r.UseRepository))
            {
                AddTemplatesVersion(listWorkTemplates, repository);
            }

            var hasVersionXYZ = false;
            var repositoryVersionXYZ = settingsService.Settings.TemplateRepositories.FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);
            if (repositoryVersionXYZ is not null)
            {
                listWorkTemplates.Add(new WorkRepository(repositoryVersionXYZ, "VX.Y.Z"));
                hasVersionXYZ = true;
            }

            WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
            if (listWorkTemplates.Count >= 1)
            {
                WorkTemplate = hasVersionXYZ && listWorkTemplates.Count >= 2 ? listWorkTemplates[^2] : listWorkTemplates[^1];
            }

            SettingsUseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            UseCompanyFiles = settingsService.Settings.UseCompanyFiles;
            if (settingsService.Settings.UseCompanyFiles)
            {
                foreach (var repository in settingsService.Settings.CompanyFilesRepositories.Where(r => r.UseRepository))
                {
                    AddTemplatesVersion(listCompanyFiles, repository);
                }
                WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (WorkCompanyFiles.Count >= 1 && WorkTemplate is not null)
                {
                    WorkCompanyFile = GetWorkCompanyFile(WorkTemplate.Version);
                }
            }
        }

        private void AddTemplatesVersion(List<WorkRepository> WorkTemplates, IRepository repository)
        {
            foreach (var release in repository.Releases)
            {
                WorkTemplates.Add(new WorkRepository(repository, release.Name));
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        public void SelectVersion(string version)
        {
            WorkTemplate = WorkTemplates.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            this.currentProjectPath = path;
            this.LoadfeatureSetting();

            if (originFeatureSettings != null)
            {
                eventBroker.OnOriginFeatureSettingsChanged -= OnOriginFeatureSettingsChanged;
                eventBroker.OnOriginFeatureSettingsChanged += OnOriginFeatureSettingsChanged;
                OriginFeatureSettings = new List<FeatureSetting>(originFeatureSettings);
            }

            this.LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        private void OnOriginFeatureSettingsChanged(List<FeatureSetting> featureSettings)
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
                VersionAndOptionMapper.DtoToModel(versionAndOptionDto, this, mapCompanyFileVersion, mapFrameworkVersion, OriginFeatureSettings);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error when reading {projectGenerationFile} : {ex.Message}", "red");
            }
        }

        private void LoadfeatureSetting()
        {
            List<FeatureSetting> featureSettings = FeatureSettingHelper.Get(WorkTemplate?.VersionFolderPath);
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

            FeatureSettings = featureSettingViewModels;
        }

        public async Task FillVersionFolderPathAsync()
        {
            if (WorkTemplate?.Repository != null)
            {
                if (WorkTemplate.Version == "VX.Y.Z")
                {
                    WorkTemplate.VersionFolderPath = WorkTemplate.Repository.LocalPath;
                }
                else
                {
                    WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(WorkTemplate.Repository, WorkTemplate.Version);
                }
            }
        }

        // Commands for event handlers
        [RelayCommand]
        private void OnFrameworkVersionSelectionChanged()
        {
            eventBroker.RequestExecuteActionWithWaiter(async () =>
            {
                await this.FillVersionFolderPathAsync();
                this.LoadfeatureSetting();
                this.LoadVersionAndOption(false, false);
                if (OriginFeatureSettings is null)
                {
                    eventBroker.NotifyOriginFeatureSettingsChanged(FeatureSettings.Select(x => x.FeatureSetting).ToList());
                }
            });
        }

        [RelayCommand]
        private void OnCFVersionSelectionChanged()
        {
            this.LoadVersionAndOption(false, false);
        }
    }
}
