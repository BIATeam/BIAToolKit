namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }

        private bool hasFeature = false;

        public VersionAndOptionViewModel()
        {
            VersionAndOption = new VersionAndOption();
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
                }
            }
        }

        public IList<CFOption> Options
        {
            get { return VersionAndOption.Options; }
            set
            {
                if (VersionAndOption.Options != value)
                {
                    VersionAndOption.Options = value;
                    RaisePropertyChanged(nameof(UseCompanyFiles));
                }
            }
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
                    RaisePropertyChanged(nameof(FeatureSettings));
                }
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
                }
            }
        }
    }
}
