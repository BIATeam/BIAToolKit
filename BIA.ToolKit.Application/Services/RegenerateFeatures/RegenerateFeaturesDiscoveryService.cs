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

    public class RegenerateFeaturesDiscoveryService(IConsoleWriter consoleWriter, SettingsService settingsService)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly CRUDSettings crudSettings = new(settingsService);

        public List<RegenerableEntity> DiscoverRegenerableEntities(Project project)
        {
            var entities = new Dictionary<string, RegenerableEntity>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Load CRUD history
                string crudHistoryFile = Path.Combine(project.Folder, Constants.FolderBia, crudSettings.CrudGenerationHistoryFileName);
                CRUDGeneration crudGeneration = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFile);
                if (crudGeneration != null)
                {
                    foreach (CRUDGenerationHistory entry in crudGeneration.CRUDGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
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
                OptionGeneration optionGeneration = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFile);
                if (optionGeneration != null)
                {
                    foreach (OptionGenerationHistory entry in optionGeneration.OptionGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
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
                DtoGenerationHistory dtoGenerationHistory = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(dtoHistoryFile);
                if (dtoGenerationHistory != null)
                {
                    foreach (DtoGeneration entry in dtoGenerationHistory.Generations)
                    {
                        if (string.IsNullOrEmpty(entry.EntityName))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityName, null);
                        entity.DtoHistory = entry;
                        entity.DtoStatus = ValidateDtoHistory(entry, project);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading DTO generation history: {ex.Message}", "orange");
            }

            return [.. entities.Values
                .Where(e => e.HasAnyGeneratedFeature)
                .OrderBy(e => e.EntityNameSingular)];
        }

        private static RegenerableEntity GetOrCreate(Dictionary<string, RegenerableEntity> entities, string entityNameSingular, string entityNamePlural)
        {
            if (!entities.TryGetValue(entityNameSingular, out RegenerableEntity entity))
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

                string filePath = Path.Combine(project.Folder, Constants.FolderDotNet, entry.Mapping.Dto);
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
                if (string.IsNullOrEmpty(entry.EntityNameSingular) || string.IsNullOrEmpty(entry.Domain))
                    return RegenerableFeatureStatus.Missing;

                string domainFolder = $"{project.CompanyName}.{project.Name}.Domain";
                string entityPath = Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, entry.Domain, "Entities", $"{entry.EntityNameSingular}.cs");
                return File.Exists(entityPath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
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
                if (string.IsNullOrEmpty(entry.EntityName) || string.IsNullOrEmpty(entry.Domain))
                    return RegenerableFeatureStatus.Missing;

                string domainFolder = $"{project.CompanyName}.{project.Name}.Domain";
                string entityPath = Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, entry.Domain, "Entities", $"{entry.EntityName}.cs");
                return File.Exists(entityPath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
            }
            catch
            {
                return RegenerableFeatureStatus.Error;
            }
        }
    }
}
