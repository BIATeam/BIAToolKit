﻿namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Xml.Linq;

    public class SettingsService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly UIEventBroker eventBroker;
        private BIATKSettings settings = new();
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

        public void SetUseCompanyFiles(bool use)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.UseCompanyFiles = use);
        }

        public void SetAutoUpdate(bool autoUpdate)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.AutoUpdate = autoUpdate);
        }

        public void SetCreateProjectRootProjectPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CreateProjectRootProjectsPath = path);
        }

        public void SetModifyProjectRootProjectPath(string path)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.ModifyProjectRootProjectsPath = path);
        }

        public void SetCreateCompanyName(string name)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CreateCompanyName = name);
        }

        public void SetToolkitRepository(IRepository repository)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.ToolkitRepository = repository);
        }

        public void SetTemplateRepositories(IReadOnlyList<IRepository> repositories)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.TemplateRepositories = repositories);
        }

        public void SetCompanyFilesRepositories(IReadOnlyList<IRepository> repositories)
        {
            ExecuteAndNotifySettingsUpdated(() => settings.CompanyFilesRepositories = repositories);
        }

        private void ExecuteAndNotifySettingsUpdated(Action action)
        {
            action.Invoke();
            eventBroker.NotifySettingsUpdated(settings);
        }
    }
}
