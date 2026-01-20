namespace BIA.ToolKit.Application.Services.DTO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Services.FileGenerator.Models;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;

    /// <summary>
    /// Service implementation for DTO generation operations.
    /// Encapsulates business logic extracted from DtoGeneratorViewModel.
    /// </summary>
    public class DtoGenerationService : IDtoGenerationService
    {
        private readonly CSharpParserService parserService;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly CRUDSettings settings;

        private string dtoGenerationHistoryFile;
        private DtoGenerationHistory generationHistory = new();
        private Project currentProject;

        public string ProjectDomainNamespace { get; private set; }

        public DtoGenerationService(
            CSharpParserService parserService,
            FileGeneratorService fileGeneratorService,
            SettingsService settingsService,
            IConsoleWriter consoleWriter)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.settings = new CRUDSettings(settingsService ?? throw new ArgumentNullException(nameof(settingsService)));
        }

        /// <inheritdoc/>
        public void Initialize(Project project)
        {
            currentProject = project;
            ProjectDomainNamespace = GetProjectDomainNamespace(project);

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

        /// <inheritdoc/>
        public Task<IEnumerable<EntityInfo>> ListEntitiesAsync(Project project)
        {
            var domainEntities = parserService.GetDomainEntities(project).ToList();
            ComputeBaseKeyType(domainEntities);
            return Task.FromResult<IEnumerable<EntityInfo>>(domainEntities);
        }

        /// <inheritdoc/>
        public async Task GenerateAsync(DtoGenerationRequest request)
        {
            if (request.Project is null)
                return;

            await fileGeneratorService.GenerateDtoAsync(new FileGeneratorDtoContext
            {
                CompanyName = request.Project.CompanyName,
                ProjectName = request.Project.Name,
                DomainName = request.EntityDomain,
                EntityName = request.Entity.Name,
                EntityNamePlural = request.Entity.NamePluralized,
                BaseKeyType = request.BaseKeyType,
                Properties = [.. request.MappingProperties],
                IsTeam = request.IsTeam,
                IsVersioned = request.IsVersioned,
                IsArchivable = request.IsArchivable,
                IsFixable = request.IsFixable,
                AncestorTeamName = request.AncestorTeam,
                HasAncestorTeam = !string.IsNullOrEmpty(request.AncestorTeam),
                GenerateBack = true,
                HasAudit = request.UseDedicatedAudit
            });
        }

        /// <inheritdoc/>
        public DtoGeneration LoadFromHistory(EntityInfo entity)
        {
            return generationHistory.Generations.FirstOrDefault(g =>
                g.EntityName.Equals(entity.Name) && g.EntityNamespace.Equals(entity.Namespace));
        }

        /// <inheritdoc/>
        public void UpdateHistory(DtoGeneration generation)
        {
            var existingGeneration = generationHistory.Generations.FirstOrDefault(g =>
                g.EntityName.Equals(generation.EntityName) && g.EntityNamespace.Equals(generation.EntityNamespace));

            if (existingGeneration != null)
            {
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

        private static string GetProjectDomainNamespace(Project project)
        {
            if (project == null)
                return string.Empty;

            return string.Join(".", project.CompanyName, project.Name, "Domain");
        }

        private static void ComputeBaseKeyType(List<EntityInfo> entities)
        {
            foreach (var entity in entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.BaseKeyType))
                    continue;

                var baseEntityInfo = GetBaseEntityInfoWithNonEmptyBaseKeyType(entity, entities);
                if (baseEntityInfo != null)
                {
                    entity.BaseKeyType = baseEntityInfo.BaseKeyType;
                }
            }
        }

        private static EntityInfo GetBaseEntityInfoWithNonEmptyBaseKeyType(EntityInfo entityInfo, List<EntityInfo> entities)
        {
            var baseTypeEntityInfo = entities.FirstOrDefault(e => entityInfo.BaseList.Contains(e.Name));
            if (baseTypeEntityInfo != null)
            {
                return string.IsNullOrWhiteSpace(baseTypeEntityInfo.BaseKeyType) ?
                    GetBaseEntityInfoWithNonEmptyBaseKeyType(baseTypeEntityInfo, entities) :
                    baseTypeEntityInfo;
            }
            return null;
        }
    }
}
