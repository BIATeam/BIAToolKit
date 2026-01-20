namespace BIA.ToolKit.Application.Services.ProjectMigration
{
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.ModifyProject;

    /// <summary>
    /// Service interface for project migration operations.
    /// Encapsulates business logic extracted from ModifyProjectViewModel.
    /// </summary>
    public interface IProjectMigrationService
    {
        /// <summary>
        /// Gets or sets the current project.
        /// </summary>
        Project CurrentProject { get; set; }

        /// <summary>
        /// Loads a project and resolves its metadata.
        /// </summary>
        /// <param name="project">The project to load.</param>
        Task LoadProjectAsync(Project project);

        /// <summary>
        /// Parses the project solution.
        /// </summary>
        /// <param name="project">The project to parse.</param>
        Task ParseProjectAsync(Project project);

        /// <summary>
        /// Performs full migration: generate, apply diff, merge rejected.
        /// </summary>
        /// <param name="request">The migration request.</param>
        /// <returns>Migration result.</returns>
        Task<MigrationResult> MigrateAsync(MigrationRequest request);

        /// <summary>
        /// Generates migration projects only.
        /// </summary>
        /// <param name="request">The migration request.</param>
        /// <returns>0 if successful, -1 on error.</returns>
        Task<int> GenerateOnlyAsync(MigrationRequest request);

        /// <summary>
        /// Applies diff to the project.
        /// </summary>
        /// <param name="request">The migration request.</param>
        /// <returns>True if successful.</returns>
        Task<bool> ApplyDiffAsync(MigrationRequest request);

        /// <summary>
        /// Merges rejected files.
        /// </summary>
        /// <param name="request">The migration request.</param>
        Task MergeRejectedAsync(MigrationRequest request);

        /// <summary>
        /// Overwrites BIA folder from target.
        /// </summary>
        /// <param name="request">The migration request.</param>
        Task OverwriteBIAFolderAsync(MigrationRequest request);

        /// <summary>
        /// Fixes usings in the project.
        /// </summary>
        Task FixUsingsAsync();

        /// <summary>
        /// Gets the migration patch file path.
        /// </summary>
        string GetMigrationPatchFilePath(string originalFolderName, string targetFolderName);
    }

    /// <summary>
    /// Request for project migration.
    /// </summary>
    public class MigrationRequest
    {
        public Project Project { get; set; }
        public string OriginVersion { get; set; }
        public string TargetVersion { get; set; }
        public bool OverwriteBIAFromOriginal { get; set; }
    }

    /// <summary>
    /// Result of migration operation.
    /// </summary>
    public class MigrationResult
    {
        public bool Success { get; set; }
        public int GenerateResult { get; set; }
        public bool ApplyDiffResult { get; set; }
        public string Message { get; set; }
    }
}
