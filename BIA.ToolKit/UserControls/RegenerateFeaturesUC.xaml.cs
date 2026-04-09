namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    /// <summary>
    /// Interaction logic for RegenerateFeaturesUC.xaml
    /// </summary>
    public partial class RegenerateFeaturesUC : UserControl
    {
        private RegenerateFeaturesViewModel viewModel;
        private IConsoleWriter consoleWriter;
        private UIEventBroker uiEventBroker;
        private RegenerateFeaturesDiscoveryService discoveryService;
        private RegenerationOrchestrationService orchestrationService;
        private Project currentProject;

        public RegenerateFeaturesUC()
        {
            InitializeComponent();
        }

        public void Inject(
            IConsoleWriter consoleWriter,
            UIEventBroker uiEventBroker,
            RegenerateFeaturesDiscoveryService discoveryService,
            RegenerationOrchestrationService orchestrationService)
        {
            this.consoleWriter = consoleWriter;
            this.uiEventBroker = uiEventBroker;
            this.discoveryService = discoveryService;
            this.orchestrationService = orchestrationService;

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
            if (!RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(currentProject)) return;

            try
            {
                List<RegenerableEntity> entities = discoveryService.DiscoverRegenerableEntities(currentProject);
                var versions = orchestrationService.GetAvailableVersions().Select(w => w.Version).ToList();
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

        private async Task Regenerate_Run(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (currentProject == null)
            {
                MessageBox.Show("Select a project before regenerating.");
                return;
            }

            // Validate all selected features have a FROM version.
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

            await orchestrationService.RegenerateAsync(currentProject, viewModel.SelectedFeatures, viewModel.EntityRows, cancellationToken);
        }
    }
}
