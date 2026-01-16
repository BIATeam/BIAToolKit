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

        /// <summary>
        /// Check if local path has uncommitted changes
        /// </summary>
        Task<bool> IsLocalProjectUncommittedChanges(string localPath);

        /// <summary>
        /// Get current branch name
        /// </summary>
        Task<string> GetCurrentBranchNameAsync(string localPath);

        /// <summary>
        /// Get list of branches
        /// </summary>
        Task<List<string>> GetBranchesAsync(string localPath);

        /// <summary>
        /// Commit all changes
        /// </summary>
        Task CommitAllAsync(string projectPath, string message);

        /// <summary>
        /// Create a new branch
        /// </summary>
        Task CreateBranchAsync(string projectPath, string branchName);

        /// <summary>
        /// Checkout a branch
        /// </summary>
        Task CheckoutAsync(string projectPath, string branchName);

        /// <summary>
        /// Get tags from repository
        /// </summary>
        Task<List<string>> GetTagsAsync(IRepositoryGit repositoryGit);
    }
}
