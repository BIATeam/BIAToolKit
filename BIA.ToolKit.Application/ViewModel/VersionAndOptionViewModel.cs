namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class VersionAndOptionViewModel : ObservableObject
    {
        public VersionAndOption VersionAndOption { get; set; }

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
                    RaisePropertyChanged("WorkTemplates");
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
                    RaisePropertyChanged("WorkTemplate");
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
                    RaisePropertyChanged("Profiles");
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
                    RaisePropertyChanged("Profile");
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
                    RaisePropertyChanged("WorkCompanyFiles");
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
                    RaisePropertyChanged("WorkCompanyFile");
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
                    RaisePropertyChanged("UseCompanyFiles");
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
                    RaisePropertyChanged("UseCompanyFiles");
                }
            }
        }
    }
}
