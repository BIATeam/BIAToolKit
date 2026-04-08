namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;

    /// <summary>
    /// Orchestrates the full feature-regeneration workflow that was previously spread across
    /// <c>RegenerateFeaturesUC.xaml.cs</c>.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Creating the FROM and TO skeleton projects via <see cref="ProjectCreatorService"/>.</item>
    ///   <item>Invoking <see cref="FeatureMigrationGeneratorService"/> on both projects.</item>
    ///   <item>Computing and applying the three-point git diff to the real project.</item>
    ///   <item>Updating history framework-version stamps via <see cref="GenerationHistoryService"/>.</item>
    /// </list>
    /// The UI code-behind (<c>RegenerateFeaturesUC</c>) becomes a thin coordinator that delegates
    /// all business logic here.
    /// </para>
    /// </summary>
    public class RegenerationOrchestrationService(
        IConsoleWriter consoleWriter,
        TemplateVersionService templateVersionService,
        RepositoryService repositoryService,
        ProjectCreatorService projectCreatorService,
        FeatureSettingService featureSettingService,
        GitService gitService,
        FeatureMigrationGeneratorService featureMigrationGeneratorService,
        GenerationHistoryService historyService,
        CSharpParserService cSharpParserService)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly TemplateVersionService templateVersionService = templateVersionService;
        private readonly RepositoryService repositoryService = repositoryService;
        private readonly ProjectCreatorService projectCreatorService = projectCreatorService;
        private readonly FeatureSettingService featureSettingService = featureSettingService;
        private readonly GitService gitService = gitService;
        private readonly FeatureMigrationGeneratorService featureMigrationGeneratorService = featureMigrationGeneratorService;
        private readonly GenerationHistoryService historyService = historyService;
        private readonly CSharpParserService cSharpParserService = cSharpParserService;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the list of available template versions (FROM/TO candidates).
        /// </summary>
        public List<WorkRepository> GetAvailableVersions()
            => templateVersionService.GetAvailableTemplateVersions();

        /// <summary>
        /// Normalises a version string to the canonical <c>V{x}.{y}.{z}</c> form used by template
        /// folder names (strips a leading <c>v</c>/<c>V</c> prefix if present, then adds <c>V</c>).
        /// </summary>
        public static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return version;
            return "V" + version.TrimStart('v', 'V');
        }

        /// <summary>
        /// Runs the full regeneration workflow:
        /// <list type="number">
        ///   <item>Groups the selected features by their effective FROM version.</item>
        ///   <item>For each group, creates FROM and TO skeleton projects.</item>
        ///   <item>Generates the selected features into both projects.</item>
        ///   <item>Computes a three-point diff and applies it to the real project.</item>
        ///   <item>Updates the history framework-version stamps.</item>
        /// </list>
        /// </summary>
        public async Task RegenerateAsync(
            Project currentProject,
            IEnumerable<FeatureRegenerationItem> selectedFeatures,
            IReadOnlyList<RegenerableEntityRowViewModel> entityRows)
        {
            List<WorkRepository> workTemplates = GetAvailableVersions();
            string toVersionNorm = NormalizeVersion(currentProject.FrameworkVersion);

            WorkRepository toWorkRepo = workTemplates.FirstOrDefault(w =>
                string.Equals(NormalizeVersion(w.Version), toVersionNorm, StringComparison.OrdinalIgnoreCase));

            if (toWorkRepo == null)
            {
                consoleWriter.AddMessageLine(
                    $"No template found for current project version '{toVersionNorm}'. Cannot regenerate.", "orange");
                return;
            }

            // Create a master temp folder for this regeneration session.
            string masterFolderName = $"{currentProject.Name}_RegenerateFeatures";
            string masterFolderPath = Path.Combine(AppSettings.TmpFolderPath, masterFolderName);
            if (Directory.Exists(masterFolderPath))
                await Task.Run(() => FileTransform.ForceDeleteDirectory(masterFolderPath));
            Directory.CreateDirectory(masterFolderPath);

            // Group features by FROM version so each version pair shares the same FROM/TO projects.
            var groups = selectedFeatures
                .GroupBy(f => NormalizeVersion(f.EffectiveFromVersion), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (IGrouping<string, FeatureRegenerationItem> group in groups)
            {
                string fromVersionNorm = group.Key;

                if (string.Equals(fromVersionNorm, toVersionNorm, StringComparison.OrdinalIgnoreCase))
                {
                    consoleWriter.AddMessageLine(
                        $"FROM version ({fromVersionNorm}) equals current project version; skipping group.", "gray");
                    continue;
                }

                WorkRepository fromWorkRepo = workTemplates.FirstOrDefault(w =>
                    string.Equals(NormalizeVersion(w.Version), fromVersionNorm, StringComparison.OrdinalIgnoreCase));

                if (fromWorkRepo == null)
                {
                    consoleWriter.AddMessageLine(
                        $"No template found for version '{fromVersionNorm}'. Skipping features with this FROM version.", "orange");
                    continue;
                }

                string fromFolderName = $"{currentProject.Name}_{fromVersionNorm}_From";
                string toFolderName = $"{currentProject.Name}_{toVersionNorm}_To";
                string fromPath = Path.Combine(masterFolderPath, fromFolderName);
                string toPath = Path.Combine(masterFolderPath, toFolderName);

                consoleWriter.AddMessageLine($"Creating FROM project at version {fromVersionNorm}...", "Blue");
                if (!await projectCreatorService.Create(false, fromPath, await BuildProjectParametersAsync(fromWorkRepo, currentProject)))
                {
                    consoleWriter.AddMessageLine($"Failed to create FROM project at version {fromVersionNorm}. Skipping group.", "Red");
                    continue;
                }

                ClearProjectPermissions(fromPath, currentProject);

                consoleWriter.AddMessageLine($"Creating TO project at version {toVersionNorm}...", "Blue");
                if (!await projectCreatorService.Create(false, toPath, await BuildProjectParametersAsync(toWorkRepo, currentProject)))
                {
                    consoleWriter.AddMessageLine($"Failed to create TO project at version {toVersionNorm}. Skipping group.", "Red");
                    continue;
                }

                ClearProjectPermissions(toPath, currentProject);

                // Build entity list for this version group.
                List<RegenerableEntity> entities = group
                    .Select(feature => BuildEntityForFeature(feature, entityRows))
                    .Where(e => e != null)
                    .ToList();

                // Generate features into FROM then TO projects.
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, fromPath, fromWorkRepo.Version, entities, cSharpParserService);

                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, toPath, toWorkRepo.Version, entities, cSharpParserService);

                // Apply three-point diff to the real project.
                string patchFilePath = Path.Combine(AppSettings.TmpFolderPath,
                    $"Regen_{fromFolderName}-{toFolderName}.patch");

                bool hasDiff = await gitService.DiffFolder(
                    false, masterFolderPath, fromFolderName, toFolderName, patchFilePath);

                if (hasDiff)
                {
                    await gitService.ApplyDiff(false, currentProject.Folder, patchFilePath);
                    await gitService.MergeRejected(false, new GitService.MergeParameter
                    {
                        ProjectPath = currentProject.Folder,
                        ProjectOriginPath = fromPath,
                        ProjectOriginVersion = fromWorkRepo.Version,
                        ProjectTargetPath = toPath,
                        ProjectTargetVersion = toWorkRepo.Version,
                        MigrationPatchFilePath = patchFilePath
                    });
                }

                // Stamp FrameworkVersion in history for every feature in this group.
                foreach (FeatureRegenerationItem feature in group)
                {
                    try
                    {
                        historyService.UpdateFrameworkVersion(currentProject, feature.EntityNameSingular, feature.FeatureType);
                    }
                    catch (Exception ex)
                    {
                        consoleWriter.AddMessageLine($"Warning: {ex.Message}", "orange");
                    }
                }
            }

            consoleWriter.AddMessageLine("Feature regeneration completed.", "green");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Builds <see cref="ProjectParameters"/> for use with <see cref="ProjectCreatorService.Create"/>
        /// using the current project's identity and the supplied template repository.
        /// Feature settings are loaded and merged so that generated FROM/TO projects match the
        /// project's actual feature selection. Company-file overlays are intentionally skipped.
        /// </summary>
        private async Task<ProjectParameters> BuildProjectParametersAsync(WorkRepository workRepo, Project currentProject)
        {
            if (string.IsNullOrEmpty(workRepo.VersionFolderPath))
            {
                workRepo.VersionFolderPath = await repositoryService.PrepareVersionFolder(
                    workRepo.Repository, workRepo.Version);
            }

            List<FeatureSetting> featureSettings = FeatureSettingService.LoadAndMergeFeatureSettings(
                workRepo.VersionFolderPath, currentProject.Folder);

            featureSettingService.ApplyProjectGenerationSettings(currentProject.Folder, featureSettings);

            return new ProjectParameters
            {
                CompanyName = currentProject.CompanyName,
                ProjectName = currentProject.Name,
                AngularFronts = [.. currentProject.BIAFronts],
                VersionAndOption = new VersionAndOption
                {
                    WorkTemplate = workRepo,
                    FeatureSettings = featureSettings,
                    UseCompanyFiles = false,
                },
            };
        }

        /// <summary>
        /// Clears the permissions array from <c>bianetpermissions.json</c> inside a generated
        /// project so the file does not pollute patch diffs.
        /// </summary>
        private void ClearProjectPermissions(string projectPath, Project currentProject)
        {
            string filePath = Path.Combine(
                projectPath,
                Constants.FolderNetCore,
                $"{currentProject.CompanyName}.{currentProject.Name}.Presentation.Api",
                "bianetpermissions.json");

            if (File.Exists(filePath))
                projectCreatorService.ClearPermissions(filePath);
        }

        /// <summary>
        /// Builds a <see cref="RegenerableEntity"/> containing only the single feature described by
        /// <paramref name="feature"/> (CRUD, Option, or DTO).
        /// Returns <see langword="null"/> when the corresponding entity row cannot be found or the
        /// feature is not eligible.
        /// </summary>
        private static RegenerableEntity BuildEntityForFeature(
            FeatureRegenerationItem feature,
            IReadOnlyList<RegenerableEntityRowViewModel> entityRows)
        {
            RegenerableEntityRowViewModel row = entityRows.FirstOrDefault(r =>
                string.Equals(r.EntityNameSingular, feature.EntityNameSingular, StringComparison.OrdinalIgnoreCase));

            if (row == null)
                return null;

            bool includeCrud = feature.FeatureType == "CRUD" && row.IsCrudEnabled;
            bool includeOption = feature.FeatureType == "Option" && row.IsOptionEnabled;
            bool includeDto = feature.FeatureType == "DTO" && row.Entity.CanRegenerateDto;

            if (!includeCrud && !includeOption && !includeDto)
                return null;

            return new RegenerableEntity
            {
                EntityNameSingular = row.Entity.EntityNameSingular,
                EntityNamePlural = row.Entity.EntityNamePlural,
                CrudHistory = includeCrud ? row.Entity.CrudHistory : null,
                CrudStatus = includeCrud ? row.Entity.CrudStatus : RegenerableFeatureStatus.Missing,
                OptionHistory = includeOption ? row.Entity.OptionHistory : null,
                OptionStatus = includeOption ? row.Entity.OptionStatus : RegenerableFeatureStatus.Missing,
                DtoHistory = includeDto ? row.Entity.DtoHistory : null,
                DtoStatus = includeDto ? row.Entity.DtoStatus : RegenerableFeatureStatus.Missing,
                OptionEntityInfo = row.Entity.OptionEntityInfo,
                DtoEntityInfo = row.Entity.DtoEntityInfo,
                CrudEntityInfo = row.Entity.CrudEntityInfo,
            };
        }
    }
}
