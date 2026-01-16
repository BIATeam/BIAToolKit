using System.Threading.Tasks;
using BIA.ToolKit.Domain.Model;
using BIA.ToolKit.Domain.Work;

namespace BIA.ToolKit.Application.Services
{
    /// <summary>
    /// Service interface for project creation operations
    /// </summary>
    public interface IProjectCreatorService
    {
        /// <summary>
        /// Create a new project from template
        /// </summary>
        Task Create(bool actionFinishedAtEnd, string projectPath, ProjectParameters projectParameters);
    }
}
