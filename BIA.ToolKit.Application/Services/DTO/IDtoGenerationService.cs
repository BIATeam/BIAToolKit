namespace BIA.ToolKit.Application.Services.DTO
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator.Models;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;

    /// <summary>
    /// Service interface for DTO generation operations.
    /// Encapsulates business logic extracted from DtoGeneratorViewModel.
    /// </summary>
    public interface IDtoGenerationService
    {
        /// <summary>
        /// Initializes the service for a given project.
        /// </summary>
        /// <param name="project">The project to initialize for.</param>
        void Initialize(Project project);

        /// <summary>
        /// Lists domain entities from the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>List of entity info.</returns>
        Task<IEnumerable<EntityInfo>> ListEntitiesAsync(Project project);

        /// <summary>
        /// Generates DTO files based on the request.
        /// </summary>
        /// <param name="request">The DTO generation request.</param>
        Task GenerateAsync(DtoGenerationRequest request);

        /// <summary>
        /// Loads history for a specific entity.
        /// </summary>
        /// <param name="entity">The entity to load history for.</param>
        /// <returns>The DTO generation entry if found.</returns>
        DtoGeneration LoadFromHistory(EntityInfo entity);

        /// <summary>
        /// Updates the generation history file.
        /// </summary>
        /// <param name="generation">The generation to save.</param>
        void UpdateHistory(DtoGeneration generation);

        /// <summary>
        /// Gets the project domain namespace.
        /// </summary>
        string ProjectDomainNamespace { get; }
    }

    /// <summary>
    /// Request for DTO generation.
    /// </summary>
    public class DtoGenerationRequest
    {
        public Project Project { get; set; }
        public EntityInfo Entity { get; set; }
        public string EntityDomain { get; set; }
        public string BaseKeyType { get; set; }
        public IEnumerable<MappingEntityProperty> MappingProperties { get; set; } = [];
        public bool IsTeam { get; set; }
        public bool IsVersioned { get; set; }
        public bool IsArchivable { get; set; }
        public bool IsFixable { get; set; }
        public string AncestorTeam { get; set; }
        public bool UseDedicatedAudit { get; set; }
    }
}
