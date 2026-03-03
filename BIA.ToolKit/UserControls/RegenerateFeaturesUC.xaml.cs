namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
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
        private readonly RegenerateFeaturesViewModel vm;

        private RegenerateFeaturesDiscoveryService discoveryService;
        private UIEventBroker uiEventBroker;
        private IConsoleWriter consoleWriter;
        private Project currentProject;

        public RegenerateFeaturesUC()
        {
            InitializeComponent();
            vm = (RegenerateFeaturesViewModel)DataContext;
        }

        public void Inject(RegenerateFeaturesDiscoveryService discoveryService, UIEventBroker uiEventBroker, IConsoleWriter consoleWriter)
        {
            this.discoveryService = discoveryService;
            this.uiEventBroker = uiEventBroker;
            this.consoleWriter = consoleWriter;

            uiEventBroker.OnProjectChanged += UIEventBroker_OnProjectChanged;
            uiEventBroker.OnSolutionClassesParsed += UiEventBroker_OnSolutionClassesParsed;
        }

        private void UIEventBroker_OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        private void UiEventBroker_OnSolutionClassesParsed()
        {
            if (currentProject != null)
            {
                LoadEntities(currentProject);
            }
        }

        public void SetCurrentProject(Project project)
        {
            if (project == currentProject)
                return;

            currentProject = project;
            vm.DeselectAll();
            vm.Entities = [];

            if (project == null)
                return;

            LoadEntities(project);
        }

        private void LoadEntities(Project project)
        {
            _ = LoadEntitiesAsync(project);
        }

        private async Task LoadEntitiesAsync(Project project)
        {
            try
            {
                var entities = await Task.Run(() => discoveryService.DiscoverRegenerableEntities(project));
                if (project != currentProject)
                    return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    vm.Entities = new ObservableCollection<RegenerableEntity>(entities);
                });
            }
            catch (Exception ex)
            {
                consoleWriter?.AddMessageLine($"Error loading regenerable entities: {ex.Message}", "orange");
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            vm.SelectAll();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            vm.DeselectAll();
        }

        private void EntityCheckBox_Click(object sender, RoutedEventArgs e)
        {
            vm.RefreshRegenerateEnabled();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            vm.DeselectAll();
        }

        private void Regenerate_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntities = vm.SelectedEntities;
            if (!selectedEntities.Any())
            {
                consoleWriter.AddMessageLine("No entities selected for regeneration.", "orange");
                return;
            }

            consoleWriter.AddMessageLine($"Regenerate features: {selectedEntities.Count} entities selected (regeneration execution is out of scope for Phase 1).", "blue");
        }
    }
}
