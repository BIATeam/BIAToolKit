namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;

    /// <summary>
    /// Interaction logic for RegenerateFeaturesUC.xaml
    /// </summary>
    public partial class RegenerateFeaturesUC : UserControl
    {
        private RegenerateFeaturesViewModel viewModel;
        private IConsoleWriter consoleWriter;
        private UIEventBroker uiEventBroker;
        private SettingsService settingsService;
        private RegenerateFeaturesDiscoveryService discoveryService;
        private FeatureMigrationGeneratorService featureMigrationGeneratorService;
        private GitService gitService;
        private CSharpParserService cSharpParserService;
        private ProjectCreatorService projectCreatorService;
        private TemplateVersionService templateVersionService;
        private FeatureSettingService featureSettingService;
        private RepositoryService repositoryService;
        private Project currentProject;

        public RegenerateFeaturesUC()
        {
            InitializeComponent();
        }

        public void Inject(
            IConsoleWriter consoleWriter,
            UIEventBroker uiEventBroker,
            SettingsService settingsService,
            RegenerateFeaturesDiscoveryService discoveryService,
            FeatureMigrationGeneratorService featureMigrationGeneratorService,
            GitService gitService,
            CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService,
            TemplateVersionService templateVersionService,
            FeatureSettingService featureSettingService,
            RepositoryService repositoryService)
        {
            this.consoleWriter = consoleWriter;
            this.uiEventBroker = uiEventBroker;
            this.settingsService = settingsService;
            this.discoveryService = discoveryService;
            this.featureMigrationGeneratorService = featureMigrationGeneratorService;
            this.gitService = gitService;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            this.templateVersionService = templateVersionService;
            this.featureSettingService = featureSettingService;
            this.repositoryService = repositoryService;

            viewModel = new RegenerateFeaturesViewModel();
            DataContext = viewModel;

            uiEventBroker.OnProjectChanged += OnProjectChanged;
            uiEventBroker.OnSolutionClassesParsed += OnSolutionClassesParsed;
        }

        private void OnProjectChanged(Project project)
        {
            currentProject = project;
        }

        private void OnSolutionClassesParsed()
        {
            LoadFeatures();
        }

        private void LoadFeatures()
        {
            if (currentProject == null) return;
            if (!FeatureMigrationGeneratorService.IsProjectCompatibleForRegenerateFeatures(currentProject)) return;

            try
            {
                List<RegenerableEntity> entities = discoveryService.DiscoverRegenerableEntities(currentProject);
                var versions = GetAvailableVersions().Select(w => w.Version).ToList();
                viewModel.Initialize(currentProject, entities, versions);
            }
            catch (Exception ex)
            {
                consoleWriter?.AddMessageLine($"Error loading regenerable features: {ex.Message}", "orange");
            }
        }

        private void Regenerate_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(Regenerate_Run);
        }

        private async Task Regenerate_Run()
        {
            if (currentProject == null)
            {
                MessageBox.Show("Select a project before regenerating.");
                return;
            }

            // Validate all selected features have a FROM version
            var missingVersion = viewModel.SelectedFeatures
                .Where(f => string.IsNullOrEmpty(f.EffectiveFromVersion))
                .ToList();

            if (missingVersion.Count > 0)
            {
                string list = string.Join(", ", missingVersion.Select(f => $"{f.EntityNameSingular}/{f.FeatureType}"));
                MessageBox.Show($"Please select a 'from version' for the following features before regenerating:\n{list}");
                return;
            }

            if (viewModel.SelectedFeatures.Count == 0)
            {
                MessageBox.Show("No features selected for regeneration.");
                return;
            }

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

            // Create a master temp folder for this regeneration session
            string masterFolderName = $"{currentProject.Name}_RegenerateFeatures";
            string masterFolderPath = Path.Combine(AppSettings.TmpFolderPath, masterFolderName);
            if (Directory.Exists(masterFolderPath))
                await Task.Run(() => FileTransform.ForceDeleteDirectory(masterFolderPath));
            Directory.CreateDirectory(masterFolderPath);

            // Group features by FROM version so each version pair shares the same FROM/TO projects
            var groups = viewModel.SelectedFeatures
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

                // Create FROM project (full project at FROM version)
                consoleWriter.AddMessageLine($"Creating FROM project at version {fromVersionNorm}...", "Blue");
                if (!await projectCreatorService.Create(false, fromPath, await BuildProjectParametersAsync(fromWorkRepo)))
                {
                    consoleWriter.AddMessageLine($"Failed to create FROM project at version {fromVersionNorm}. Skipping group.", "Red");
                    continue;
                }

                ClearProjectPermissions(fromPath);

                // Create TO project (full project at current version)
                consoleWriter.AddMessageLine($"Creating TO project at version {toVersionNorm}...", "Blue");
                if (!await projectCreatorService.Create(false, toPath, await BuildProjectParametersAsync(toWorkRepo)))
                {
                    consoleWriter.AddMessageLine($"Failed to create TO project at version {toVersionNorm}. Skipping group.", "Red");
                    continue;
                }

                ClearProjectPermissions(toPath);

                // Build entity list for this version group
                List<RegenerableEntity> entities = group
                    .Select(feature => BuildEntityForFeature(feature))
                    .Where(e => e != null)
                    .ToList();

                // Generate features into FROM project
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, fromPath, fromWorkRepo.Version, entities, cSharpParserService);

                // Generate features into TO project
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, toPath, toWorkRepo.Version, entities, cSharpParserService);

                // Apply 3-point diff to the current project
                string patchFilePath = Path.Combine(AppSettings.TmpFolderPath,
                    $"Regen_{fromFolderName}-{toFolderName}.patch");

                bool hasDiff = await gitService.DiffFolder(
                    false, masterFolderPath, fromFolderName, toFolderName, patchFilePath);
                if (hasDiff)
                {
                    await gitService.ApplyDiff(false, currentProject.Folder, patchFilePath);
                    await gitService.MergeRejected(false, new GitService.MergeParameter()
                    {
                        ProjectPath = currentProject.Folder,
                        ProjectOriginPath = fromPath,
                        ProjectOriginVersion = fromWorkRepo.Version,
                        ProjectTargetPath = toPath,
                        ProjectTargetVersion = toWorkRepo.Version,
                        MigrationPatchFilePath = patchFilePath
                    }); ;
                }

                // Update framework version in history for each feature in this group
                foreach (FeatureRegenerationItem feature in group)
                {
                    UpdateHistoryFrameworkVersion(feature.EntityNameSingular, feature.FeatureType);
                }
            }

            consoleWriter.AddMessageLine("Feature regeneration completed.", "green");
        }

        private List<WorkRepository> GetAvailableVersions()
        {
            return templateVersionService.GetAvailableTemplateVersions();
        }

        /// <summary>
        /// Builds <see cref="ProjectParameters"/> for use with <see cref="ProjectCreatorService.Create"/>
        /// using the current project's identity and the supplied template repository.
        /// Feature settings are loaded and merged from the template and the current project
        /// so that the generated FROM/TO projects match the project's actual feature selection.
        /// Company-file overlays are intentionally skipped to keep regeneration simple.
        /// </summary>
        private async Task<ProjectParameters> BuildProjectParametersAsync(WorkRepository workRepo)
        {
            // Ensure VersionFolderPath is resolved (needed to load feature settings from template)
            if (string.IsNullOrEmpty(workRepo.VersionFolderPath))
            {
                workRepo.VersionFolderPath = await repositoryService.PrepareVersionFolder(
                    workRepo.Repository, workRepo.Version);
            }

            // Load and merge feature settings from template + project
            List<FeatureSetting> featureSettings = FeatureSettingService.LoadAndMergeFeatureSettings(
                workRepo.VersionFolderPath, currentProject.Folder);

            // Apply ProjectGeneration overrides if available
            featureSettingService.ApplyProjectGenerationSettings(
                currentProject.Folder, featureSettings);

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
        private void ClearProjectPermissions(string projectPath)
        {
            string filePath = Path.Combine(
                projectPath,
                Constants.FolderNetCore,
                $"{currentProject.CompanyName}.{currentProject.Name}.Presentation.Api",
                "bianetpermissions.json");

            if (File.Exists(filePath))
                projectCreatorService.ClearPermissions(filePath);
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return version;
            string stripped = version.TrimStart('v', 'V');
            return "V" + stripped;
        }

        /// <summary>
        /// Build a <see cref="RegenerableEntity"/> containing only the single feature described by
        /// <paramref name="feature"/> (CRUD, Option, or DTO).
        /// </summary>
        private RegenerableEntity BuildEntityForFeature(FeatureRegenerationItem feature)
        {
            var rowsByEntity = viewModel.EntityRows
                .ToDictionary(r => r.EntityNameSingular, StringComparer.OrdinalIgnoreCase);

            if (!rowsByEntity.TryGetValue(feature.EntityNameSingular, out RegenerableEntityRowViewModel row))
                return null;

            bool includeCrud = feature.FeatureType == "CRUD" && row.IsCrudEnabled;
            bool includeOption = feature.FeatureType == "Option" && row.IsOptionEnabled;
            bool includeDto = feature.FeatureType == "DTO" && row.Entity.CanRegenerateDto;

            if (!includeCrud && !includeOption && !includeDto) return null;

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
                CrudEntityInfo = row.Entity.CrudEntityInfo
            };
        }

        /// <summary>
        /// Updates the <c>FrameworkVersion</c> field of the history entry for 
        /// <paramref name="entityName"/>/<paramref name="featureType"/> to the current project version
        /// and persists the history file. Creates the property if it was not previously set.
        /// </summary>
        private void UpdateHistoryFrameworkVersion(string entityName, string featureType)
        {
            var crudSettings = new CRUDSettings(settingsService);
            try
            {
                switch (featureType)
                {
                    case "CRUD":
                        {
                            string historyFile = Path.Combine(currentProject.Folder, Constants.FolderBia, crudSettings.CrudGenerationHistoryFileName);
                            CRUDGeneration history = CommonTools.DeserializeJsonFile<CRUDGeneration>(historyFile);
                            if (history == null) break;
                            CRUDGenerationHistory entry = history.CRUDGenerationHistory
                                .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entityName, StringComparison.OrdinalIgnoreCase));
                            if (entry != null)
                            {
                                entry.FrameworkVersion = currentProject.FrameworkVersion;
                                CommonTools.SerializeToJsonFile(history, historyFile);
                            }
                            break;
                        }
                    case "Option":
                        {
                            string historyFile = Path.Combine(currentProject.Folder, Constants.FolderBia, crudSettings.OptionGenerationHistoryFileName);
                            OptionGeneration history = CommonTools.DeserializeJsonFile<OptionGeneration>(historyFile);
                            if (history == null) break;
                            OptionGenerationHistory entry = history.OptionGenerationHistory
                                .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entityName, StringComparison.OrdinalIgnoreCase));
                            if (entry != null)
                            {
                                entry.FrameworkVersion = currentProject.FrameworkVersion;

                                // Back-populate EntityNamespace for older history entries that predate the fix.
                                // Once set, future discovery will use the fast namespace-based path resolution.
                                if (string.IsNullOrEmpty(entry.EntityNamespace))
                                {
                                    string ns = discoveryService.ResolveOptionEntityNamespace(entry, currentProject);
                                    if (!string.IsNullOrEmpty(ns))
                                        entry.EntityNamespace = ns;
                                }

                                CommonTools.SerializeToJsonFile(history, historyFile);
                            }
                            break;
                        }
                    case "DTO":
                        {
                            string historyFile = Path.Combine(currentProject.Folder, Constants.FolderBia, crudSettings.DtoGenerationHistoryFileName);
                            DtoGenerationHistory history = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(historyFile);
                            if (history == null) break;
                            DtoGeneration entry = history.Generations
                                .FirstOrDefault(h => string.Equals(h.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
                            if (entry != null)
                            {
                                entry.FrameworkVersion = currentProject.FrameworkVersion;
                                CommonTools.SerializeToJsonFile(history, historyFile);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(
                    $"Warning: Could not update FrameworkVersion in history for {entityName}/{featureType}: {ex.Message}", "orange");
            }
        }
    }
}
