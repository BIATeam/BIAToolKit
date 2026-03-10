namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class VersionAndOptionViewModel : ViewModelBase
    {
        public VersionAndOption VersionAndOption { get; set; }
        public RepositoryService repositoryService;
        IConsoleWriter consoleWriter;
        private SettingsService settingsService;
        private string currentProjectPath;
        private List<FeatureSetting> OriginFeatureSettings;
        private bool isOriginFeatureSettingsListener = false;

        private bool hasFeature = false;
        private bool areFeatureInitialized = false;

        public VersionAndOptionViewModel(IMessenger messenger, RepositoryService repositoryService, IConsoleWriter consoleWriter, SettingsService settingsService)
            : base(messenger)
        {
            VersionAndOption = new VersionAndOption();
            this.repositoryService = repositoryService;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
        }

        public override void Initialize()
        {
            Messenger.Subscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
        }

        public override void Cleanup()
        {
            Messenger.Unsubscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
            if (isOriginFeatureSettingsListener)
            {
                Messenger.Unsubscribe<OriginFeatureSettingsChangedMessage>(OnOriginFeatureSettingsChanged);
            }
        }

        private void OnSettingsUpdated(SettingsUpdatedMessage msg)
        {
            RefreshConfiguration();
            SettingsUseCompanyFiles = msg.Settings.UseCompanyFiles;
        }

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

        private void AddTemplatesVersion(List<WorkRepository> workTemplates, IRepository repository)
        {
            foreach (var release in repository.Releases)
            {
                workTemplates.Add(new WorkRepository(repository, release.Name));
            }

            workTemplates.Sort(new WorkRepository.VersionComparer());
        }

        public ObservableCollection<WorkRepository> WorkTemplates
        {
            get { return VersionAndOption.WorkTemplates; }
            set
            {
                if (VersionAndOption.WorkTemplates != value)
                {
                    VersionAndOption.WorkTemplates = value;
                    RaisePropertyChanged(nameof(WorkTemplates));
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
                    RaisePropertyChanged(nameof(WorkTemplate));
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
                    RaisePropertyChanged(nameof(Profiles));
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
                    RaisePropertyChanged(nameof(Profile));
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
                    RaisePropertyChanged(nameof(WorkCompanyFiles));
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
                        Messenger.Send(new ExecuteWithWaiterMessage
                        {
                            Task = async () =>
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
                            }
                        });
                    }
                    RaisePropertyChanged(nameof(WorkCompanyFile));
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
                RaisePropertyChanged(nameof(SettingsUseCompanyFiles));
                RaisePropertyChanged(nameof(SettingsNotUseCompanyFiles));
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
                    RaisePropertyChanged(nameof(UseCompanyFiles));
                    RaisePropertyChanged(nameof(NotUseCompanyFiles));
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
                    RaisePropertyChanged(nameof(Options));
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
                    RaisePropertyChanged(nameof(FeatureSettings));
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
                    RaisePropertyChanged(nameof(HasFeature));
                    RaisePropertyChanged(nameof(AreFeatureVisible));
                    RaisePropertyChanged(nameof(IsVisibileNoFeature));
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
                    RaisePropertyChanged(nameof(AreFeatureInitialized));
                    RaisePropertyChanged(nameof(AreFeatureVisible));
                    RaisePropertyChanged(nameof(AreFeatureLoading));
                    RaisePropertyChanged(nameof(IsVisibileNoFeature));
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

        public ICommand OnFeatureSettingSelectionChangedCommand => new RelayCommand(_ => OnFeatureSettingSelectionChanged());

        private void OnFeatureSettingSelectionChanged()
        {
            var notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (var notSelectedFeature in notSelectedFeatures)
            {
                FeatureSettings.Single(x => x.FeatureSetting.Id == notSelectedFeature.Id).IsSelected = false;
            }
        }

        public void SelectVersion(string version)
        {
            WorkTemplate = WorkTemplates.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            currentProjectPath = path;
            LoadfeatureSetting();

            if (originFeatureSettings != null)
            {
                if (!isOriginFeatureSettingsListener)
                {
                    Messenger.Subscribe<OriginFeatureSettingsChangedMessage>(OnOriginFeatureSettingsChanged);
                    isOriginFeatureSettingsListener = true;
                }
                OriginFeatureSettings = new List<FeatureSetting>(originFeatureSettings);
            }

            LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        private void OnOriginFeatureSettingsChanged(OriginFeatureSettingsChangedMessage message)
        {
            OriginFeatureSettings = new List<FeatureSetting>(message.FeatureSettings);
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
                VersionAndOptionMapper.DtoToModel(versionAndOptionDto, this, mapCompanyFileVersion, mapFrameworkVersion, OriginFeatureSettings);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error when reading {projectGenerationFile} : {ex.Message}", "red");
            }
        }

        public void LoadfeatureSetting()
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
                    WorkTemplate.VersionFolderPath = WorkTemplate.Repository.LocalPath;
                else
                    WorkTemplate.VersionFolderPath = await repositoryService.PrepareVersionFolder(WorkTemplate.Repository, WorkTemplate.Version);
            }
        }

        public void NotifyOriginFeatureSettingsChanged()
        {
            if (OriginFeatureSettings is null)
            {
                Messenger.Send(new OriginFeatureSettingsChangedMessage { FeatureSettings = FeatureSettings.Select(x => x.FeatureSetting).ToList() });
            }
        }
    }
}
