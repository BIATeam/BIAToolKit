namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    public class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }
        public RepositoryService repositoryService;
        IConsoleWriter consoleWriter;

        private bool hasFeature = false;
        private bool areFeatureInitialized = false;

        public VersionAndOptionViewModel()
        {
            VersionAndOption = new VersionAndOption();
        }

        public void Inject(RepositoryService repositoryService, IConsoleWriter consoleWriter)
        {
            this.repositoryService = repositoryService;
            this.consoleWriter = consoleWriter;
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
                        WorkCompanyFile.VersionFolderPath = repositoryService.PrepareVersionFolder(WorkCompanyFile.RepositorySettings, WorkCompanyFile.Version).Result;
                        string fileName = WorkCompanyFile.VersionFolderPath + "\\biaCompanyFiles.json";

                        try
                        {
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
                    RaisePropertyChanged(nameof(WorkCompanyFile));
                }
            }
        }

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

        public void CheckOptions (List<string> checkedOptions)
        {
            var options = new List<CFOption>();
            foreach (CFOption option in Options)
            {
                option.IsChecked = (checkedOptions?.Any(o => string.Equals(o, option.Key)) == true);
                options.Add(option);
            }
            Options = new ObservableCollection<CFOption>(options);
        }

        public ObservableCollection<FeatureSetting> FeatureSettings
        {
            get { return VersionAndOption.FeatureSettings; }
            set
            {
                if (VersionAndOption.FeatureSettings != value)
                {
                    VersionAndOption.FeatureSettings = value;
                    HasFeature = VersionAndOption.FeatureSettings.Any();
                    AreFeatureInitialized = true;
                    RaisePropertyChanged(nameof(FeatureSettings));
                }
            }
        }

        public void CheckFeature(List<string> tags, List<string> folders)
        {
            var features = new List<FeatureSetting>();
            foreach (FeatureSetting feature in FeatureSettings)
            {
                if ((feature.Tags != null && tags != null && feature.Tags.Any(t1 => tags.Any(t2 => string.Equals(t1,t2))))
                    ||
                    (feature.FoldersToExcludes != null && folders != null && feature.FoldersToExcludes.Any(f1 => folders.Any(f2 => string.Equals(f1, f2))))
                    )
                {
                    feature.IsSelected = true;
                }
                else
                {
                    feature.IsSelected = false;
                }
                features.Add(feature);
            }
            FeatureSettings = new ObservableCollection<FeatureSetting>(features);
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
    }
}
