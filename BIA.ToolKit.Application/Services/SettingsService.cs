namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Configuration;

    public class SettingsService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly UIEventBroker eventBroker;
        private BIATKSettings settings;
        public IBIATKSettings Settings => settings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SettingsService(IConsoleWriter consoleWriter, UIEventBroker eventBroker)
        {
            this.consoleWriter = consoleWriter;
            this.eventBroker = eventBroker;
        }

        /// <summary>
        /// Read App.config file and get value corresponding to "key".
        /// </summary>
        public string ReadSetting(string key)
        {
            string result = null;
            try
            {
                result = ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException ex)
            {
                consoleWriter.AddMessageLine($"Error reading app settings (key={key}): {ex}", "Red");
            }
            return result;
        }

        public void Init(BIATKSettings settings)
        {
            this.settings = settings;
            eventBroker.NotifySettingsUpdated(settings);
        }

        public void SetBIATemplateRepositoryUrlRepository(string url)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.BIATemplateRepository).UrlRepo = url);
        }

        public void SetUseBIATemplateRepositoryLocalFolder(bool use)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.BIATemplateRepository).UseLocalFolder = use);
        }

        public void SetBIATemplateRepositoryLocalFolderPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.BIATemplateRepository).LocalFolderPath = path);
        }

        public void SetUseCompanyFiles(bool use)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.UseCompanyFiles = use);
        }

        public void SetCompanyFileseRepositoryUrlRepository(string url)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.CompanyFilesRepository).UrlRepo = url);
        }

        public void SetUseCompanyFilesRepositoryLocalFolder(bool use)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.CompanyFilesRepository).UseLocalFolder = use);
        }

        public void SetCompanyFilesRepositoryLocalFolderPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => ((RepositorySettings)settings.CompanyFilesRepository).LocalFolderPath = path);
        }

        public void SetCustomRepositories(List<RepositorySettings> repositoriesSettings)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CustomRepoTemplates = repositoriesSettings);
        }

        public void SetAutoUpdate(bool autoUpdate)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.AutoUpdate = autoUpdate);
        }

        public void SetUseLocalReleaseRepository(bool use)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.UseLocalReleaseRepository = use);
        }

        public void SetCreateProjectRootProjectPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CreateProjectRootProjectsPath = path);
        }

        public void SetModifyProjectRootProjectPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.ModifyProjectRootProjectsPath = path);
        }

        public void SetLocalReleaseRepositoryPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.LocalReleaseRepositoryPath = path);
        }

        public void SetCreateCompanyName(string name)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CreateCompanyName = name);
        }

        private void ExecuteAndNotifySettingsUpdated(Action action)
        {
            action.Invoke();
            eventBroker.NotifySettingsUpdated(settings);
        }
    }
}
