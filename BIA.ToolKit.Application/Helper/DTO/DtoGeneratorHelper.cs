namespace BIA.ToolKit.Application.Helper.DTO
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Application.Services;

    /// <summary>
    /// Helper to encapsulate DtoGenerator business logic.
    /// Migrated from BIA.ToolKit.ViewModels to Application layer.
    /// </summary>
    public class DtoGeneratorHelper
    {
        private readonly CSharpParserService parserService;
        private readonly CRUDSettings settings;
        private readonly IConsoleWriter consoleWriter;

        private string dtoGenerationHistoryFile;
        private DtoGenerationHistory generationHistory = new();
        private DtoGeneration currentGeneration;

        public DtoGeneratorHelper(CSharpParserService parserService, CRUDSettings settings, IConsoleWriter consoleWriter)
        {
            this.parserService = parserService;
            this.settings = settings;
            this.consoleWriter = consoleWriter;
        }

        /// <summary>
        /// Initializes the helper for a project.
        /// </summary>
        public void InitProject(Project project)
        {
            dtoGenerationHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, settings.DtoGenerationHistoryFileName);
            if (File.Exists(dtoGenerationHistoryFile))
            {
                generationHistory = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoGenerationHistoryFile);
            }
            else
            {
                generationHistory = new DtoGenerationHistory();
            }
        }

        /// <summary>
        /// Gets the project domain namespace.
        /// </summary>
        public static string GetProjectDomainNamespace(Project project)
        {
            if (project == null)
                return string.Empty;

            return string.Join(".", project.CompanyName, project.Name, "Domain");
        }

        /// <summary>
        /// Lists domain entities from the project.
        /// </summary>
        public Task<System.Collections.Generic.IEnumerable<EntityInfo>> ListEntitiesAsync(Project project)
        {
            var domainEntities = parserService.GetDomainEntities(project).ToList();
            return Task.FromResult<System.Collections.Generic.IEnumerable<EntityInfo>>(domainEntities);
        }

        /// <summary>
        /// Updates the history file.
        /// </summary>
        public void UpdateHistoryFile(DtoGeneration generation)
        {
            var isNewGeneration = !generationHistory.Generations.Any(g =>
                g.EntityName.Equals(generation.EntityName) && g.EntityNamespace.Equals(generation.EntityNamespace));

            if (!isNewGeneration)
            {
                var existingGeneration = generationHistory.Generations.First(g =>
                    g.EntityName.Equals(generation.EntityName) && g.EntityNamespace.Equals(generation.EntityNamespace));
                generationHistory.Generations.Remove(existingGeneration);
            }

            generationHistory.Generations.Add(generation);

            var historyDirectory = Path.GetDirectoryName(dtoGenerationHistoryFile);
            if (!Directory.Exists(historyDirectory))
            {
                Directory.CreateDirectory(historyDirectory!);
            }
            CommonTools.SerializeToJsonFile(generationHistory, dtoGenerationHistoryFile);
        }

        /// <summary>
        /// Loads generation from history for an entity.
        /// </summary>
        public DtoGeneration LoadFromHistory(EntityInfo entity)
        {
            currentGeneration = generationHistory.Generations.FirstOrDefault(g =>
                g.EntityName.Equals(entity.Name) && g.EntityNamespace.Equals(entity.Namespace));

            return currentGeneration;
        }
    }
}
