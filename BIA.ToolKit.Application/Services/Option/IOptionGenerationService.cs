namespace BIA.ToolKit.Application.Services.Option
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Service interface for Option generation operations.
    /// Encapsulates business logic extracted from OptionGeneratorViewModel.
    /// </summary>
    public interface IOptionGenerationService
    {
        /// <summary>
        /// Gets or sets the current project.
        /// </summary>
        Project CurrentProject { get; set; }

        /// <summary>
        /// Initializes the service for a given project.
        /// </summary>
        /// <param name="project">The project to initialize for.</param>
        /// <returns>Initialization result containing settings and history.</returns>
        Task<OptionInitializationResult> InitializeAsync(Project project);

        /// <summary>
        /// Lists entity files from the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>List of entity info.</returns>
        IEnumerable<EntityInfo> ListEntities(Project project);

        /// <summary>
        /// Parses an entity file and extracts property information.
        /// </summary>
        /// <param name="entity">The entity to parse.</param>
        /// <returns>Parse result with domain and properties.</returns>
        OptionEntityParseResult ParseEntityFile(EntityInfo entity);

        /// <summary>
        /// Generates Option files based on the request.
        /// </summary>
        /// <param name="request">The Option generation request.</param>
        /// <returns>True if generation succeeded.</returns>
        Task<bool> GenerateAsync(OptionGenerationRequest request);

        /// <summary>
        /// Deletes the last Option generation.
        /// </summary>
        /// <param name="request">The deletion request.</param>
        void DeleteLastGeneration(OptionDeletionRequest request);

        /// <summary>
        /// Deletes all BIA Toolkit annotations from the project.
        /// </summary>
        /// <param name="folders">Folders to clean.</param>
        Task DeleteAnnotationsAsync(IEnumerable<string> folders);

        /// <summary>
        /// Gets the Option generation history for a project.
        /// </summary>
        /// <returns>The Option generation history.</returns>
        OptionGeneration GetHistory();

        /// <summary>
        /// Loads entity history from the Option generation history.
        /// </summary>
        /// <param name="entityPath">The entity file path.</param>
        /// <returns>The specific entity generation history.</returns>
        OptionGenerationHistory LoadEntityHistory(string entityPath);

        /// <summary>
        /// Updates the Option generation history.
        /// </summary>
        /// <param name="history">The generation history entry to add/update.</param>
        void UpdateHistory(OptionGenerationHistory history);

        /// <summary>
        /// Loads front-end generation settings for a specific BIA front folder.
        /// </summary>
        /// <param name="biaFront">The BIA front folder name.</param>
        void LoadFrontSettings(string biaFront);
    }

    /// <summary>
    /// Result of Option service initialization.
    /// </summary>
    public class OptionInitializationResult
    {
        public List<FeatureGenerationSettings> BackSettings { get; set; } = [];
        public List<FeatureGenerationSettings> FrontSettings { get; set; } = [];
        public OptionGeneration History { get; set; }
        public List<ZipFeatureType> ZipFeatureTypeList { get; set; } = [];
    }

    /// <summary>
    /// Result of entity file parsing.
    /// </summary>
    public class OptionEntityParseResult
    {
        public bool Success { get; set; }
        public string Domain { get; set; }
        public string EntityNamePlural { get; set; }
        public List<string> DisplayItems { get; set; } = [];
        public string DefaultDisplayItem { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request for Option generation.
    /// </summary>
    public class OptionGenerationRequest
    {
        public EntityInfo Entity { get; set; }
        public string EntityNamePlural { get; set; }
        public string DisplayItem { get; set; }
        public string Domain { get; set; }
        public string BiaFront { get; set; }
        public bool UseHubForClient { get; set; }
    }

    /// <summary>
    /// Request for Option deletion.
    /// </summary>
    public class OptionDeletionRequest
    {
        public OptionGenerationHistory History { get; set; }
        public string Domain { get; set; }
    }
}
