namespace BIA.ToolKit.Infrastructure
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;

    /// <summary>
    /// WPF implementation of <see cref="ISettingsPersistence"/> backed by the
    /// auto-generated <see cref="Properties.Settings"/> file (user.config).
    /// Handles the one-time per-version settings upgrade on first load.
    /// </summary>
    public class UserPreferencesSettingsPersistence : ISettingsPersistence
    {
        private bool upgradeChecked;

        public BIATKSettings Load()
        {
            EnsureApplicationSettingsUpgraded();

            return new BIATKSettings
            {
                UseCompanyFiles = Properties.Settings.Default.UseCompanyFile,
                CreateProjectRootProjectsPath = Properties.Settings.Default.CreateProjectRootFolderText,
                CreateCompanyName = Properties.Settings.Default.CreateCompanyName,
                ModifyProjectRootProjectsPath = Properties.Settings.Default.ModifyProjectRootFolderText,
                AutoUpdate = Properties.Settings.Default.AutoUpdate,
                IsDarkTheme = Properties.Settings.Default.IsDarkTheme,

                ToolkitRepositoryConfig = !string.IsNullOrEmpty(Properties.Settings.Default.ToolkitRepository)
                    ? JsonConvert.DeserializeObject<RepositoryUserConfig>(Properties.Settings.Default.ToolkitRepository)
                    : new RepositoryUserConfig
                    {
                        Name = "BIAToolkit GIT",
                        RepositoryType = RepositoryType.Git,
                        RepositoryGitKind = RepositoryGitKind.Github,
                        Url = "https://github.com/BIATeam/BIAToolKit",
                        GitRepositoryName = "BIAToolKit",
                        Owner = "BIATeam",
                        UseRepository = true
                    },

                TemplateRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.TemplateRepositories)
                    ? JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.TemplateRepositories)
                    :
                    [
                        new RepositoryUserConfig
                        {
                            Name = "BIATemplate GIT",
                            RepositoryType = RepositoryType.Git,
                            RepositoryGitKind = RepositoryGitKind.Github,
                            Url = "https://github.com/BIATeam/BIADemo",
                            GitRepositoryName = "BIATemplate",
                            Owner = "BIATeam",
                            CompanyName = "TheBIADevCompany",
                            ProjectName = "BIATemplate",
                            UseRepository = true
                        }
                    ],

                CompanyFilesRepositoriesConfig = !string.IsNullOrEmpty(Properties.Settings.Default.CompanyFilesRepositories)
                    ? JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(Properties.Settings.Default.CompanyFilesRepositories)
                    : [],
            };
        }

        public void Save(IBIATKSettings settings)
        {
            Properties.Settings.Default.UseCompanyFile = settings.UseCompanyFiles;
            Properties.Settings.Default.CreateProjectRootFolderText = settings.CreateProjectRootProjectsPath;
            Properties.Settings.Default.ModifyProjectRootFolderText = settings.ModifyProjectRootProjectsPath;
            Properties.Settings.Default.CreateCompanyName = settings.CreateCompanyName;
            Properties.Settings.Default.AutoUpdate = settings.AutoUpdate;
            Properties.Settings.Default.IsDarkTheme = settings.IsDarkTheme;
            Properties.Settings.Default.ToolkitRepository = JsonConvert.SerializeObject(settings.ToolkitRepository);
            Properties.Settings.Default.TemplateRepositories = JsonConvert.SerializeObject(settings.TemplateRepositories);
            Properties.Settings.Default.CompanyFilesRepositories = JsonConvert.SerializeObject(settings.CompanyFilesRepositories);
            Properties.Settings.Default.Save();
        }

        private void EnsureApplicationSettingsUpgraded()
        {
            if (upgradeChecked)
                return;

            if (Properties.Settings.Default.ApplicationUpdated)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.ApplicationUpdated = false;
                Properties.Settings.Default.Save();
            }

            upgradeChecked = true;
        }
    }
}
