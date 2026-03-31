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
            CSharpParserService cSharpParserService)
        {
            this.consoleWriter = consoleWriter;
            this.uiEventBroker = uiEventBroker;
            this.settingsService = settingsService;
            this.discoveryService = discoveryService;
            this.featureMigrationGeneratorService = featureMigrationGeneratorService;
            this.gitService = gitService;
            this.cSharpParserService = cSharpParserService;

            viewModel = new RegenerateFeaturesViewModel();
            this.DataContext = viewModel;

            uiEventBroker.OnProjectChanged += OnProjectChanged;
            uiEventBroker.OnSolutionClassesParsed += OnSolutionClassesParsed;
        }

        private void OnProjectChanged(Project project)
        {
            currentProject = project;
            LoadFeatures();
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
                var entities = discoveryService.DiscoverRegenerableEntities(currentProject);
                var versions = GetAvailableVersions().Select(w => w.Version).ToList();
                viewModel.Initialize(currentProject, entities, versions);
            }
            catch (Exception ex)
            {
                consoleWriter?.AddMessageLine($"Error loading regenerable features: {ex.Message}", "orange");
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadFeatures();
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
                var list = string.Join(", ", missingVersion.Select(f => $"{f.EntityNameSingular}/{f.FeatureType}"));
                MessageBox.Show($"Please select a 'from version' for the following features before regenerating:\n{list}");
                return;
            }

            if (viewModel.SelectedFeatures.Count == 0)
            {
                MessageBox.Show("No features selected for regeneration.");
                return;
            }

            var workTemplates = GetAvailableVersions();
            string toVersionNorm = NormalizeVersion(currentProject.FrameworkVersion);

            var toWorkRepo = workTemplates.FirstOrDefault(w =>
                string.Equals(NormalizeVersion(w.Version), toVersionNorm, StringComparison.OrdinalIgnoreCase));

            if (toWorkRepo == null)
            {
                consoleWriter.AddMessageLine(
                    $"No template found for current project version '{toVersionNorm}'. Cannot regenerate.", "orange");
                return;
            }

            // Process each feature individually so each gets its own isolated temp folders
            foreach (var feature in viewModel.SelectedFeatures)
            {
                string fromVersionNorm = NormalizeVersion(feature.EffectiveFromVersion);

                if (string.Equals(fromVersionNorm, toVersionNorm, StringComparison.OrdinalIgnoreCase))
                {
                    consoleWriter.AddMessageLine(
                        $"FROM version ({fromVersionNorm}) equals current project version for {feature.EntityNameSingular}/{feature.FeatureType}; skipping.", "gray");
                    continue;
                }

                var fromWorkRepo = workTemplates.FirstOrDefault(w =>
                    string.Equals(NormalizeVersion(w.Version), fromVersionNorm, StringComparison.OrdinalIgnoreCase));

                if (fromWorkRepo == null)
                {
                    consoleWriter.AddMessageLine(
                        $"No template found for version '{fromVersionNorm}'. Skipping {feature.EntityNameSingular}/{feature.FeatureType}.", "orange");
                    continue;
                }

                var entity = BuildEntityForFeature(feature);
                if (entity == null) continue;

                // Folder names include entity name and feature type to distinguish feature-level
                // regeneration from full-project regeneration
                string safeEntity = feature.EntityNameSingular.Replace(" ", "_");
                string safeFeatureType = feature.FeatureType;
                string safeFrom = fromVersionNorm.Replace(".", "_");
                string safeTo = toVersionNorm.Replace(".", "_");
                string fromFolderName = $"{currentProject.Name}_{safeEntity}_{safeFeatureType}_{safeFrom}_From";
                string toFolderName = $"{currentProject.Name}_{safeEntity}_{safeFeatureType}_{safeTo}_To";
                string fromPath = Path.Combine(AppSettings.TmpFolderPath, fromFolderName);
                string toPath = Path.Combine(AppSettings.TmpFolderPath, toFolderName);

                // Create isolated empty directories for feature generation
                if (Directory.Exists(fromPath))
                    await Task.Run(() => FileTransform.ForceDeleteDirectory(fromPath));
                Directory.CreateDirectory(fromPath);

                if (Directory.Exists(toPath))
                    await Task.Run(() => FileTransform.ForceDeleteDirectory(toPath));
                Directory.CreateDirectory(toPath);

                // Generate only this feature into the isolated FROM and TO folders
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, fromPath, fromWorkRepo.Version, [entity], cSharpParserService);
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, toPath, toWorkRepo.Version, [entity], cSharpParserService);

                // Apply 3-point diff to the current project
                string patchFilePath = Path.Combine(AppSettings.TmpFolderPath,
                    $"Regen_{fromFolderName}-{toFolderName}.patch");

                bool hasDiff = await gitService.DiffFolder(
                    false, AppSettings.TmpFolderPath, fromFolderName, toFolderName, patchFilePath);
                if (hasDiff)
                {
                    await gitService.ApplyDiff(false, currentProject.Folder, patchFilePath);
                }

                UpdateHistoryFrameworkVersion(feature.EntityNameSingular, feature.FeatureType);
            }

            consoleWriter.AddMessageLine("Feature regeneration completed.", "green");
        }

        private List<WorkRepository> GetAvailableVersions()
        {
            var result = new List<WorkRepository>();
            if (settingsService?.Settings?.TemplateRepositories == null)
                return result;

            foreach (var repo in settingsService.Settings.TemplateRepositories.Where(r => r.UseRepository))
            {
                foreach (var release in repo.Releases)
                    result.Add(new WorkRepository(repo, release.Name));
            }

            result.Sort(new WorkRepository.VersionComparer());
            return result;
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return version;
            var stripped = version.TrimStart('v', 'V');
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

            if (!rowsByEntity.TryGetValue(feature.EntityNameSingular, out var row))
                return null;

            bool includeCrud = feature.FeatureType == "CRUD" && row.IsCrudEnabled;
            bool includeOption = feature.FeatureType == "Option" && row.IsOptionEnabled;
            bool includeDto = feature.FeatureType == "DTO" && row.IsDtoEnabled;

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
                        var history = CommonTools.DeserializeJsonFile<CRUDGeneration>(historyFile);
                        if (history == null) break;
                        var entry = history.CRUDGenerationHistory
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
                        var history = CommonTools.DeserializeJsonFile<OptionGeneration>(historyFile);
                        if (history == null) break;
                        var entry = history.OptionGenerationHistory
                            .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entityName, StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                        {
                            entry.FrameworkVersion = currentProject.FrameworkVersion;
                            CommonTools.SerializeToJsonFile(history, historyFile);
                        }
                        break;
                    }
                    case "DTO":
                    {
                        string historyFile = Path.Combine(currentProject.Folder, Constants.FolderBia, crudSettings.DtoGenerationHistoryFileName);
                        var history = CommonTools.DeserializeJsonFile<DtoGenerationHistory>(historyFile);
                        if (history == null) break;
                        var entry = history.Generations
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
