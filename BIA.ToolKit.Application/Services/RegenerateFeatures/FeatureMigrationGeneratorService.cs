namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.ProjectAnalysis;
    using Humanizer;

    public class FeatureMigrationGeneratorService(IConsoleWriter consoleWriter)
    {
        private class MutedConsoleWriter : IConsoleWriter
        {
            public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
            {
                return;
            }
        }

        private const int REGENERATE_FEATURES_VERSION_MINIMUM = 500;
        private static readonly Regex PlaceholderPattern = new(@"\{[^}]+\}", RegexOptions.Compiled);
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        public static bool IsProjectCompatibleForRegenerateFeatures(Project project)
        {
            if (!string.IsNullOrEmpty(project?.FrameworkVersion))
            {
                string version = project.FrameworkVersion.Replace(".", "");
                if (int.TryParse(version, out int value))
                {
                    return value >= REGENERATE_FEATURES_VERSION_MINIMUM;
                }
            }

            return false;
        }

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
                await TryGenerateOptionAsync(entity.OptionHistory, targetProject, fileGenerator);
            }

            if (entity.DtoHistory != null && entity.DtoStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateDtoAsync(entity.DtoHistory, targetProject, fileGenerator);
            }

            if (entity.CrudHistory != null && entity.CrudStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateCrudAsync(entity.CrudHistory, currentProject, targetProject, fileGenerator, parserService);
            }
        }

        private async Task TryGenerateOptionAsync(
            OptionGenerationHistory history,
            Project targetProject,
            FileGeneratorService fileGenerator)
        {
            try
            {
                await fileGenerator.GenerateOptionAsync(new FileGeneratorOptionContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityNameSingular,
                    EntityNamePlural = history.EntityNamePlural,
                    DisplayName = history.DisplayItem,
                    AngularFront = history.BiaFront,
                    UseHubForClient = history.UseHubClient,
                    GenerateBack = true,
                    GenerateFront = true,
                });
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating Option for {history.EntityNameSingular}: {ex.Message}", "orange");
            }
        }

        private async Task TryGenerateDtoAsync(
            DtoGeneration history,
            Project targetProject,
            FileGeneratorService fileGenerator)
        {
            try
            {
                var properties = history.PropertyMappings.Select(pm => new MappingEntityProperty
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
                }).ToList();

                await fileGenerator.GenerateDtoAsync(new FileGeneratorDtoContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityName,
                    EntityNamePlural = history.EntityName ?? history.EntityName.Pluralize(),
                    BaseKeyType = history.EntityBaseKeyType ?? "int",
                    Properties = properties,
                    IsTeam = history.IsTeam,
                    IsVersioned = history.IsVersioned,
                    IsArchivable = history.IsArchivable,
                    IsFixable = history.IsFixable,
                    AncestorTeamName = history.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrEmpty(history.AncestorTeam),
                    HasAudit = history.UseDedicatedAudit,
                    GenerateBack = true,
                });
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating DTO for {history.EntityName}: {ex.Message}", "orange");
            }
        }

        private async Task TryGenerateCrudAsync(
            CRUDGenerationHistory history,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            try
            {
                if (history.Mapping?.Dto == null)
                    return;

                string dtoFilePath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, history.Mapping.Dto);
                ClassInfo classInfo = parserService.CurrentSolutionClasses
                    .FirstOrDefault(c => string.Equals(c.FilePath, dtoFilePath, StringComparison.OrdinalIgnoreCase));

                if (classInfo == null)
                {
                    consoleWriter.AddMessageLine($"Feature migration: DTO class not found for {history.EntityNameSingular}, skipping CRUD.", "orange");
                    return;
                }

                var entityInfo = new EntityInfo(classInfo);

                bool generateBack = history.Generation.Any(g => g.GenerationType == "WebApi");
                bool generateFront = history.Generation.Any(g => g.GenerationType == "Front")
                                     && !string.IsNullOrEmpty(history.BiaFront);

                await fileGenerator.GenerateCRUDAsync(new FileGeneratorCrudContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityNameSingular,
                    EntityNamePlural = history.EntityNamePlural,
                    BaseKeyType = history.EntityBaseKeyType,
                    IsTeam = history.IsTeam,
                    Properties = [.. entityInfo.Properties],
                    OptionItems = history.OptionItems ?? [],
                    HasParent = history.HasParent,
                    ParentName = history.ParentName,
                    ParentNamePlural = history.ParentNamePlural,
                    AncestorTeamName = history.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrEmpty(history.AncestorTeam),
                    AngularFront = generateFront ? history.BiaFront : null,
                    GenerateBack = generateBack,
                    GenerateFront = generateFront,
                    DisplayItemName = history.DisplayItem,
                    TeamTypeId = history.TeamTypeId,
                    TeamRoleId = history.TeamRoleId,
                    UseHubForClient = history.UseHubClient,
                    HasCustomRepository = history.UseCustomRepository,
                    HasReadOnlyMode = history.HasFormReadOnlyMode,
                    CanImport = history.UseImport,
                    IsFixable = history.IsFixable,
                    HasFixableParent = history.HasFixableParent,
                    HasAdvancedFilter = history.HasAdvancedFilter,
                    FormReadOnlyMode = history.FormReadOnlyMode,
                    IsVersioned = history.IsVersioned,
                    IsArchivable = history.IsArchivable,
                    DisplayHistorical = history.DisplayHistorical,
                    UseDomainUrl = history.UseDomainUrl,
                });
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
        private async Task CleanProjectForFeatureGenerationAsync(FileGeneratorService fileGenerator, Project targetProject)
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
    }
}
