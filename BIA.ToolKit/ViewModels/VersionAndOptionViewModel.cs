namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Helper;
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
        private readonly IDialogService dialogService;

        private string currentProjectPath;
        private List<FeatureSetting> OriginFeatureSettings;
        private bool disposed;
        private bool listeningForOriginFeatureSettings;

        public VersionAndOptionViewModel(
            RepositoryService repositoryService,
            SettingsService settingsService,
            GitService gitService,
            IConsoleWriter consoleWriter,
            IDialogService dialogService)
        {
            this.repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

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
                    DefaultTeamName = null;
                    DefaultTeamNamePlural = null;
                    DefaultTeamDomainName = null;
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

                string jsonString = await Task.Run(() => File.ReadAllText(fileName), ct);

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

                // Normal mode: auto-apply preferred defaults (profile Wiring, DMEU, Remove example settings).
                ApplyCompanyFilesDefaults();
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
                    OnPropertyChanged(nameof(CheckedOptionsSummary));
                    OnPropertyChanged(nameof(RemoveExampleSettingsChecked));
                    OnPropertyChanged(nameof(RemoveExampleSettingsSuffix));
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

            if (dto.HasDefaultTeam)
            {
                FeatureSettingViewModel createDefaultTeamVm = FeatureSettings?.FirstOrDefault(f => f.IsCreateDefaultTeam);
                if (createDefaultTeamVm != null)
                {
                    createDefaultTeamVm.IsSelected = true;
                }
            }

            // Default Team
            DefaultTeamName = dto.DefaultTeamName;
            DefaultTeamNamePlural = dto.DefaultTeamNamePlural;
            DefaultTeamDomainName = dto.DefaultTeamDomainName;

            // Company Files
            if (mapCompanyFileVersion)
            {
                WorkCompanyFile = GetWorkCompanyFile(dto.CompanyFileVersion);
            }

            // The project was created with Company Files but none are available now
            // (global setting off, or matching repo/version no longer configured).
            // Drop the flag silently-for-model + log a clear warning, rather than letting
            // downstream copy blow up with a NullReferenceException.
            if (UseCompanyFiles && WorkCompanyFile == null)
            {
                UseCompanyFiles = false;
                consoleWriter.AddMessageLine(
                    $"Project was created with Company Files (version '{dto.CompanyFileVersion}') but none are available — overlay skipped. Enable Company Files in Settings and configure the matching repository to restore it.",
                    "Orange");
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
            RefreshDefaultTeamConfigStatus();

            // In Normal mode, reapply the current project-type preset each time a new feature
            // list is loaded (e.g. framework version change) so the card selection stays in sync.
            if (!IsAdvancedMode)
            {
                ApplyProjectTypePreset(SelectedProjectType);
            }
        }

        #region Normal / Advanced mode

        [ObservableProperty]
        private bool isAdvancedMode;

        [ObservableProperty]
        private ProjectTypeKind selectedProjectType = ProjectTypeKind.Complete;

        partial void OnSelectedProjectTypeChanged(ProjectTypeKind value)
        {
            ApplyProjectTypePreset(value);
        }

        [RelayCommand]
        private void ToggleAdvancedMode()
        {
            IsAdvancedMode = !IsAdvancedMode;
        }

        /// <summary>
        /// Opens a dedicated popup to edit only the Company Files (version, profile, options).
        /// Distinct from "More options" which reveals the full feature grid — this is a
        /// targeted editor for the card's footer summary.
        /// </summary>
        [RelayCommand]
        private void EditCompanyFiles()
        {
            dialogService.ShowCompanyFilesEditor(this);
        }

        partial void OnIsAdvancedModeChanged(bool value)
        {
            // Intentionally no-op: toggling between Normal and Advanced must preserve
            // whatever the user has already configured (features + Company Files).
            // Presets are only (re)applied when the user explicitly selects a card or
            // when a new framework version is loaded.
        }

        /// <summary>
        /// Applies the feature-settings checkbox preset associated with a project type.
        /// Leaves <see cref="BiaFeatureSettingsEnum.UserCustomFields"/> and
        /// <see cref="BiaFeatureSettingsEnum.CreateDefaultTeam"/> untouched.
        /// </summary>
        private void ApplyProjectTypePreset(ProjectTypeKind projectType)
        {
            if (FeatureSettings is null || FeatureSettings.Count == 0)
                return;

            bool frontEnd, auth, db, deployDb, worker;
            switch (projectType)
            {
                case ProjectTypeKind.ApiWithExistingDb:
                    frontEnd = false; auth = false; db = true; deployDb = false; worker = false;
                    break;
                case ProjectTypeKind.Complete:
                default:
                    frontEnd = true; auth = true; db = true; deployDb = true; worker = true;
                    break;
            }

            SetFeatureSelected(BiaFeatureSettingsEnum.FrontEnd, frontEnd);
            SetFeatureSelected(BiaFeatureSettingsEnum.BackToBackAuth, auth);
            SetFeatureSelected(BiaFeatureSettingsEnum.Database, db);
            SetFeatureSelected(BiaFeatureSettingsEnum.DeployDb, deployDb);
            SetFeatureSelected(BiaFeatureSettingsEnum.WorkerService, worker);

            // Honour DisabledFeatures constraints declared by the JSON (same as user click).
            OnFeatureSettingSelectionChanged();
        }

        private void SetFeatureSelected(BiaFeatureSettingsEnum feature, bool isSelected)
        {
            var fvm = FeatureSettings?.FirstOrDefault(f => f.FeatureSetting.Id == (int)feature);
            if (fvm != null && fvm.IsSelected != isSelected)
            {
                fvm.IsSelected = isSelected;
            }
        }

        /// <summary>
        /// In Normal mode, force Company Files defaults: profile = "Wiring", option "DMEU" checked,
        /// option "Remove example settings" checked. Called after each company-file settings load.
        /// </summary>
        private void ApplyCompanyFilesDefaults()
        {
            if (IsAdvancedMode)
                return;

            UseCompanyFiles = true;

            if (Profiles?.Any() == true)
            {
                string wiring = Profiles.FirstOrDefault(p => string.Equals(p, "Wiring", StringComparison.OrdinalIgnoreCase));
                if (wiring != null)
                    Profile = wiring;
            }

            if (Options?.Any() == true)
            {
                foreach (var opt in Options)
                {
                    if (IsDmeuOption(opt))
                        opt.IsChecked = true;
                    else if (IsRemoveExampleOption(opt))
                        opt.IsChecked = true;
                }
                // Refresh the collection so the UI (if ever shown later) picks up IsChecked changes.
                Options = new ObservableCollection<CFOption>(Options);
            }
        }

        private static bool IsDmeuOption(CFOption opt)
            => string.Equals(opt?.Key, "DMEU", StringComparison.OrdinalIgnoreCase)
            || string.Equals(opt?.Name, "DMEU", StringComparison.OrdinalIgnoreCase);

        private static bool IsRemoveExampleOption(CFOption opt)
            => (opt?.Key?.Contains("RemoveExample", StringComparison.OrdinalIgnoreCase) == true)
            || (opt?.Name?.Contains("Remove example", StringComparison.OrdinalIgnoreCase) == true);

        /// <summary>
        /// Summary of the checked Company Files options (excluding "Remove example settings"
        /// which is surfaced separately), shown on the Normal-mode cards.
        /// </summary>
        public string CheckedOptionsSummary
        {
            get
            {
                if (Options is null || Options.Count == 0)
                    return "—";
                var checkedNames = Options
                    .Where(o => o.IsChecked && !IsRemoveExampleOption(o))
                    .Select(o => string.IsNullOrWhiteSpace(o.Name) ? o.Key : o.Name)
                    .ToList();
                return checkedNames.Count == 0 ? "—" : string.Join(", ", checkedNames);
            }
        }

        public bool RemoveExampleSettingsChecked
            => Options?.Any(o => o.IsChecked && IsRemoveExampleOption(o)) == true;

        /// <summary>
        /// Suffix appended to the card footer summary when "Remove example settings" is on.
        /// </summary>
        public string RemoveExampleSettingsSuffix
            => RemoveExampleSettingsChecked ? " · Remove examples" : string.Empty;

        /// <summary>
        /// Called by the Advanced-mode Company Files option checkboxes so the Normal-mode
        /// card footer stays in sync with user tweaks.
        /// </summary>
        [RelayCommand]
        private void OptionCheckedChanged()
        {
            OnPropertyChanged(nameof(CheckedOptionsSummary));
            OnPropertyChanged(nameof(RemoveExampleSettingsChecked));
            OnPropertyChanged(nameof(RemoveExampleSettingsSuffix));
        }

        #endregion

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

        #region Default Team

        public string DefaultTeamName
        {
            get => VersionAndOption.DefaultTeamName;
            set
            {
                if (VersionAndOption.DefaultTeamName != value)
                {
                    VersionAndOption.DefaultTeamName = value;
                    OnPropertyChanged(nameof(DefaultTeamName));
                    RefreshDefaultTeamConfigStatus();
                }
            }
        }

        public string DefaultTeamNamePlural
        {
            get => VersionAndOption.DefaultTeamNamePlural;
            set
            {
                if (VersionAndOption.DefaultTeamNamePlural != value)
                {
                    VersionAndOption.DefaultTeamNamePlural = value;
                    OnPropertyChanged(nameof(DefaultTeamNamePlural));
                    RefreshDefaultTeamConfigStatus();
                }
            }
        }

        public string DefaultTeamDomainName
        {
            get => VersionAndOption.DefaultTeamDomainName;
            set
            {
                if (VersionAndOption.DefaultTeamDomainName != value)
                {
                    VersionAndOption.DefaultTeamDomainName = value;
                    OnPropertyChanged(nameof(DefaultTeamDomainName));
                    RefreshDefaultTeamConfigStatus();
                }
            }
        }

        public bool IsDefaultTeamSetupValid
        {
            get
            {
                bool isSelected = FeatureSettings?.Any(f =>
                    f.FeatureSetting.Id == (int)BiaFeatureSettingsEnum.CreateDefaultTeam &&
                    f.IsSelected) ?? false;

                return !isSelected ||
                    (!string.IsNullOrWhiteSpace(VersionAndOption.DefaultTeamName) &&
                     !string.IsNullOrWhiteSpace(VersionAndOption.DefaultTeamNamePlural) &&
                     !string.IsNullOrWhiteSpace(VersionAndOption.DefaultTeamDomainName));
            }
        }

        private void RefreshDefaultTeamConfigStatus()
        {
            bool isValid = IsDefaultTeamSetupValid;
            FeatureSettingViewModel fvm = FeatureSettings?.FirstOrDefault(f => f.IsCreateDefaultTeam);
            if (fvm != null)
            {
                fvm.IsDefaultTeamConfigValid = isValid;
            }
            OnPropertyChanged(nameof(IsDefaultTeamSetupValid));
        }

        [RelayCommand]
        private void OpenDefaultTeamSettings()
        {
            var result = dialogService.ShowDefaultTeamSettings(
                DefaultTeamName, DefaultTeamNamePlural, DefaultTeamDomainName);
            if (result.HasValue)
            {
                DefaultTeamName = result.Value.TeamName;
                DefaultTeamNamePlural = result.Value.TeamNamePlural;
                DefaultTeamDomainName = result.Value.DomainName;
            }
        }

        #endregion

        [RelayCommand]
        private void OnFeatureSettingSelectionChanged()
        {
            List<FeatureSetting> notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (FeatureSetting notSelectedFeature in notSelectedFeatures)
            {
                FeatureSettings.Single(x => x.FeatureSetting.Id == notSelectedFeature.Id).IsSelected = false;
            }

            RefreshDefaultTeamConfigStatus();
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

        public async Task SetCurrentProjectPathAsync(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null, CancellationToken ct = default)
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
            await this.FillVersionFolderPathAsync(ct);

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

        public async Task FillVersionFolderPathAsync(CancellationToken ct = default)
        {
            if (WorkTemplate?.Repository != null)
            {
                if (WorkTemplate.Version == "VX.Y.Z")
                {
                    WorkTemplate.VersionFolderPath = WorkTemplate.Repository.LocalPath;
                }
                else
                {
                    WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(WorkTemplate.Repository, WorkTemplate.Version, ct);
                }
            }
        }

        // Commands for event handlers
        [RelayCommand]
        private void OnFrameworkVersionSelectionChanged()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await this.FillVersionFolderPathAsync(ct);
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
