namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;

    public class MainViewModel : ObservableObject
    {
        public BIATKSettings Settings { get; set; }
        public VersionAndOptionViewModel VersionAndOptionViewModel { get; set; }

        public IncludeProjectViewModel IncludeProjectViewModel { get; private set; }

        

        public MainViewModel()
        {
            IncludeProjectViewModel = new IncludeProjectViewModel();

            Settings = new BIATKSettings();
            // Because the RootProjects is in 2 VM (create and modify)
            SynchronizeSettings.AddCallBack("RootProjectsPath", DelegateSetRootProjectsPath);
        }

        public void DelegateSetRootProjectsPath(string value)
        {
            Settings_RootProjectsPath = value;
        }

        public string Settings_RootProjectsPath
        {
            get { return Settings.RootProjectsPath; }
            set
            {
                if (Settings.RootProjectsPath != value)
                {
                    Settings.RootProjectsPath = value;
                    RaisePropertyChanged("Settings_RootProjectsPath");
                }
            }
        }

        public string Settings_BIATemplateRepository_LocalFolderPath
        {
            get { return Settings.BIATemplateRepository.LocalFolderPath; }
            set
            {
                if (Settings.BIATemplateRepository.LocalFolderPath != value)
                {
                    Settings.BIATemplateRepository.LocalFolderPath = value;
                    RaisePropertyChanged("Settings_BIATemplateRepository_LocalFolderPath");
                }
            }
        }

        public string Settings_CompanyFiles_LocalFolderPath
        {
            get { return Settings.CompanyFiles.LocalFolderPath; }
            set
            {
                if (Settings.CompanyFiles.LocalFolderPath != value)
                {
                    Settings.CompanyFiles.LocalFolderPath = value;
                    RaisePropertyChanged("Settings_CompanyFiles_LocalFolderPath");
                }
            }
        }

    }
}
