namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain.Model;
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
    using System.Text.Json;
    using System.Windows.Input;

    public class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }
        public RepositoryService repositoryService;
        IConsoleWriter consoleWriter;
        private IMessenger messenger;

        private bool hasFeature = false;
        private bool areFeatureInitialized = false;

        public VersionAndOptionViewModel()
        {
            VersionAndOption = new VersionAndOption();
        }

        public void Inject(RepositoryService repositoryService, IConsoleWriter consoleWriter, IMessenger messenger)
        {
            this.repositoryService = repositoryService;
            this.consoleWriter = consoleWriter;
            this.messenger = messenger;
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

        private void OnFeatureSettingSelectionChanged()
        {
            var notSelectedFeatures = VersionAndOption.FeatureSettings.FilterNotSelectedFeatures();
            foreach (var notSelectedFeature in notSelectedFeatures)
            {
                FeatureSettings.Single(x => x.FeatureSetting.Id == notSelectedFeature.Id).IsSelected = false;
            }
        }
    }
}
