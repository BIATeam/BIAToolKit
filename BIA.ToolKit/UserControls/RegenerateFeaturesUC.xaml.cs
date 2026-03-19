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
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
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
            if (!GenerateCrudService.IsProjectCompatibleForRegenerateFeatures(currentProject)) return;

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

            // Group selected features by their effective FROM version
            var fromVersionGroups = viewModel.SelectedFeatures
                .GroupBy(f => NormalizeVersion(f.EffectiveFromVersion), StringComparer.OrdinalIgnoreCase)
                .ToList();

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

            foreach (var group in fromVersionGroups)
            {
                string fromVersionNorm = group.Key;

                if (string.Equals(fromVersionNorm, toVersionNorm, StringComparison.OrdinalIgnoreCase))
                {
                    consoleWriter.AddMessageLine(
                        $"FROM version ({fromVersionNorm}) equals current project version; skipping.", "gray");
                    continue;
                }

                var fromWorkRepo = workTemplates.FirstOrDefault(w =>
                    string.Equals(NormalizeVersion(w.Version), fromVersionNorm, StringComparison.OrdinalIgnoreCase));

                if (fromWorkRepo == null)
                {
                    consoleWriter.AddMessageLine(
                        $"No template found for version '{fromVersionNorm}'. Skipping group.", "orange");
                    continue;
                }

                // Build entity list for this FROM-version group (only features matching this FROM version)
                var entitiesForGroup = BuildEntitiesForGroup(group.ToList());
                if (entitiesForGroup.Count == 0) continue;

                string safeFrom = fromVersionNorm.Replace(".", "_");
                string safeTo = toVersionNorm.Replace(".", "_");
                string fromFolderName = $"{currentProject.Name}_{safeFrom}_From";
                string toFolderName = $"{currentProject.Name}_{safeTo}_To";
                string fromPath = Path.Combine(AppSettings.TmpFolderPath, fromFolderName);
                string toPath = Path.Combine(AppSettings.TmpFolderPath, toFolderName);

                // Create isolated empty directories for feature generation
                if (Directory.Exists(fromPath))
                    await Task.Run(() => FileTransform.ForceDeleteDirectory(fromPath));
                Directory.CreateDirectory(fromPath);

                if (Directory.Exists(toPath))
                    await Task.Run(() => FileTransform.ForceDeleteDirectory(toPath));
                Directory.CreateDirectory(toPath);

                // Generate only the selected features into the isolated FROM and TO folders
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, fromPath, fromWorkRepo.Version, entitiesForGroup, cSharpParserService);
                await featureMigrationGeneratorService.GenerateFeaturesAsync(
                    currentProject, toPath, toWorkRepo.Version, entitiesForGroup, cSharpParserService);

                // Apply 3-point diff to the current project
                string patchFilePath = Path.Combine(AppSettings.TmpFolderPath,
                    $"Regen_{fromFolderName}-{toFolderName}.patch");

                bool diffOk = await gitService.DiffFolder(
                    false, AppSettings.TmpFolderPath, fromFolderName, toFolderName, patchFilePath);
                if (!diffOk) continue;

                await gitService.ApplyDiff(false, currentProject.Folder, patchFilePath);
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
        /// Build a list of <see cref="RegenerableEntity"/> from the selected FeatureRegenerationItems
        /// (all items in this group share the same FROM version).
        /// </summary>
        private List<RegenerableEntity> BuildEntitiesForGroup(List<FeatureRegenerationItem> groupItems)
        {
            var result = new List<RegenerableEntity>();

            // Lookup entity rows by name
            var rowsByEntity = viewModel.EntityRows
                .ToDictionary(r => r.EntityNameSingular, StringComparer.OrdinalIgnoreCase);

            // Group items by entity
            foreach (var entityGroup in groupItems.GroupBy(f => f.EntityNameSingular, StringComparer.OrdinalIgnoreCase))
            {
                if (!rowsByEntity.TryGetValue(entityGroup.Key, out var row))
                    continue;

                bool includeCrud = entityGroup.Any(f => f.FeatureType == "CRUD") && row.IsCrudEnabled;
                bool includeOption = entityGroup.Any(f => f.FeatureType == "Option") && row.IsOptionEnabled;
                bool includeDto = entityGroup.Any(f => f.FeatureType == "DTO") && row.IsDtoEnabled;

                if (!includeCrud && !includeOption && !includeDto) continue;

                var entityCopy = new RegenerableEntity
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
                result.Add(entityCopy);
            }

            return result;
        }
    }
}
