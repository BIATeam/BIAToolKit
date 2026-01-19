namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Application.Services;

    /// <summary>
    /// Helper to encapsulate DtoGenerator business logic for testability and SRP.
    /// </summary>
    public class DtoGeneratorHelper
    {
        private readonly CSharpParserService parserService;
        private readonly CRUDSettings settings;
        private readonly IConsoleWriter consoleWriter;

        private string dtoGenerationHistoryFile;
        private DtoGenerationHistory generationHistory = new();
        private DtoGeneration generation;

        public DtoGeneratorHelper(CSharpParserService parserService, CRUDSettings settings, IConsoleWriter consoleWriter)
        {
            this.parserService = parserService;
            this.settings = settings;
            this.consoleWriter = consoleWriter;
        }

        public void InitProject(Project project, DtoGeneratorViewModel vm)
        {
            vm.SetProject(project);
            dtoGenerationHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, settings.DtoGenerationHistoryFileName);
            if (File.Exists(dtoGenerationHistoryFile))
            {
                generationHistory = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoGenerationHistoryFile);
            }
        }

        public Task ListEntitiesAsync(Project project, DtoGeneratorViewModel vm)
        {
            var domainEntities = parserService.GetDomainEntities(project).ToList();
            vm.SetEntities(domainEntities);
            return Task.CompletedTask;
        }

        public void UpdateHistoryFile(DtoGeneratorViewModel vm)
        {
            var isNewGeneration = generation is null;
            generation ??= new DtoGeneration();

            generation.DateTime = DateTime.Now;
            generation.EntityName = vm.Entity.Name;
            generation.EntityNamespace = vm.Entity.Namespace;
            generation.Domain = vm.EntityDomain;
            generation.AncestorTeam = vm.AncestorTeam;
            generation.IsTeam = vm.IsTeam;
            generation.IsVersioned = vm.IsVersioned;
            generation.IsArchivable = vm.IsArchivable;
            generation.IsFixable = vm.IsFixable;
            generation.EntityBaseKeyType = vm.SelectedBaseKeyType;
            generation.UseDedicatedAudit = vm.UseDedicatedAudit;
            generation.PropertyMappings.Clear();

            foreach (var property in vm.MappingEntityProperties)
            {
                var generationPropertyMapping = new DtoGenerationPropertyMapping
                {
                    DateType = property.MappingDateType,
                    EntityPropertyCompositeName = property.EntityCompositeName,
                    IsRequired = property.IsRequired,
                    MappingName = property.MappingName,
                    OptionMappingDisplayProperty = property.OptionDisplayProperty,
                    OptionMappingEntityIdProperty = property.OptionEntityIdProperty,
                    OptionMappingIdProperty = property.OptionIdProperty,
                    IsParent = property.IsParent
                };
                generation.PropertyMappings.Add(generationPropertyMapping);
            }

            if (isNewGeneration)
            {
                generationHistory.Generations.Add(generation);
            }

            var historyDirectory = Path.GetDirectoryName(dtoGenerationHistoryFile);
            if (!Directory.Exists(historyDirectory))
            {
                Directory.CreateDirectory(historyDirectory!);
            }
            CommonTools.SerializeToJsonFile(generationHistory, dtoGenerationHistoryFile);
        }

        public void LoadFromHistory(EntityInfo entity, DtoGeneratorViewModel vm)
        {
            generation = generationHistory.Generations.FirstOrDefault(g =>
                g.EntityName.Equals(entity.Name) && g.EntityNamespace.Equals(entity.Namespace));

            if (generation is null)
            {
                vm.WasAlreadyGenerated = false;
                return;
            }

            vm.WasAlreadyGenerated = true;
            vm.EntityDomain = generation.Domain;
            vm.AncestorTeam = generation.AncestorTeam;
            vm.IsTeam = generation.IsTeam;
            vm.IsVersioned = generation.IsVersioned;
            vm.IsFixable = generation.IsFixable;
            vm.IsArchivable = generation.IsArchivable;
            vm.SelectedBaseKeyType = generation.EntityBaseKeyType;
            vm.UseDedicatedAudit = generation.UseDedicatedAudit;

            var allEntityProperties = vm.AllEntityPropertiesRecursively.ToList();
            foreach (var property in allEntityProperties)
            {
                property.IsSelected = false;
            }
            foreach (var property in generation.PropertyMappings)
            {
                var entityProperty = allEntityProperties.FirstOrDefault(x => x.CompositeName == property.EntityPropertyCompositeName);
                if (entityProperty is null)
                    continue;

                entityProperty.IsSelected = true;
            }

            vm.RefreshMappingProperties();

            foreach (var property in generation.PropertyMappings)
            {
                var mappingProperty = vm.MappingEntityProperties.FirstOrDefault(x => x.EntityCompositeName == property.EntityPropertyCompositeName);
                if (mappingProperty is null)
                    continue;

                mappingProperty.MappingName = property.MappingName;
                mappingProperty.MappingDateType = property.DateType;
                mappingProperty.IsRequired = property.IsRequired;
                mappingProperty.OptionIdProperty = property.OptionMappingIdProperty;
                mappingProperty.OptionDisplayProperty = property.OptionMappingDisplayProperty;
                mappingProperty.OptionEntityIdProperty = property.OptionMappingEntityIdProperty;
                mappingProperty.IsParent = property.IsParent;
            }

            vm.ComputePropertiesValidity();
        }
    }
}
