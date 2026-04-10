namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Owns the current <see cref="BIATKSettings"/> and delegates reading/writing
    /// to an injected <see cref="ISettingsPersistence"/> so that the Application
    /// layer remains unaware of any specific storage backend.
    /// </summary>
    public class SettingsService(IConsoleWriter consoleWriter, ISettingsPersistence persistence)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly ISettingsPersistence persistence = persistence;
        private BIATKSettings settings = new();
        public IBIATKSettings Settings => settings;

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

        /// <summary>
        /// Loads settings from persistence and wires up repository interfaces.
        /// The returned instance is the one the service will keep and broadcast.
        /// Call <see cref="NotifyInitialized"/> once the caller has finished any
        /// asynchronous post-load work to fire the initial update.
        /// </summary>
        public BIATKSettings Load()
        {
            settings = persistence.Load();
            settings.InitRepositoriesInterfaces();
            return settings;
        }

        /// <summary>
        /// Broadcasts a <see cref="SettingsUpdatedMessage"/> for the currently
        /// loaded settings. Intended to be called after <see cref="Load"/> and
        /// any startup enrichment step (e.g. fetching repository releases).
        /// </summary>
        public void NotifyInitialized()
        {
            WeakReferenceMessenger.Default.Send(new SettingsUpdatedMessage(settings));
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
            ExecuteAndNotifySettingsUpdated(() =>
            {
                settings.ToolkitRepository = repository;
                settings.ToolkitRepositoryConfig = repository.ToRepositoryConfig();
            });
        }

        public void SetTemplateRepositories(IReadOnlyList<IRepository> repositories)
        {
            ExecuteAndNotifySettingsUpdated(() =>
            {
                settings.TemplateRepositories = repositories;
                settings.TemplateRepositoriesConfig = [.. repositories.Select(r => r.ToRepositoryConfig())];
            });
        }

        public void SetCompanyFilesRepositories(IReadOnlyList<IRepository> repositories)
        {
            ExecuteAndNotifySettingsUpdated(() =>
            {
                settings.CompanyFilesRepositories = repositories;
                settings.CompanyFilesRepositoriesConfig = [.. repositories.Select(r => r.ToRepositoryConfig())];
            });
        }

        private void ExecuteAndNotifySettingsUpdated(Action action)
        {
            action.Invoke();
            persistence.Save(settings);
            WeakReferenceMessenger.Default.Send(new SettingsUpdatedMessage(settings));
        }
    }
}
