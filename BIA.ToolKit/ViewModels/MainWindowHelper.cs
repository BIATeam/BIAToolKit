namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper class for MainWindow initialization and validation logic.
    /// Applies SRP by separating initialization concerns from UI.
    /// </summary>
    public class MainWindowHelper
    {
        private readonly UpdateService updateService;
        private readonly CSharpParserService cSharpParserService;
        private readonly SettingsService settingsService;
        private readonly GitService gitService;
        private readonly RepositoryService repositoryService;
        private readonly IConsoleWriter consoleWriter;

        public MainWindowHelper(
            UpdateService updateService,
            CSharpParserService cSharpParserService,
            SettingsService settingsService,
            GitService gitService,
            RepositoryService repositoryService,
            IConsoleWriter consoleWriter)
        {
            this.updateService = updateService;
            this.cSharpParserService = cSharpParserService;
            this.settingsService = settingsService;
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.consoleWriter = consoleWriter;
        }

        /// <summary>
        /// Initializes application settings from Properties.
        /// </summary>
        public async Task<BIATKSettings> InitializeSettingsAsync()
        {
            var properties = BIA.ToolKit.Properties.Settings.Default;

            var settings = new BIATKSettings
            {
                UseCompanyFiles = properties.UseCompanyFile,
                CreateProjectRootProjectsPath = properties.CreateProjectRootFolderText,
                CreateCompanyName = properties.CreateCompanyName,
                ModifyProjectRootProjectsPath = properties.ModifyProjectRootFolderText,
                AutoUpdate = properties.AutoUpdate,

                ToolkitRepositoryConfig = !string.IsNullOrEmpty(properties.ToolkitRepository) ?
                    JsonConvert.DeserializeObject<RepositoryUserConfig>(properties.ToolkitRepository) :
                    CreateDefaultToolkitRepository(),

                TemplateRepositoriesConfig = !string.IsNullOrEmpty(properties.TemplateRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(properties.TemplateRepositories) :
                    CreateDefaultTemplateRepositories(),

                CompanyFilesRepositoriesConfig = !string.IsNullOrEmpty(properties.CompanyFilesRepositories) ?
                    JsonConvert.DeserializeObject<List<RepositoryUserConfig>>(properties.CompanyFilesRepositories) :
                    new List<RepositoryUserConfig>(),
            };

            settings.InitRepositoriesInterfaces();
            await FetchReleaseDataAsync(settings, syncBefore: false);

            return settings;
        }

        /// <summary>
        /// Fetches release data for all active repositories (DRY principle).
        /// </summary>
        public async Task FetchReleaseDataAsync(BIATKSettings settings, bool syncBefore = false)
        {
            var repositories = settings.TemplateRepositories
                .Concat(settings.CompanyFilesRepositories)
                .Where(r => r.UseRepository)
                .ToList();

            var tasks = repositories.Select(r => FillRepositoryReleasesAsync(r, syncBefore));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Fills releases for a single repository (extracted for clarity - KISS).
        /// </summary>
        private async Task FillRepositoryReleasesAsync(IRepository repository, bool syncBefore)
        {
            try
            {
                if (syncBefore && repository is IRepositoryGit repoGit)
                {
                    consoleWriter.AddMessageLine($"Synchronizing repository {repository.Name}...", "pink");
                    await gitService.Synchronize(repoGit);
                    consoleWriter.AddMessageLine($"Synchronized successfully repository {repository.Name}", "green");
                }

                consoleWriter.AddMessageLine($"Getting releases data for repository {repository.Name}...", "pink");
                await repository.FillReleasesAsync();
                consoleWriter.AddMessageLine($"Releases data got successfully for repository {repository.Name}", "green");

                if (repository.UseDownloadedReleases)
                {
                    consoleWriter.AddMessageLine(
                        $"WARNING: Releases data got from downloaded releases for repository {repository.Name}",
                        "orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(
                    $"Error while processing repository {repository.Name}: {ex.Message}",
                    "red");
            }
        }

        /// <summary>
        /// Validates all repository configurations (DRY - extracted common logic).
        /// </summary>
        public bool ValidateRepositoriesConfiguration(IBIATKSettings settings)
        {
            var templatesValid = ValidateTemplateRepositories(settings);
            var companyFilesValid = ValidateCompanyFilesRepositories(settings);
            return templatesValid && companyFilesValid;
        }

        /// <summary>
        /// Validates template repositories.
        /// </summary>
        public bool ValidateTemplateRepositories(IBIATKSettings settings)
        {
            if (!ValidateRepositoryCollection(
                settings.TemplateRepositories.Where(r => r.UseRepository),
                "Templates"))
            {
                return false;
            }

            var repositoryVersionXYZ = settings.TemplateRepositories.FirstOrDefault(
                r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);

            if (repositoryVersionXYZ != null)
            {
                if (!repositoryService.CheckRepoFolder(repositoryVersionXYZ))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates company files repositories.
        /// </summary>
        public bool ValidateCompanyFilesRepositories(IBIATKSettings settings)
        {
            if (!settings.UseCompanyFiles)
            {
                return true;
            }

            return ValidateRepositoryCollection(
                settings.CompanyFilesRepositories.Where(r => r.UseRepository),
                "Company Files");
        }

        /// <summary>
        /// Generic repository collection validation (DRY principle).
        /// </summary>
        private bool ValidateRepositoryCollection(
            IEnumerable<IRepository> repositories,
            string repositoryTypeName)
        {
            if (!repositories.Any())
            {
                consoleWriter.AddMessageLine(
                    $"You must use at least one {repositoryTypeName} repository",
                    "red");
                return false;
            }

            foreach (var repository in repositories)
            {
                if (!repositoryService.CheckRepoFolder(repository))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates default BIAToolkit repository configuration (YAGNI - only essentials).
        /// </summary>
        private static RepositoryUserConfig CreateDefaultToolkitRepository()
        {
            return new RepositoryUserConfig
            {
                Name = "BIAToolkit GIT",
                RepositoryType = RepositoryType.Git,
                RepositoryGitKind = RepositoryGitKind.Github,
                Url = "https://github.com/BIATeam/BIAToolKit",
                GitRepositoryName = "BIAToolKit",
                Owner = "BIATeam",
                UseRepository = true
            };
        }

        /// <summary>
        /// Creates default template repositories configuration.
        /// </summary>
        private static List<RepositoryUserConfig> CreateDefaultTemplateRepositories()
        {
            return new List<RepositoryUserConfig>
            {
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
            };
        }
    }
}
