namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    public class RegenerateFeaturesDiscoveryService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly CRUDSettings crudSettings;

        public RegenerateFeaturesDiscoveryService(IConsoleWriter consoleWriter, SettingsService settingsService)
        {
            this.consoleWriter = consoleWriter;
            this.crudSettings = new CRUDSettings(settingsService);
        }

        public List<RegenerableEntity> DiscoverRegenerableEntities(Project project)
        {
            var entities = new Dictionary<string, RegenerableEntity>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Load CRUD history
                string crudHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, crudSettings.CrudGenerationHistoryFileName);
                var crudGeneration = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFile);
                if (crudGeneration != null)
                {
                    foreach (var entry in crudGeneration.CRUDGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        var entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
                        entity.CrudHistory = entry;
                        entity.CrudStatus = ValidateCrudHistory(entry, project);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading CRUD generation history: {ex.Message}", "orange");
            }

            try
            {
                // Load Option history
                string optionHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, crudSettings.OptionGenerationHistoryFileName);
                var optionGeneration = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFile);
                if (optionGeneration != null)
                {
                    foreach (var entry in optionGeneration.OptionGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        var entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
                        entity.OptionHistory = entry;
                        entity.OptionStatus = ValidateOptionHistory(entry, project);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading Option generation history: {ex.Message}", "orange");
            }

            try
            {
                // Load DTO history
                string dtoHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, crudSettings.DtoGenerationHistoryFileName);
                var dtoGenerationHistory = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoHistoryFile);
                if (dtoGenerationHistory != null)
                {
                    foreach (var entry in dtoGenerationHistory.Generations)
                    {
                        if (string.IsNullOrEmpty(entry.EntityName))
                            continue;

                        var entity = GetOrCreate(entities, entry.EntityName, null);
                        entity.DtoHistory = entry;
                        entity.DtoStatus = ValidateDtoHistory(entry, project);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading DTO generation history: {ex.Message}", "orange");
            }

            return entities.Values
                .Where(e => e.HasAnyGeneratedFeature)
                .OrderBy(e => e.EntityNameSingular)
                .ToList();
        }

        private static RegenerableEntity GetOrCreate(Dictionary<string, RegenerableEntity> entities, string entityNameSingular, string entityNamePlural)
        {
            if (!entities.TryGetValue(entityNameSingular, out var entity))
            {
                entity = new RegenerableEntity
                {
                    EntityNameSingular = entityNameSingular,
                    EntityNamePlural = entityNamePlural ?? entityNameSingular,
                };
                entities[entityNameSingular] = entity;
            }
            else if (!string.IsNullOrEmpty(entityNamePlural) && string.IsNullOrEmpty(entity.EntityNamePlural))
            {
                entity.EntityNamePlural = entityNamePlural;
            }

            return entity;
        }

        private static RegenerableFeatureStatus ValidateCrudHistory(CRUDGenerationHistory entry, Project project)
        {
            try
            {
                if (entry.Mapping?.Dto == null)
                    return RegenerableFeatureStatus.Missing;

                string filePath = Path.Combine(project.Folder, entry.Mapping.Dto);
                return File.Exists(filePath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
            }
            catch
            {
                return RegenerableFeatureStatus.Error;
            }
        }

        private static RegenerableFeatureStatus ValidateOptionHistory(OptionGenerationHistory entry, Project project)
        {
            try
            {
                if (entry.Mapping?.Entity == null)
                    return RegenerableFeatureStatus.Missing;

                string filePath = Path.Combine(project.Folder, entry.Mapping.Entity);
                return File.Exists(filePath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
            }
            catch
            {
                return RegenerableFeatureStatus.Error;
            }
        }

        private static RegenerableFeatureStatus ValidateDtoHistory(DtoGeneration entry, Project project)
        {
            try
            {
                if (string.IsNullOrEmpty(entry.EntityName))
                    return RegenerableFeatureStatus.Missing;

                string dtoFileName = $"{entry.EntityName}Dto.cs";
                string dtoFolder = $"{project.CompanyName}.{project.Name}.Domain.Dto";
                string filePath = Path.Combine(project.Folder, Constants.FolderDotNet, dtoFolder, dtoFileName);
                return File.Exists(filePath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
            }
            catch
            {
                return RegenerableFeatureStatus.Error;
            }
        }
    }
}
