using System.Threading.Tasks;
using BIA.ToolKit.Domain;

namespace BIA.ToolKit.Application.Services
{
    /// <summary>
    /// Service interface for repository management operations
    /// </summary>
    public interface IRepositoryService
    {
        /// <summary>
        /// Check if repository folder exists
        /// </summary>
        bool CheckRepoFolder(IRepository repository);

        /// <summary>
        /// Clean the repository local folder
        /// </summary>
        Task CleanRepository(IRepository repository);

        /// <summary>
        /// Clean downloaded releases of repository
        /// </summary>
        Task CleanReleases(IRepository repository);

        /// <summary>
        /// Prepare version folder for a specific release
        /// </summary>
        Task<string> PrepareVersionFolder(IRepository repository, string version);
    }
}
