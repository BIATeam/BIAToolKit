namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Messages;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public partial class VersionAndOptionViewModel : ObservableObject, IDisposable,
        IRecipient<SettingsUpdatedMessage>,
        IRecipient<RepositoryReleaseDataUpdatedMessage>,
        IRecipient<OriginFeatureSettingsChangedMessage>
    {
        public VersionAndOption VersionAndOption { get; set; }
        private readonly RepositoryService repositoryService;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly IConsoleWriter consoleWriter;

        private string currentProjectPath;
        private List<FeatureSetting> OriginFeatureSettings;
        private bool disposed;
        private bool listeningForOriginFeatureSettings;

        public VersionAndOptionViewModel(
            RepositoryService repositoryService,
            SettingsService settingsService,
            GitService gitService,
            IConsoleWriter consoleWriter)
        {
            this.repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));

            VersionAndOption = new VersionAndOption();

            // Subscribe to messenger events
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(SettingsUpdatedMessage message) => OnSettingsUpdated(message.Settings);
        public void Receive(RepositoryReleaseDataUpdatedMessage message) => OnRepositoryViewModelReleaseDataUpdated(message.Repository);
        public void Receive(OriginFeatureSettingsChangedMessage message)
        {
            if (listeningForOriginFeatureSettings)
            {
                OnOriginFeatureSettingsChanged(message.FeatureSettings);
            }
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
                    foreach (WorkRepository workCompanyFile in WorkCompanyFiles)
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
            WorkRepository result = WorkCompanyFiles.FirstOrDefault(x => x.Version == version);
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
                        WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await LoadCompanyFileSettingsAsync(ct)));
                    }
                    OnPropertyChanged(nameof(WorkCompanyFile));
                }
            }
        }

        private async Task LoadCompanyFileSettingsAsync(CancellationToken ct = default)
        {
            try
            {
                WorkCompanyFile.VersionFolderPath = await repositoryService.PrepareVersionFolder(WorkCompanyFile.Repository, WorkCompanyFile.Version, ct);
                string fileName = Path.Combine(WorkCompanyFile.VersionFolderPath, "biaCompanyFiles.json");

                string jsonString = await Task.Run(() => File.ReadAllText(fileName));

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SettingsNotUseCompanyFiles))]
        private bool settingsUseCompanyFiles;

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

        public void ApplyFromDto(VersionAndOptionDto dto, bool mapCompanyFileVersion, bool mapFrameworkVersion, List<FeatureSetting> originFeatureSettings)
        {
            // Company Files
            UseCompanyFiles = dto.UseCompanyFiles;

            // Feature
            SetFeaturesSelection(dto.Tags, dto.Folders, originFeatureSettings);

            // Company Files
            if (mapCompanyFileVersion)
            {
                WorkCompanyFile = GetWorkCompanyFile(dto.CompanyFileVersion);
            }

            if (Profiles.Any(p => p == dto.Profile))
            {
                Profile = dto.Profile;
            }

            CheckOptions(dto.Options);

            if (mapFrameworkVersion)
            {
                WorkRepository workTemplate = WorkTemplates.FirstOrDefault(x => x.Version == dto.FrameworkVersion);
                if (workTemplate is not null)
                {
                    WorkTemplate = workTemplate;
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<FeatureSettingViewModel> featureSettings = [];

        partial void OnFeatureSettingsChanged(ObservableCollection<FeatureSettingViewModel> value)
        {
            HasFeature = value?.Any() == true;
            AreFeatureInitialized = true;
            VersionAndOption.FeatureSettings = [.. (value ?? []).Select(x => x.FeatureSetting)];
        }

        public void SetFeaturesSelection(List<string> projectGenerationTags, List<string> projectGenerationExcludedFolders, List<FeatureSetting> originFeatureSettings)
        {
            FeatureSettingService.ApplyFeaturesSelection(
                VersionAndOption.FeatureSettings, projectGenerationTags, projectGenerationExcludedFolders, originFeatureSettings);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreFeatureVisible))]
        [NotifyPropertyChangedFor(nameof(IsVisibileNoFeature))]
        private bool hasFeature;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreFeatureVisible))]
        [NotifyPropertyChangedFor(nameof(AreFeatureLoading))]
        [NotifyPropertyChangedFor(nameof(IsVisibileNoFeature))]
        private bool areFeatureInitialized;

        public bool AreFeatureVisible => HasFeature && AreFeatureInitialized;

        public bool AreFeatureLoading => !AreFeatureInitialized;

        public bool IsVisibileNoFeature => !AreFeatureVisible;

        [RelayCommand]
        private void OnFeatureSettingSelectionChanged()
        {
            List<FeatureSetting> notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (FeatureSetting notSelectedFeature in notSelectedFeatures)
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

            if (WorkTemplate is not null)
            {
                OnFrameworkVersionSelectionChanged();
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

        public async Task SetCurrentProjectPathAsync(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            this.currentProjectPath = path;

            if (originFeatureSettings != null)
            {
                listeningForOriginFeatureSettings = true;
                OriginFeatureSettings = new List<FeatureSetting>(originFeatureSettings);
            }

            // The version folder must be prepared (potentially downloaded) before we can read
            // its feature settings. The XAML SelectionChanged EventTrigger normally handles this
            // for user-driven version changes, but when InitVersionAndOptionComponents calls us
            // programmatically that path races with us — so we await it here explicitly.
            await this.FillVersionFolderPathAsync();

            this.LoadfeatureSetting();
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
                ApplyFromDto(versionAndOptionDto, mapCompanyFileVersion, mapFrameworkVersion, OriginFeatureSettings);
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
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await this.FillVersionFolderPathAsync();
                this.LoadfeatureSetting();
                this.LoadVersionAndOption(false, false);
                if (OriginFeatureSettings is null)
                {
                    WeakReferenceMessenger.Default.Send(new OriginFeatureSettingsChangedMessage(FeatureSettings.Select(x => x.FeatureSetting).ToList()));
                }
            }));
        }

        [RelayCommand]
        private void OnCFVersionSelectionChanged()
        {
            this.LoadVersionAndOption(false, false);
        }
    }
}
