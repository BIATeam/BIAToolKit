namespace BIA.ToolKit.Application.Services.CRUD
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Service interface for CRUD generation operations.
    /// Encapsulates business logic extracted from CRUDGeneratorViewModel.
    /// </summary>
    public interface ICRUDGenerationService
    {
        /// <summary>
        /// Gets or sets the current project.
        /// </summary>
        Project CurrentProject { get; set; }

        /// <summary>
        /// Initializes the service for a given project.
        /// </summary>
        /// <param name="project">The project to initialize for.</param>
        /// <returns>Initialization result containing settings, feature names, history, and whether to use file generator.</returns>
        Task<CRUDInitializationResult> InitializeAsync(Project project);

        /// <summary>
        /// Lists DTO files from the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>List of entity info representing DTOs.</returns>
        IEnumerable<EntityInfo> ListDtoFiles(Project project);

        /// <summary>
        /// Parses a DTO file and extracts property information.
        /// </summary>
        /// <param name="dtoEntity">The DTO entity to parse.</param>
        /// <returns>List of property names from the DTO.</returns>
        CRUDDtoParseResult ParseDtoFile(EntityInfo dtoEntity);

        /// <summary>
        /// Generates CRUD files based on the request.
        /// </summary>
        /// <param name="request">The CRUD generation request.</param>
        /// <returns>True if generation succeeded.</returns>
        Task<bool> GenerateAsync(CRUDGenerationRequest request);

        /// <summary>
        /// Deletes the last CRUD generation.
        /// </summary>
        /// <param name="request">The deletion request.</param>
        void DeleteLastGeneration(CRUDDeletionRequest request);

        /// <summary>
        /// Deletes all BIA Toolkit annotations from the project.
        /// </summary>
        /// <param name="folders">Folders to clean.</param>
        Task DeleteAnnotationsAsync(IEnumerable<string> folders);

        /// <summary>
        /// Gets the CRUD generation history for a project.
        /// </summary>
        /// <param name="projectPath">The project path.</param>
        /// <returns>The CRUD generation history.</returns>
        CRUDGeneration GetHistory(string projectPath);

        /// <summary>
        /// Loads DTO history from the CRUD generation history.
        /// </summary>
        /// <param name="history">The CRUD generation history.</param>
        /// <param name="dtoPath">The DTO file path.</param>
        /// <returns>The specific DTO generation history.</returns>
        CRUDGenerationHistory LoadDtoHistory(CRUDGeneration history, string dtoPath);

        /// <summary>
        /// Updates the CRUD generation history.
        /// </summary>
        /// <param name="history">The generation history entry to add/update.</param>
        void UpdateHistory(CRUDGenerationHistory history);

        /// <summary>
        /// Loads front-end generation settings for a specific BIA front folder.
        /// </summary>
        /// <param name="biaFront">The BIA front folder name.</param>
        void LoadFrontSettings(string biaFront);

        /// <summary>
        /// Parses front-end domains for options.
        /// </summary>
        /// <param name="biaFront">The BIA front folder name.</param>
        /// <returns>List of option items.</returns>
        IEnumerable<string> ParseFrontDomains(string biaFront);
    }

    /// <summary>
    /// Result of CRUD service initialization.
    /// </summary>
    public class CRUDInitializationResult
    {
        public List<FeatureGenerationSettings> BackSettings { get; set; } = [];
        public List<FeatureGenerationSettings> FrontSettings { get; set; } = [];
        public List<string> FeatureNames { get; set; } = [];
        public CRUDGeneration History { get; set; }
        public bool UseFileGenerator { get; set; }
        public List<ZipFeatureType> ZipFeatureTypeList { get; set; } = [];
    }

    /// <summary>
    /// Result of DTO file parsing.
    /// </summary>
    public class CRUDDtoParseResult
    {
        public bool Success { get; set; }
        public List<string> DisplayItems { get; set; } = [];
        public string DefaultDisplayItem { get; set; }
    }

    /// <summary>
    /// Request for CRUD generation.
    /// </summary>
    public class CRUDGenerationRequest
    {
        public EntityInfo DtoEntity { get; set; }
        public string CRUDNameSingular { get; set; }
        public string CRUDNamePlural { get; set; }
        public string DisplayItem { get; set; }
        public string Domain { get; set; }
        public string FeatureName { get; set; }
        public bool HasParent { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
        public string BiaFront { get; set; }
        public bool IsWebApiSelected { get; set; }
        public bool IsFrontSelected { get; set; }
        public bool IsTeam { get; set; }
        public int TeamTypeId { get; set; }
        public int TeamRoleId { get; set; }
        public bool UseHubClient { get; set; }
        public bool HasCustomRepository { get; set; }
        public bool HasFormReadOnlyMode { get; set; }
        public string FormReadOnlyMode { get; set; }
        public bool UseImport { get; set; }
        public bool IsFixable { get; set; }
        public bool HasFixableParent { get; set; }
        public bool IsVersioned { get; set; }
        public bool IsArchivable { get; set; }
        public bool UseAdvancedFilter { get; set; }
        public string AncestorTeam { get; set; }
        public string BaseKeyType { get; set; }
        public bool DisplayHistorical { get; set; }
        public bool UseDomainUrl { get; set; }
        public List<string> SelectedOptions { get; set; } = [];
        public List<ZipFeatureType> ZipFeatureTypeList { get; set; } = [];
    }

    /// <summary>
    /// Request for CRUD deletion.
    /// </summary>
    public class CRUDDeletionRequest
    {
        public CRUDGenerationHistory History { get; set; }
        public string FeatureName { get; set; }
        public string ParentDomain { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
        public bool HasParent { get; set; }
        public List<CRUDGenerationHistory> HistoryOptions { get; set; } = [];
        public List<ZipFeatureType> ZipFeatureTypeList { get; set; } = [];
    }
}
