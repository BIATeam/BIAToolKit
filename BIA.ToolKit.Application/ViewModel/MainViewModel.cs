namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Reflection;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;

    public class MainViewModel : ObservableObject
    {
        private readonly Version applicationVersion;

        public BIATKSettings Settings { get; set; }

        public MainViewModel(Version applicationVersion)
        {
            Settings = new BIATKSettings();
            // Because the RootProjects is in 2 VM (create and modify)
            SynchronizeSettings.AddCallBack("RootProjectsPath", DelegateSetRootProjectsPath);
            this.applicationVersion = applicationVersion;
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
                    RaisePropertyChanged(nameof(Settings_RootProjectsPath));
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
                    RaisePropertyChanged(nameof(Settings_BIATemplateRepository_LocalFolderPath));
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
                    RaisePropertyChanged(nameof(Settings_CompanyFiles_LocalFolderPath));
                }
            }
        }

        public string ApplicationVersion => $"V{applicationVersion.Major}.{applicationVersion.Minor}.{applicationVersion.Build}";

        private bool _updateAvailable;
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                _updateAvailable = value;
                RaisePropertyChanged(nameof(UpdateAvailable));
            }
        }

        public bool UseTemplateLocalFolder
        {
            get { return Settings.BIATemplateRepository.UseLocalFolder; }
            set 
            {
                Settings.BIATemplateRepository.UseLocalFolder = value; 
                RaisePropertyChanged(nameof(UseTemplateLocalFolder));
                RaisePropertyChanged(nameof(IsEnableTemplateCleanRelease));
            }
        }

        public bool IsEnableTemplateCleanRelease => !UseTemplateLocalFolder;
    }
}
