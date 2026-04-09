namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    /// <summary>
    /// Discovers all entities that have at least one generation history entry (CRUD, Option, DTO)
    /// and validates their eligibility for regeneration.
    /// <para>
    /// Entity resolution (finding parsed classes, validating files) is delegated to
    /// <see cref="EntityResolutionService"/>. History loading is delegated to
    /// <see cref="GenerationHistoryService"/>. This service focuses exclusively on the
    /// discovery and cross-entity coherence logic.
    /// </para>
    /// </summary>
    public class RegenerateFeaturesDiscoveryService(
        IConsoleWriter consoleWriter,
        EntityResolutionService entityResolutionService,
        GenerationHistoryService historyService)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly EntityResolutionService entityResolutionService = entityResolutionService;
        private readonly GenerationHistoryService historyService = historyService;

        private const int RegenerateFeaturesVersionMinimum = 500;

        /// <summary>
        /// Returns <see langword="true"/> when the project framework version supports feature
        /// regeneration (>= 5.0.0).
        /// </summary>
        public static bool IsProjectCompatibleForRegenerateFeatures(Project project)
        {
            if (!string.IsNullOrEmpty(project?.FrameworkVersion))
            {
                string version = project.FrameworkVersion.Replace(".", "");
                if (int.TryParse(version, out int value))
                    return value >= RegenerateFeaturesVersionMinimum;
            }

            return false;
        }

        /// <summary>
        /// Discovers all entities that have at least one generation history entry and runs
        /// coherence checks across entities.
        /// </summary>
        public List<RegenerableEntity> DiscoverRegenerableEntities(Project project)
        {
            var entities = new Dictionary<string, RegenerableEntity>(StringComparer.OrdinalIgnoreCase);

            // Pre-fetch parsed entity information when the solution has been parsed.
            // Domain entities (used for Option and DTO validation/info) come from GetDomainEntities,
            // following the same approach as DtoGeneratorUC.ListEntities.
            // DTO entities (used for CRUD validation/info) come from CurrentSolutionClasses filtered
            // to the DTO project folder, following the same approach as CRUDGeneratorUC.ListDtoFiles.
            bool parserReady = entityResolutionService.IsParserReady;

            IReadOnlyList<EntityInfo> domainEntities = parserReady
                ? [.. entityResolutionService.GetDomainEntityInfos(project)]
                : [];

            IReadOnlyList<EntityInfo> dtoEntities = parserReady
                ? [.. entityResolutionService.GetDtoEntityInfos(project)]
                : [];

            try
            {
                CRUDGeneration crudGeneration = historyService.LoadCrudHistory(project);
                if (crudGeneration != null)
                {
                    foreach (CRUDGenerationHistory entry in crudGeneration.CRUDGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
                        entity.CrudHistory = entry;
                        (entity.CrudStatus, entity.CrudEntityInfo) = entityResolutionService.ValidateCrudHistory(entry, project, dtoEntities);

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
                OptionGeneration optionGeneration = historyService.LoadOptionHistory(project);
                if (optionGeneration != null)
                {
                    foreach (OptionGenerationHistory entry in optionGeneration.OptionGenerationHistory)
                    {
                        if (string.IsNullOrEmpty(entry.EntityNameSingular))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityNameSingular, entry.EntityNamePlural);
                        entity.OptionHistory = entry;
                        (entity.OptionStatus, entity.OptionEntityInfo) = entityResolutionService.ValidateOptionHistory(entry, project, domainEntities);
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading Option generation history: {ex.Message}", "orange");
            }

            try
            {
                DtoGenerationHistory dtoGenerationHistory = historyService.LoadDtoHistory(project);
                if (dtoGenerationHistory != null)
                {
                    foreach (DtoGeneration entry in dtoGenerationHistory.Generations)
                    {
                        if (string.IsNullOrEmpty(entry.EntityName))
                            continue;

                        RegenerableEntity entity = GetOrCreate(entities, entry.EntityName, null);
                        entity.DtoHistory = entry;
                        (entity.DtoStatus, entity.DtoEntityInfo) = entityResolutionService.ValidateDtoHistory(entry, project, domainEntities);

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
        private static void RunCoherenceChecks(Dictionary<string, RegenerableEntity> entities)
        {
            foreach (RegenerableEntity entity in entities.Values)
            {
                // 1 — Missing model file warnings (set before cross-entity checks so they can be overridden)
                if (entity.CrudHistory != null && entity.CrudStatus == RegenerableFeatureStatus.Missing
                    && string.IsNullOrEmpty(entity.CrudWarningMessage))
                {
                    entity.CrudWarningMessage = entity.CrudHistory.Mapping?.Dto != null
                        ? $"The model file '{entity.CrudHistory.Mapping.Dto}' was not found in the current project. The CRUD feature cannot be regenerated."
                        : "The CRUD generation history is incomplete (missing DTO path). The CRUD feature cannot be regenerated.";
                }

                if (entity.OptionHistory != null && entity.OptionStatus == RegenerableFeatureStatus.Missing
                    && string.IsNullOrEmpty(entity.OptionWarningMessage))
                {
                    entity.OptionWarningMessage = !string.IsNullOrEmpty(entity.OptionHistory.EntityNameSingular)
                        ? $"The model file '{entity.OptionHistory.EntityNameSingular}.cs' was not found in the current project. The Option feature cannot be regenerated."
                        : "The Option generation history is incomplete (missing entity name). The Option feature cannot be regenerated.";
                }

                if (entity.DtoHistory != null && entity.DtoStatus == RegenerableFeatureStatus.Missing
                    && string.IsNullOrEmpty(entity.DtoWarningMessage))
                {
                    entity.DtoWarningMessage = !string.IsNullOrEmpty(entity.DtoHistory.EntityName)
                        ? $"The model file '{entity.DtoHistory.EntityName}.cs' was not found in the current project. The DTO feature cannot be regenerated."
                        : "The DTO generation history is incomplete (missing entity name). The DTO feature cannot be regenerated.";
                }

                // 2b — CRUD requires a DTO history entry
                if (entity.CrudHistory != null && entity.DtoHistory == null
                    && entity.CrudStatus == RegenerableFeatureStatus.Ready)
                {
                    entity.CrudStatus = RegenerableFeatureStatus.BlockedNoDtoHistory;
                    entity.CrudWarningMessage = "The corresponding DTO does not exist in the generation history. The CRUD feature cannot be regenerated without its DTO.";
                }

                // 2c — Parent/child: check whether the parent entity exists in history
                if (!string.IsNullOrEmpty(entity.ParentEntityName))
                {
                    if (entities.ContainsKey(entity.ParentEntityName))
                    {
                        entity.HasParentDependency = true;
                    }
                    else
                    {
                        string missingParentMsg = $"The parent entity '{entity.ParentEntityName}' is not present in the generation history.";

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
                            $"The following options will not be automatically migrated as they are not present in the history: {string.Join(", ", missingOptions)}.";
                    }
                }
            }

            // Phase 2 — Propagate parent blocking to children.
            bool propagationChanged;
            do
            {
                propagationChanged = false;

                foreach (RegenerableEntity entity in entities.Values)
                {
                    if (!entity.HasParentDependency || string.IsNullOrEmpty(entity.ParentEntityName))
                        continue;

                    if (!entities.TryGetValue(entity.ParentEntityName, out RegenerableEntity parent))
                        continue;

                    if (entity.DtoStatus == RegenerableFeatureStatus.Ready && !parent.CanRegenerateDto)
                    {
                        entity.DtoStatus = RegenerableFeatureStatus.BlockedParentNotMigrated;
                        entity.DtoWarningMessage = parent.DtoHistory == null
                            ? $"The parent entity '{entity.ParentEntityName}' has no DTO in the generation history. The child DTO cannot be regenerated without the parent DTO."
                            : $"The parent entity '{entity.ParentEntityName}' DTO cannot be regenerated. The child DTO cannot be regenerated without the parent DTO.";
                        propagationChanged = true;
                    }

                    if (entity.CrudStatus == RegenerableFeatureStatus.Ready && !parent.CanRegenerateCrud)
                    {
                        entity.CrudStatus = RegenerableFeatureStatus.BlockedParentNotMigrated;
                        entity.CrudWarningMessage = parent.CrudHistory == null
                            ? $"The parent entity '{entity.ParentEntityName}' has no CRUD in the generation history. The child CRUD cannot be regenerated without the parent CRUD."
                            : $"The parent entity '{entity.ParentEntityName}' CRUD cannot be regenerated. The child CRUD cannot be regenerated without the parent CRUD.";
                        propagationChanged = true;
                    }
                }
            }
            while (propagationChanged);
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

        /// <summary>
        /// Attempts to derive an entity name from a property name by stripping a trailing "Id" suffix.
        /// e.g. "StatusId" → "Status", "RoleId" → "Role".
        /// Returns null when the derivation is not possible.
        /// </summary>
        private static string ExtractEntityNameFromProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            const string idSuffix = "Id";
            if (propertyName.EndsWith(idSuffix, StringComparison.OrdinalIgnoreCase) && propertyName.Length > idSuffix.Length)
                return propertyName[..^idSuffix.Length];

            return null;
        }
    }
}
