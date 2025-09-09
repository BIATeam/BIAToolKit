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
            RaisePropertyChanged(nameof(Settings_UseCompanyFiles));
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
