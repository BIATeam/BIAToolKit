namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Models.DtoGenerator;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.ProjectAnalysis;

    public partial class FeatureMigrationGeneratorService(IConsoleWriter consoleWriter, DtoMappingService dtoMappingService)
    {
        private class MutedConsoleWriter : IConsoleWriter
        {
            public void AddMessageLine(string message, string color = null, bool refreshimediate = true) { }
            public void Clear() { }
            public void CopyToClipboard() { }
        }

        private const int REGENERATE_FEATURES_VERSION_MINIMUM = 500;
        private static readonly Regex PlaceholderPattern = MyRegex();
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly DtoMappingService dtoMappingService = dtoMappingService;

        /// <summary>
        /// Generates features (CRUD, Option, DTO) into the target project folder (FROM or TO)
        /// using the given framework version's templates.
        /// </summary>
        public async Task GenerateFeaturesAsync(
            Project currentProject,
            string targetFolderPath,
            string targetVersion,
            List<RegenerableEntity> entities,
            CSharpParserService parserService)
        {
            consoleWriter.AddMessageLine($"Feature migration: Generating features for version {targetVersion} into {targetFolderPath}", "pink");

            var targetProject = new Project
            {
                CompanyName = currentProject.CompanyName,
                Name = currentProject.Name,
                FrameworkVersion = targetVersion[1..],
                Folder = targetFolderPath,
                BIAFronts = [.. currentProject.BIAFronts],
            };

            var fileGenerator = new FileGeneratorService(consoleWriter);
            await fileGenerator.Init(targetProject, fromUnitTest: false);

            if (!fileGenerator.IsInit)
            {
                consoleWriter.AddMessageLine($"Feature migration: Unable to initialize generator for version {targetVersion}, skipping features.", "orange");
                return;
            }

            // Clean the generated project so it only contains files that the feature generator can
            // produce (per the manifest). This keeps the FROM/TO diff focused on feature files only.
            await CleanProjectForFeatureGenerationAsync(fileGenerator, targetProject);

            // Use the real project's prettier environment (node_modules + .prettierrc) rather than
            // the temp project's, which does not have node_modules installed.
            SetPrettierOverride(fileGenerator, currentProject);

            foreach (RegenerableEntity entity in entities
                .Where(e => e.CanSelectEntity)
                .OrderBy(e => e.LastGenerationDate))
            {
                await GenerateEntityFeaturesAsync(entity, currentProject, targetProject, fileGenerator, parserService);
            }

            consoleWriter.AddMessageLine($"Feature migration: Completed generating features for version {targetVersion} into {targetFolderPath}", "green");
        }

        private async Task GenerateEntityFeaturesAsync(
            RegenerableEntity entity,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            _ = new List<Task>();
            if (entity.OptionHistory != null && entity.OptionStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateOptionAsync(entity, targetProject, fileGenerator);
            }

            if (entity.DtoHistory != null && entity.DtoStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateDtoAsync(entity, currentProject, targetProject, fileGenerator, parserService);
            }

            if (entity.CrudHistory != null && entity.CrudStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateCrudAsync(entity, currentProject, targetProject, fileGenerator, parserService);
            }
        }

        private async Task TryGenerateOptionAsync(
            RegenerableEntity entity,
            Project targetProject,
            FileGeneratorService fileGenerator)
        {
            OptionGenerationHistory history = entity.OptionHistory;
            try
            {
                await fileGenerator.GenerateOptionAsync(
                    FeatureGenerationContextFactory.CreateOptionContext(history, entity.OptionEntityInfo, targetProject));
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating Option for {history.EntityNameSingular}: {ex.Message}", "orange");
            }
        }

        private async Task TryGenerateDtoAsync(
            RegenerableEntity entity,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            DtoGeneration history = entity.DtoHistory;
            try
            {
                List<MappingEntityProperty> properties = BuildDtoMappingProperties(entity, currentProject, parserService, history);

                await fileGenerator.GenerateDtoAsync(
                    FeatureGenerationContextFactory.CreateDtoContext(history, properties, targetProject, entity.DtoEntityInfo));
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating DTO for {history.EntityName}: {ex.Message}", "orange");
            }
        }

        /// <summary>
        /// Builds the <see cref="MappingEntityProperty"/> list for DTO generation.
        /// When the domain entity has been resolved (<see cref="RegenerableEntity.DtoEntityInfo"/> is
        /// set), uses <see cref="DtoMappingService"/> to build fully-populated mapping properties
        /// (including EntityType, MappingType, OptionType, OptionRelationType, etc.) then applies
        /// the user-customized values stored in history on top. This mirrors exactly what the
        /// interactive <c>DtoGeneratorViewModel</c> produces.
        /// Falls back to a history-only mapping (limited) when the entity info is not available.
        /// </summary>
        private List<MappingEntityProperty> BuildDtoMappingProperties(
            RegenerableEntity entity,
            Project currentProject,
            CSharpParserService parserService,
            DtoGeneration history)
        {
            if (entity.DtoEntityInfo != null)
            {
                try
                {
                    // Build the entity property tree the same way DtoGeneratorViewModel does.
                    IEnumerable<EntityInfo> allDomainEntities = parserService.GetDomainEntities(currentProject);
                    List<EntityProperty> entityPropertyTree = DtoMappingService.BuildEntityPropertyTree(entity.DtoEntityInfo, allDomainEntities);

                    // Mark only the properties referenced in the generation history as selected.
                    var selectedNames = new HashSet<string>(
                        history.PropertyMappings.Select(pm => pm.EntityPropertyCompositeName),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (EntityProperty ep in GetAllEntityPropertiesRecursively(entityPropertyTree))
                        ep.IsSelected = selectedNames.Contains(ep.CompositeName);

                    // Build fully-populated MappingEntityProperty list (EntityType, MappingType,
                    // OptionType, OptionRelationType, OptionRelationFirstIdProperty, etc.).
                    List<MappingEntityProperty> properties = dtoMappingService.BuildMappingProperties(entityPropertyTree, allDomainEntities);

                    // Apply user-customized values stored in the history on top of the
                    // auto-computed defaults, so that user choices (MappingName, DateType, etc.)
                    // are preserved.
                    foreach (DtoGenerationPropertyMapping pm in history.PropertyMappings)
                    {
                        MappingEntityProperty mp = properties.FirstOrDefault(
                            x => x.EntityCompositeName.Equals(pm.EntityPropertyCompositeName, StringComparison.OrdinalIgnoreCase));
                        if (mp == null)
                            continue;

                        if (!string.IsNullOrEmpty(pm.MappingName))
                            mp.MappingName = pm.MappingName;
                        if (!string.IsNullOrEmpty(pm.DateType))
                            mp.MappingDateType = pm.DateType;
                        mp.IsRequired = pm.IsRequired;
                        mp.IsParent = pm.IsParent;
                        if (!string.IsNullOrEmpty(pm.OptionMappingDisplayProperty))
                            mp.OptionDisplayProperty = pm.OptionMappingDisplayProperty;
                        if (!string.IsNullOrEmpty(pm.OptionMappingIdProperty))
                            mp.OptionIdProperty = pm.OptionMappingIdProperty;
                        if (!string.IsNullOrEmpty(pm.OptionMappingEntityIdProperty))
                            mp.OptionEntityIdProperty = pm.OptionMappingEntityIdProperty;
                    }

                    return properties;
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine(
                        $"Feature migration: Warning - could not resolve entity properties for DTO '{history.EntityName}' from parsed solution, falling back to history-only mapping: {ex.Message}",
                        "orange");
                }
            }

            // Fallback: build from history data only. Missing fields: EntityType (real C# type),
            // OptionType, OptionRelationType, OptionRelationFirstIdProperty, OptionRelationSecondIdProperty.
            return [.. history.PropertyMappings.Select(pm => new MappingEntityProperty
            {
                EntityCompositeName = pm.EntityPropertyCompositeName,
                MappingName = pm.MappingName ?? pm.EntityPropertyCompositeName,
                IsRequired = pm.IsRequired,
                MappingDateType = pm.DateType,
                OptionDisplayProperty = pm.OptionMappingDisplayProperty,
                OptionIdProperty = pm.OptionMappingIdProperty,
                OptionEntityIdProperty = pm.OptionMappingEntityIdProperty,
                IsParent = pm.IsParent,
                EntityType = !string.IsNullOrEmpty(pm.OptionMappingEntityIdProperty)
                    ? Constants.BiaClassName.CollectionOptionDto
                    : !string.IsNullOrEmpty(pm.OptionMappingIdProperty)
                        ? Constants.BiaClassName.OptionDto
                        : string.Empty,
            })];
        }

        private static IEnumerable<EntityProperty> GetAllEntityPropertiesRecursively(IEnumerable<EntityProperty> properties)
        {
            foreach (EntityProperty p in properties)
            {
                yield return p;
                foreach (EntityProperty child in GetAllEntityPropertiesRecursively(p.Properties))
                    yield return child;
            }
        }

        private async Task TryGenerateCrudAsync(
            RegenerableEntity entity,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            CRUDGenerationHistory history = entity.CrudHistory;
            try
            {
                if (history.Mapping?.Dto == null)
                    return;

                // Prefer the EntityInfo pre-resolved during discovery (from CurrentSolutionClasses
                // filtered to the DTO folder, same approach as CRUDGeneratorUC.ListDtoFiles).
                // Fall back to a fresh lookup when discovery ran before the solution was parsed.
                EntityInfo entityInfo = entity.CrudEntityInfo;
                if (entityInfo == null)
                {
                    string dtoFilePath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, history.Mapping.Dto);
                    ClassInfo classInfo = parserService.CurrentSolutionClasses
                        .FirstOrDefault(c => string.Equals(c.FilePath, dtoFilePath, StringComparison.OrdinalIgnoreCase));

                    if (classInfo == null)
                    {
                        consoleWriter.AddMessageLine($"Feature migration: DTO class not found for {history.EntityNameSingular}, skipping CRUD.", "orange");
                        return;
                    }

                    entityInfo = new EntityInfo(classInfo);
                }

                bool generateBack = history.Generation.Any(g => g.GenerationType == "WebApi");
                bool generateFront = history.Generation.Any(g => g.GenerationType == "Front")
                                     && !string.IsNullOrEmpty(history.BiaFront);

                await fileGenerator.GenerateCRUDAsync(
                    FeatureGenerationContextFactory.CreateCrudContext(history, entityInfo, targetProject, generateBack, generateFront));
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating CRUD for {history.EntityNameSingular}: {ex.Message}", "orange");
            }
        }

        /// <summary>
        /// Sets the prettier path override on <paramref name="fileGenerator"/> to the first angular
        /// front folder of the real <paramref name="currentProject"/> that exists on disk. This
        /// ensures prettier uses the project's installed <c>node_modules</c> rather than the
        /// temporary FROM/TO project folder, which does not have <c>node_modules</c> installed.
        /// </summary>
        private void SetPrettierOverride(FileGeneratorService fileGenerator, Project currentProject)
        {
            string prettierPath = currentProject.BIAFronts
                .Select(front => Path.Combine(currentProject.Folder, front))
                .FirstOrDefault(Directory.Exists);

            if (prettierPath != null)
            {
                try
                {
                    fileGenerator.SetPrettierProjectPathOverride(prettierPath);
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Feature migration: Could not set prettier override: {ex.Message}", "orange");
                }
            }
        }

        /// <summary>
        /// Cleans <paramref name="targetProject"/> so that it retains only files whose paths match
        /// an output-path pattern declared in the current manifest. Non-matching files are deleted
        /// and empty directories are removed. This limits the FROM/TO diff to feature-relevant
        /// files only.
        /// </summary>
        private static async Task CleanProjectForFeatureGenerationAsync(FileGeneratorService fileGenerator, Project targetProject)
        {
            var manifestPaths = fileGenerator.GetAllManifestOutputPaths().ToList();
            string projectPrefix = $"{targetProject.CompanyName}.{targetProject.Name}";

            // Build full-path glob patterns (with ".*" wildcards for all entity/domain placeholders).
            var allowedPatterns = new List<string>();

            // Placeholders that can span multiple path segments (e.g. ParentChildrenRelativePath)
            // must also be treated as multi-level wildcards.  We therefore replace all remaining
            // braced tokens with ".*" (match anything, including path separators).

            foreach ((string templatePath, bool isDotNet) in manifestPaths)
            {
                // Resolve the project prefix first, then turn every remaining placeholder into a
                // regex wildcard that may span multiple path segments.
                string resolved = PlaceholderPattern.Replace(
                    templatePath.Replace("{Project}", projectPrefix),
                    "*");

                if (isDotNet)
                {
                    allowedPatterns.Add(Path.Combine(targetProject.Folder, Constants.FolderDotNet, resolved));
                }
                else
                {
                    foreach (string front in targetProject.BIAFronts)
                        allowedPatterns.Add(Path.Combine(targetProject.Folder, front, resolved));
                }
            }

            await Task.Run(() =>
            {
                foreach (string file in Directory.EnumerateFiles(targetProject.Folder, "*", SearchOption.AllDirectories).ToList())
                {
                    if (!allowedPatterns.Any(p => MatchesGlobPattern(file, p)))
                        File.Delete(file);
                }

                DeleteEmptyDirectories(targetProject.Folder);
            });
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="path"/> matches
        /// <paramref name="pattern"/>, where <c>*</c> in the pattern matches any sequence of
        /// characters (including path separators).
        /// </summary>
        private static bool MatchesGlobPattern(string path, string pattern)
        {
            // Build a regex: escape everything, then turn \* back into .* (match anything).
            string regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }

        private static void DeleteEmptyDirectories(string directory)
        {
            foreach (string subdirectory in Directory.EnumerateDirectories(directory))
            {
                DeleteEmptyDirectories(subdirectory);
                if (!Directory.EnumerateFileSystemEntries(subdirectory).Any())
                    Directory.Delete(subdirectory);
            }
        }

        [GeneratedRegex(@"\{[^}]+\}", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}
