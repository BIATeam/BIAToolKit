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

                        // Extract dependency metadata from CRUD history
                        if (entry.HasParent && !string.IsNullOrEmpty(entry.ParentName))
                            entity.ParentEntityName = entry.ParentName;

                        if (entry.OptionItems?.Count > 0)
                            entity.OptionDependencies = [.. entry.OptionItems];
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

                        // Extract option dependencies from DTO property mappings when not already set via CRUD
                        if (entity.OptionDependencies.Count == 0 && entry.PropertyMappings?.Count > 0)
                        {
                            var optionNames = entry.PropertyMappings
                                .Where(p => !string.IsNullOrEmpty(p.OptionMappingIdProperty) || !string.IsNullOrEmpty(p.OptionMappingEntityIdProperty))
                                .Select(p => ExtractEntityNameFromProperty(p.OptionMappingIdProperty ?? p.OptionMappingEntityIdProperty))
                                .Where(n => !string.IsNullOrEmpty(n))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (optionNames.Count > 0)
                                entity.OptionDependencies = optionNames;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading DTO generation history: {ex.Message}", "orange");
            }

            RunCoherenceChecks(entities);

            return [.. entities.Values
                .Where(e => e.HasAnyGeneratedFeature)
                .OrderBy(e => e.EntityNameSingular)];
        }

        // ── Coherence checks ─────────────────────────────────────────────────

        /// <summary>
        /// Runs cross-history coherence checks and populates warning messages and blocking statuses.
        /// </summary>
        private void RunCoherenceChecks(Dictionary<string, RegenerableEntity> entities)
        {
            foreach (RegenerableEntity entity in entities.Values)
            {
                // 2b — CRUD requires a DTO history entry
                if (entity.CrudHistory != null && entity.DtoHistory == null
                    && entity.CrudStatus == RegenerableFeatureStatus.Ready)
                {
                    entity.CrudStatus = RegenerableFeatureStatus.BlockedNoDtoHistory;
                    entity.CrudWarningMessage = "Le DTO correspondant n'existe pas dans l'historique de génération. Le CRUD ne peut pas être migré sans son DTO.";
                }

                // 2c — Parent/child: check whether the parent entity exists in history
                if (!string.IsNullOrEmpty(entity.ParentEntityName))
                {
                    if (entities.ContainsKey(entity.ParentEntityName))
                    {
                        // Parent found in history — dynamic blocking will be handled in the ViewModel
                        entity.HasParentDependency = true;
                    }
                    else
                    {
                        // Parent absent from history — static blocking
                        string missingParentMsg = $"L'entité parente '{entity.ParentEntityName}' n'est pas présente dans l'historique de génération.";

                        if (entity.CrudStatus == RegenerableFeatureStatus.Ready)
                        {
                            entity.CrudStatus = RegenerableFeatureStatus.BlockedParentNotMigrated;
                            entity.CrudWarningMessage = missingParentMsg;
                        }

                        if (entity.DtoStatus == RegenerableFeatureStatus.Ready)
                        {
                            entity.DtoStatus = RegenerableFeatureStatus.BlockedParentNotMigrated;
                            entity.DtoWarningMessage = missingParentMsg;
                        }
                    }
                }

                // 2d — Option dependencies: informational warning only
                if (entity.OptionDependencies.Count > 0)
                {
                    var missingOptions = entity.OptionDependencies
                        .Where(optName =>
                            !entities.TryGetValue(optName, out RegenerableEntity optEntity) ||
                            optEntity.OptionHistory == null ||
                            optEntity.OptionStatus != RegenerableFeatureStatus.Ready)
                        .ToList();

                    if (missingOptions.Count > 0)
                    {
                        entity.OptionWarningMessage =
                            $"Les options suivantes ne seront pas migrées automatiquement car elles ne sont pas dans l'historique : {string.Join(", ", missingOptions)}.";
                    }
                }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

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

        /// <summary>
        /// Attempts to derive an entity name from a property name by stripping a trailing "Id" suffix.
        /// e.g. "StatusId" → "Status", "RoleId" → "Role".
        /// Returns null when the derivation is not possible.
        /// </summary>
        private static string ExtractEntityNameFromProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            if (propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && propertyName.Length > 2)
                return propertyName[..^2];

            return null;
        }
    }
}
