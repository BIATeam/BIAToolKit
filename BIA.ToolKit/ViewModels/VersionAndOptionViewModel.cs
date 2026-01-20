namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Mapper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Application.Messages;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Windows.Input;

    public class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }
        private readonly RepositoryService repositoryService;
        private readonly IConsoleWriter consoleWriter;
        private readonly IMessenger messenger;
        private readonly SettingsService settingsService;
        private string currentProjectPath;
        private List<FeatureSetting> originFeatureSettings;

        private bool hasFeature = false;
        private bool areFeatureInitialized = false;

        public VersionAndOptionViewModel(RepositoryService repositoryService, SettingsService settingsService, IConsoleWriter consoleWriter, IMessenger messenger)
        {
            this.repositoryService = repositoryService;
            this.settingsService = settingsService;
            this.consoleWriter = consoleWriter;
            this.messenger = messenger;
            VersionAndOption = new VersionAndOption();

            messenger.Register<SettingsUpdatedMessage>(this, (r, m) => OnSettingsUpdated(m.Settings));
            messenger.Register<RepositoryViewModelReleaseDataUpdatedMessage>(this, (r, m) => OnRepositoryReleaseDataUpdated());
            messenger.Register<OriginFeatureSettingsChangedMessage>(this, (r, m) => OnOriginFeatureSettingsChanged(m.FeatureSettings));
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
                        messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
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
                        }));
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

        public ICommand OnFeatureSettingSelectionChangedCommand => new RelayCommand(OnFeatureSettingSelectionChanged);
        public ICommand FrameworkVersionSelectionChangedCommand => new RelayCommand(HandleFrameworkVersionSelectionChanged);
        public ICommand CompanyFilesSelectionChangedCommand => new RelayCommand(HandleCompanyFilesSelectionChanged);

        private void OnFeatureSettingSelectionChanged()
        {
            var notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (var notSelectedFeature in notSelectedFeatures)
            {
                FeatureSettings.Single(x => x.FeatureSetting.Id == notSelectedFeature.Id).IsSelected = false;
            }
        }

        private void OnSettingsUpdated(IBIATKSettings settings)
        {
            SettingsUseCompanyFiles = settings.UseCompanyFiles;
            RefreshConfiguration();
        }

        private void OnRepositoryReleaseDataUpdated()
        {
            RefreshConfiguration();
        }

        private void OnOriginFeatureSettingsChanged(List<FeatureSetting> featureSettings)
        {
            originFeatureSettings = featureSettings;
            LoadVersionAndOption(false, false);
        }

        public void SelectVersion(string version)
        {
            WorkTemplate = WorkTemplates?.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            currentProjectPath = path;
            LoadFeatureSetting();

            if (originFeatureSettings != null)
            {
                this.originFeatureSettings = new List<FeatureSetting>(originFeatureSettings);
            }

            LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
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
                VersionAndOptionMapper.DtoToModel(versionAndOptionDto, this, mapCompanyFileVersion, mapFrameworkVersion, originFeatureSettings);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error when reading {projectGenerationFile} : {ex.Message}", "red");
            }
        }

        private void LoadFeatureSetting()
        {
            List<FeatureSetting> featureSettings = FeatureSettingHelper.Get(WorkTemplate?.VersionFolderPath);
            List<FeatureSetting> projectFeatureSettings = FeatureSettingHelper.Get(currentProjectPath);

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
                    WorkTemplate.VersionFolderPath = await repositoryService.PrepareVersionFolder(WorkTemplate.Repository, WorkTemplate.Version);
                }
            }
        }

        public void RefreshConfiguration()
        {
            var listWorkTemplates = LoadRepositoriesFromSettings(settingsService.Settings.TemplateRepositories);

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

            bool useCompanyFiles = settingsService.Settings.UseCompanyFiles;
            SettingsUseCompanyFiles = useCompanyFiles;
            UseCompanyFiles = useCompanyFiles;
            
            if (useCompanyFiles)
            {
                var listCompanyFiles = LoadRepositoriesFromSettings(settingsService.Settings.CompanyFilesRepositories);
                WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (WorkCompanyFiles.Count >= 1 && WorkTemplate is not null)
                {
                    WorkCompanyFile = GetWorkCompanyFile(WorkTemplate.Version);
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

        public void HandleFrameworkVersionSelectionChanged()
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                await FillVersionFolderPathAsync();
                LoadFeatureSetting();
                LoadVersionAndOption(false, false);
                if (originFeatureSettings is null)
                {
                    messenger.Send(new OriginFeatureSettingsChangedMessage(FeatureSettings.Select(x => x.FeatureSetting).ToList()));
                }
            }));
        }

        public void HandleCompanyFilesSelectionChanged()
        {
            LoadVersionAndOption(false, false);
        }
    }
}
