namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;

    /// <summary>
    /// Centralizes the collection and sorting of available template and company-file
    /// versions from the configured repositories.
    /// </summary>
    public class TemplateVersionService(SettingsService settingsService)
    {
        private readonly SettingsService settingsService = settingsService;

        /// <summary>
        /// Returns all available template <see cref="WorkRepository"/> versions, sorted
        /// by version, from every configured template repository where
        /// <see cref="IRepository.UseRepository"/> is <c>true</c>.
        /// </summary>
        public List<WorkRepository> GetAvailableTemplateVersions()
        {
            return CollectVersions(settingsService.Settings.TemplateRepositories);
        }

        /// <summary>
        /// Returns the <c>VX.Y.Z</c> <see cref="WorkRepository"/> when a
        /// <see cref="RepositoryGit"/> with <see cref="RepositoryGit.IsVersionXYZ"/>
        /// is configured, or <c>null</c> otherwise.
        /// </summary>
        public WorkRepository GetVersionXYZ()
        {
            IRepository repositoryVersionXYZ = settingsService.Settings.TemplateRepositories
                .FirstOrDefault(r => r is RepositoryGit repoGit && repoGit.IsVersionXYZ);

            return repositoryVersionXYZ is not null
                ? new WorkRepository(repositoryVersionXYZ, "VX.Y.Z")
                : null;
        }

        /// <summary>
        /// Returns all available company-file <see cref="WorkRepository"/> versions,
        /// sorted by version, from every configured company-file repository where
        /// <see cref="IRepository.UseRepository"/> is <c>true</c>.
        /// </summary>
        public List<WorkRepository> GetAvailableCompanyFileVersions()
        {
            return CollectVersions(settingsService.Settings.CompanyFilesRepositories);
        }

        private static List<WorkRepository> CollectVersions(IEnumerable<IRepository> repositories)
        {
            var result = new List<WorkRepository>();

            foreach (IRepository repository in repositories.Where(r => r.UseRepository))
            {
                foreach (Release release in repository.Releases)
                {
                    result.Add(new WorkRepository(repository, release.Name));
                }
            }

            result.Sort(new WorkRepository.VersionComparer());
            return result;
        }
    }
}
