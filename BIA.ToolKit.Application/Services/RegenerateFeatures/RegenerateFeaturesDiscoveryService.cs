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
                        // Parent found in history — dynamic blocking will be handled in the ViewModel
                        entity.HasParentDependency = true;
                    }
                    else
                    {
                        // Parent absent from history — static blocking
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
                // BuildOptionEntityPath returns null when no existing file was found
                string entityPath = BuildOptionEntityPath(entry, project);
                return entityPath != null ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
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

                string entityPath = BuildDtoEntityPath(entry, project);
                if (entityPath == null)
                    return RegenerableFeatureStatus.Missing;

                return File.Exists(entityPath) ? RegenerableFeatureStatus.Ready : RegenerableFeatureStatus.Missing;
            }
            catch
            {
                return RegenerableFeatureStatus.Error;
            }
        }

        /// <summary>
        /// Constructs the absolute path to the domain entity file for an Option generation entry.
        /// <para>
        /// Tier 1: derives the path from the stored <see cref="OptionGenerationHistory.EntityNamespace"/>
        /// (e.g. "Acme.MyApp.Domain.Planes.Entities" → "DotNet/Acme.MyApp.Domain/Planes/Entities/{Name}.cs")
        /// because it does not depend on <see cref="Project.CompanyName"/> being resolved.
        /// Tier 2 (fallback for older histories without EntityNamespace): searches
        /// <see cref="Project.ProjectFiles"/> for a <c>{EntityNameSingular}.cs</c> file located under
        /// the DotNet Domain project folder (the first-level subdirectory of DotNet whose name ends with
        /// ".Domain"), thus working without any project-level metadata.
        /// </para>
        /// Returns <see langword="null"/> when no existing file can be located.
        /// </summary>
        private static string BuildOptionEntityPath(OptionGenerationHistory entry, Project project)
        {
            // Tier 1 — use EntityNamespace when present (populated by all generations after the fix)
            if (!string.IsNullOrEmpty(entry.EntityNamespace))
            {
                string[] parts = entry.EntityNamespace.Split('.');
                int domainIndex = Array.IndexOf(parts, "Domain");
                if (domainIndex > 0)
                {
                    string domainFolder = string.Join(".", parts[..(domainIndex + 1)]);
                    string[] subFolders = parts[(domainIndex + 1)..];
                    string subPath = Path.Combine(subFolders);
                    string path = Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, subPath, $"{entry.EntityNameSingular}.cs");
                    if (File.Exists(path))
                        return path;
                    // EntityNamespace stored but file not found — fall through to project-files search
                }
            }

            // Tier 2 — search project files for the entity .cs file in the Domain project folder
            return FindOptionEntityInProjectFiles(entry, project);
        }

        /// <summary>
        /// Searches <see cref="Project.ProjectFiles"/> for a .cs file named
        /// <c>{entry.EntityNameSingular}.cs</c> that lives inside the DotNet Domain project folder
        /// (the first-level subdirectory of DotNet whose name ends with exactly ".Domain",
        /// e.g. "Acme.MyApp.Domain", excluding ".Domain.Dto", ".Domain.DataAccess", etc.).
        /// Returns the full absolute path when found, or <see langword="null"/> otherwise.
        /// </summary>
        private static string FindOptionEntityInProjectFiles(OptionGenerationHistory entry, Project project)
        {
            if (string.IsNullOrEmpty(entry.EntityNameSingular) || project.ProjectFiles == null)
                return null;

            string targetFileName = $"{entry.EntityNameSingular}.cs";
            string dotNetFolder = Path.Combine(project.Folder, Constants.FolderDotNet);
            string dotNetPrefix = dotNetFolder + Path.DirectorySeparatorChar;

            return project.ProjectFiles.FirstOrDefault(filePath =>
                Path.GetFileName(filePath).Equals(targetFileName, StringComparison.OrdinalIgnoreCase)
                && filePath.StartsWith(dotNetPrefix, StringComparison.OrdinalIgnoreCase)
                && IsInDomainProject(filePath, dotNetPrefix));
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="filePath"/> resides in a C# project
        /// folder whose name ends with exactly ".Domain" (e.g. "Acme.MyApp.Domain") directly under
        /// the DotNet folder, excluding companion projects such as ".Domain.Dto", ".Domain.DataAccess".
        /// </summary>
        private static bool IsInDomainProject(string filePath, string dotNetPrefix)
        {
            string relativeToDotNet = filePath[dotNetPrefix.Length..];
            int separatorIndex = relativeToDotNet.IndexOf(Path.DirectorySeparatorChar);
            if (separatorIndex < 0) return false;
            string projectFolderName = relativeToDotNet[..separatorIndex];
            return projectFolderName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Searches the project files for the option entity .cs file (using the same strategy as
        /// <see cref="BuildOptionEntityPath"/>) and reads its C# namespace declaration.
        /// Returns <see langword="null"/> when the file cannot be found or has no namespace.
        /// Used to back-populate <see cref="OptionGenerationHistory.EntityNamespace"/> in older
        /// history entries after a successful feature migration.
        /// </summary>
        public string ResolveOptionEntityNamespace(OptionGenerationHistory entry, Project project)
        {
            string entityPath = FindOptionEntityInProjectFiles(entry, project);
            return entityPath != null ? ReadNamespaceFromCsFile(entityPath) : null;
        }

        /// <summary>
        /// Reads the first <c>namespace</c> declaration from a C# source file and returns it.
        /// Handles both block-scoped (<c>namespace Foo { }</c>) and file-scoped (<c>namespace Foo;</c>) forms.
        /// Returns <see langword="null"/> when no namespace line is found.
        /// </summary>
        private static string ReadNamespaceFromCsFile(string filePath)
        {
            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                        return trimmed["namespace ".Length..].TrimEnd('{', ';', ' ');
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Constructs the absolute path to the domain entity file for a DTO generation entry.
        /// Prefers deriving the path from the stored <see cref="DtoGeneration.EntityNamespace"/>
        /// (e.g. "Acme.MyApp.Domain.Orders.Entities" → "DotNet/Acme.MyApp.Domain/Orders/Entities/{Name}.cs")
        /// because it does not depend on <see cref="Project.CompanyName"/> being resolved.
        /// Falls back to the <see cref="DtoGeneration.Domain"/> field and project metadata when available.
        /// </summary>
        private static string BuildDtoEntityPath(DtoGeneration entry, Project project)
        {
            if (!string.IsNullOrEmpty(entry.EntityNamespace))
            {
                // Namespace: "Acme.MyApp.Domain.Orders.Entities"
                // → C# project folder: "Acme.MyApp.Domain"  (join all parts up to and including "Domain")
                // → sub-path:         "Orders/Entities"     (parts after "Domain")
                string[] parts = entry.EntityNamespace.Split('.');
                int domainIndex = Array.IndexOf(parts, "Domain");
                if (domainIndex > 0)
                {
                    string domainFolder = string.Join(".", parts[..(domainIndex + 1)]);
                    string[] subFolders = parts[(domainIndex + 1)..];
                    string subPath = Path.Combine(subFolders);
                    return Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, subPath, $"{entry.EntityName}.cs");
                }
            }

            // Fallback: build path using Domain field and project-level company/name info
            if (!string.IsNullOrEmpty(entry.Domain)
                && !string.IsNullOrEmpty(project.CompanyName)
                && !string.IsNullOrEmpty(project.Name))
            {
                string domainFolder = $"{project.CompanyName}.{project.Name}.Domain";
                return Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, entry.Domain, "Entities", $"{entry.EntityName}.cs");
            }

            return null;
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
