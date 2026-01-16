using System.Collections.Generic;
using System.Threading.Tasks;
using BIA.ToolKit.Domain;

namespace BIA.ToolKit.Application.Services
{
    /// <summary>
    /// Service interface for Git operations
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Synchronize a Git repository
        /// </summary>
        Task Synchronize(IRepositoryGit repository);

        /// <summary>
        /// Synchronize from URL to local path
        /// </summary>
        Task Synchronize(string url, string localPath);

        /// <summary>
        /// Clone a repository
        /// </summary>
        Task Clone(string url, string localPath);

        /// <summary>
        /// Diff two folders and create patch file
        /// </summary>
        Task<bool> DiffFolder(bool actionFinishedAtEnd, string rootPath, string name1, string name2, string migrateFilePath);

        /// <summary>
        /// Apply a diff/patch file
        /// </summary>
        Task<bool> ApplyDiff(bool actionFinishedAtEnd, string projectPath, string migrateFilePath);

        /// <summary>
        /// Merge rejected files after a failed patch application
        /// </summary>
        Task MergeRejected(bool actionFinishedAtEnd, GitService.MergeParameter param);
    }
}
