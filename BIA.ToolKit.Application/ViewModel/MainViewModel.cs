namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Reflection;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;

    public class MainViewModel : ObservableObject
    {
        private readonly Version applicationVersion;
        private readonly SettingsService settingsService;

        public MainViewModel(Version applicationVersion, UIEventBroker eventBroker, SettingsService settingsService)
        {
            this.applicationVersion = applicationVersion;
            this.settingsService = settingsService;
            eventBroker.OnSettingsUpdated += EventBroker_OnSettingsUpdated;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            RaisePropertyChanged(nameof(Settings_RootProjectsPath));
            RaisePropertyChanged(nameof(Settings_CreateCompanyName));
            RaisePropertyChanged(nameof(Settings_BIATemplateRepository_UrlRepository));
            RaisePropertyChanged(nameof(Settings_BIATemplateRepository_UseLocalFolder));
            RaisePropertyChanged(nameof(Settings_BIATemplateRepository_LocalFolderPath));
            RaisePropertyChanged(nameof(Settings_UseCompanyFiles));
            RaisePropertyChanged(nameof(Settings_CompanyFilesRepository_UrlRepository));
            RaisePropertyChanged(nameof(Settings_CompanyFilesRepository_UseLocalFolder));
            RaisePropertyChanged(nameof(Settings_CompanyFilesRepository_LocalFolderPath));
            RaisePropertyChanged(nameof(Settings_AutoUpdate));
        }

        public string Settings_RootProjectsPath
        {
            get { return settingsService.Settings.CreateProjectRootProjectsPath; }
            set 
            {
                if (settingsService.Settings.CreateProjectRootProjectsPath != value)
                {
                    settingsService.SetCreateProjectRootProjectPath(value);
                }
            }
        }

        public string Settings_CreateCompanyName
        {
            get { return settingsService.Settings.CreateCompanyName; }
            set
            {
                if (settingsService.Settings.CreateCompanyName != value)
                {
                    settingsService.SetCreateCompanyName(value);
                }
            }
        }

        public string Settings_BIATemplateRepository_UrlRepository
        {
            get { return settingsService.Settings.BIATemplateRepository.UrlRepo; }
            set
            {
                if (settingsService.Settings.BIATemplateRepository.UrlRepo != value)
                {
                    settingsService.SetBIATemplateRepositoryUrlRepository(value);
                }
            }
        }

        public bool Settings_BIATemplateRepository_UseLocalFolder
        {
            get { return settingsService.Settings.BIATemplateRepository.UseLocalFolder; }
            set
            {
                if (settingsService.Settings.BIATemplateRepository.UseLocalFolder != value)
                {
                    settingsService.SetUseBIATemplateRepositoryLocalFolder(value);
                }
            }
        }

        public string Settings_BIATemplateRepository_LocalFolderPath
        {
            get { return settingsService.Settings.BIATemplateRepository.LocalFolderPath; }
            set
            {
                if (settingsService.Settings.BIATemplateRepository.LocalFolderPath != value)
                {
                    settingsService.SetBIATemplateRepositoryLocalFolderPath(value);
                }
            }
        }

        public bool Settings_UseCompanyFiles
        {
            get { return settingsService.Settings.UseCompanyFiles; }
            set
            {
                if (settingsService.Settings.UseCompanyFiles != value)
                {
                    settingsService.SetUseCompanyFiles(value);
                }
            }
        }

        public string Settings_CompanyFilesRepository_UrlRepository
        {
            get { return settingsService.Settings.CompanyFilesRepository.UrlRepo; }
            set
            {
                if (settingsService.Settings.CompanyFilesRepository.UrlRepo != value)
                {
                    settingsService.SetCompanyFileseRepositoryUrlRepository(value);
                }
            }
        }

        public bool Settings_CompanyFilesRepository_UseLocalFolder
        {
            get { return settingsService.Settings.CompanyFilesRepository.UseLocalFolder; }
            set
            {
                if (settingsService.Settings.CompanyFilesRepository.UseLocalFolder != value)
                {
                    settingsService.SetUseCompanyFilesRepositoryLocalFolder(value);
                }
            }
        }

        public string Settings_CompanyFilesRepository_LocalFolderPath
        {
            get { return settingsService.Settings.CompanyFilesRepository.LocalFolderPath; }
            set
            {
                if (settingsService.Settings.CompanyFilesRepository.LocalFolderPath != value)
                {
                    settingsService.SetCompanyFilesRepositoryLocalFolderPath(value);
                }
            }
        }

        public bool Settings_AutoUpdate
        {
            get => settingsService.Settings.AutoUpdate;
            set
            {
                if (settingsService.Settings.AutoUpdate != value)
                {
                    settingsService.SetAutoUpdate(value);
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
    }
}
