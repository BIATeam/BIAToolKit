namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.ProjectAnalysis;

    /// <summary>
    /// Centralises all entity and file resolution logic that is shared between the interactive
    /// generation UCs (CRUD, Option, DTO) and the regeneration workflow.
    /// <para>
    /// Eliminates duplicate entity-lookup code that previously existed in
    /// <c>RegenerateFeaturesDiscoveryService</c>, <c>CRUDGeneratorUC.ListDtoFiles</c>, and
    /// <c>OptionGeneratorUC.ListEntityFiles</c>.
    /// </para>
    /// </summary>
    public class EntityResolutionService(CSharpParserService parserService)
    {
        private readonly CSharpParserService parserService = parserService;

        /// <summary>Gets whether the Roslyn parser has already loaded the solution classes.</summary>
        public bool IsParserReady => parserService.CurrentSolutionClasses.Count > 0;

        // ── Domain / DTO entity lookups ───────────────────────────────────────

        /// <summary>
        /// Returns all DTO entity infos for a project from the already-parsed solution classes.
        /// Mirrors the approach used in <c>CRUDGeneratorUC.ListDtoFiles</c> and
        /// <c>RegenerateFeaturesDiscoveryService.BuildDtoEntityInfos</c>.
        /// </summary>
        public IEnumerable<EntityInfo> GetDtoEntityInfos(Project project)
        {
            if (string.IsNullOrEmpty(project?.CompanyName) || string.IsNullOrEmpty(project?.Name))
                yield break;

            string dtoFolder = $"{project.CompanyName}.{project.Name}.Domain.Dto";
            string dtoFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, dtoFolder);

            foreach (ClassInfo classInfo in parserService.CurrentSolutionClasses.Where(c =>
                c.FilePath.StartsWith(dtoFolderPath, StringComparison.OrdinalIgnoreCase)
                && c.FilePath.EndsWith("Dto.cs", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new EntityInfo(classInfo);
            }
        }

        /// <summary>
        /// Returns all domain entity infos for a project.
        /// Mirrors the approach used in <c>DtoGeneratorUC.ListEntities</c> and
        /// <c>OptionGeneratorUC.ListEntityFiles</c>.
        /// </summary>
        public IEnumerable<EntityInfo> GetDomainEntityInfos(Project project)
            => parserService.GetDomainEntities(project);

        // ── History validation ────────────────────────────────────────────────

        /// <summary>
        /// Validates a CRUD generation history entry by checking whether the referenced DTO file
        /// exists and (when the parser is ready) matches a known parsed class.
        /// </summary>
        public (RegenerableFeatureStatus status, EntityInfo entityInfo) ValidateCrudHistory(
            CRUDGenerationHistory entry,
            Project project,
            IReadOnlyList<EntityInfo> dtoEntities)
        {
            try
            {
                if (entry.Mapping?.Dto == null)
                    return (RegenerableFeatureStatus.Missing, null);

                string filePath = Path.Combine(project.Folder, Constants.FolderDotNet, entry.Mapping.Dto);

                if (IsParserReady)
                {
                    EntityInfo entityInfo = dtoEntities.FirstOrDefault(e =>
                        string.Equals(e.Path, filePath, StringComparison.OrdinalIgnoreCase));
                    return entityInfo != null
                        ? (RegenerableFeatureStatus.Ready, entityInfo)
                        : (RegenerableFeatureStatus.Missing, null);
                }

                return File.Exists(filePath)
                    ? (RegenerableFeatureStatus.Ready, null)
                    : (RegenerableFeatureStatus.Missing, null);
            }
            catch
            {
                return (RegenerableFeatureStatus.Error, null);
            }
        }

        /// <summary>
        /// Validates an Option generation history entry by matching the domain entity from the
        /// parsed solution or by falling back to a file-existence check.
        /// </summary>
        public (RegenerableFeatureStatus status, EntityInfo entityInfo) ValidateOptionHistory(
            OptionGenerationHistory entry,
            Project project,
            IReadOnlyList<EntityInfo> domainEntities)
        {
            try
            {
                if (IsParserReady)
                {
                    EntityInfo entityInfo = domainEntities.FirstOrDefault(e =>
                        e.Name.Equals(entry.EntityNameSingular, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(entry.EntityNamespace)
                            || e.Namespace.Equals(entry.EntityNamespace, StringComparison.OrdinalIgnoreCase)));
                    return entityInfo != null
                        ? (RegenerableFeatureStatus.Ready, entityInfo)
                        : (RegenerableFeatureStatus.Missing, null);
                }

                string entityPath = BuildOptionEntityPath(entry, project);
                return entityPath != null
                    ? (RegenerableFeatureStatus.Ready, null)
                    : (RegenerableFeatureStatus.Missing, null);
            }
            catch
            {
                return (RegenerableFeatureStatus.Error, null);
            }
        }

        /// <summary>
        /// Validates a DTO generation history entry by matching the domain entity from the parsed
        /// solution or by falling back to a file-existence check.
        /// </summary>
        public (RegenerableFeatureStatus status, EntityInfo entityInfo) ValidateDtoHistory(
            DtoGeneration entry,
            Project project,
            IReadOnlyList<EntityInfo> domainEntities)
        {
            try
            {
                if (string.IsNullOrEmpty(entry.EntityName))
                    return (RegenerableFeatureStatus.Missing, null);

                if (IsParserReady)
                {
                    EntityInfo entityInfo = domainEntities.FirstOrDefault(e =>
                        e.Name.Equals(entry.EntityName, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(entry.EntityNamespace)
                            || e.Namespace.Equals(entry.EntityNamespace, StringComparison.OrdinalIgnoreCase)));
                    return entityInfo != null
                        ? (RegenerableFeatureStatus.Ready, entityInfo)
                        : (RegenerableFeatureStatus.Missing, null);
                }

                string entityPath = BuildDtoEntityPath(entry, project);
                if (entityPath == null)
                    return (RegenerableFeatureStatus.Missing, null);

                return File.Exists(entityPath)
                    ? (RegenerableFeatureStatus.Ready, null)
                    : (RegenerableFeatureStatus.Missing, null);
            }
            catch
            {
                return (RegenerableFeatureStatus.Error, null);
            }
        }

        // ── Namespace resolution ──────────────────────────────────────────────

        /// <summary>
        /// Searches the project files for the option entity <c>.cs</c> file and reads its C#
        /// namespace declaration. Used to back-populate
        /// <see cref="OptionGenerationHistory.EntityNamespace"/> in older history entries.
        /// Returns <see langword="null"/> when the file cannot be found or has no namespace.
        /// </summary>
        public string ResolveOptionEntityNamespace(OptionGenerationHistory entry, Project project)
        {
            string entityPath = FindOptionEntityInProjectFiles(entry, project);
            return entityPath != null ? ReadNamespaceFromCsFile(entityPath) : null;
        }

        // ── Path resolution ───────────────────────────────────────────────────

        /// <summary>
        /// Constructs the absolute path to the domain entity file for an Option generation entry.
        /// Tier 1: namespace-derived path. Tier 2: project-file search.
        /// </summary>
        private static string BuildOptionEntityPath(OptionGenerationHistory entry, Project project)
        {
            if (!string.IsNullOrEmpty(entry.EntityNamespace))
            {
                string[] parts = entry.EntityNamespace.Split('.');
                int domainIndex = Array.IndexOf(parts, "Domain");
                if (domainIndex > 0)
                {
                    string domainFolder = string.Join(".", parts[..(domainIndex + 1)]);
                    string subPath = Path.Combine(parts[(domainIndex + 1)..]);
                    string path = Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, subPath, $"{entry.EntityNameSingular}.cs");
                    if (File.Exists(path))
                        return path;
                }
            }

            return FindOptionEntityInProjectFiles(entry, project);
        }

        /// <summary>
        /// Constructs the absolute path to the domain entity file for a DTO generation entry.
        /// Prefers the stored namespace; falls back to Domain field + project metadata.
        /// </summary>
        private static string BuildDtoEntityPath(DtoGeneration entry, Project project)
        {
            if (!string.IsNullOrEmpty(entry.EntityNamespace))
            {
                string[] parts = entry.EntityNamespace.Split('.');
                int domainIndex = Array.IndexOf(parts, "Domain");
                if (domainIndex > 0)
                {
                    string domainFolder = string.Join(".", parts[..(domainIndex + 1)]);
                    string subPath = Path.Combine(parts[(domainIndex + 1)..]);
                    return Path.Combine(project.Folder, Constants.FolderDotNet, domainFolder, subPath, $"{entry.EntityName}.cs");
                }
            }

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
        /// Searches <see cref="Project.ProjectFiles"/> for a <c>.cs</c> file named
        /// <c>{entry.EntityNameSingular}.cs</c> inside the DotNet Domain project folder.
        /// </summary>
        private static string FindOptionEntityInProjectFiles(OptionGenerationHistory entry, Project project)
        {
            if (string.IsNullOrEmpty(entry.EntityNameSingular) || project.ProjectFiles == null)
                return null;

            string targetFileName = $"{entry.EntityNameSingular}.cs";
            string dotNetPrefix = Path.Combine(project.Folder, Constants.FolderDotNet) + Path.DirectorySeparatorChar;

            return project.ProjectFiles.FirstOrDefault(filePath =>
                Path.GetFileName(filePath).Equals(targetFileName, StringComparison.OrdinalIgnoreCase)
                && filePath.StartsWith(dotNetPrefix, StringComparison.OrdinalIgnoreCase)
                && IsInDomainProject(filePath, dotNetPrefix));
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="filePath"/> resides in a C# project
        /// folder whose name ends with exactly <c>.Domain</c> directly under the DotNet folder,
        /// excluding companion projects such as <c>.Domain.Dto</c>, <c>.Domain.DataAccess</c>.
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
        /// Reads the first <c>namespace</c> declaration from a C# source file.
        /// Handles both block-scoped and file-scoped namespace forms.
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
    }
}
