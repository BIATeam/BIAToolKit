namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Behaviors;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using Microsoft.Xaml.Behaviors;
    using System;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using static BIA.ToolKit.Application.Services.UIEventBroker;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private DtoGeneratorViewModel vm;

        private FileGeneratorService fileGeneratorService;
        private UIEventBroker uiEventBroker;
        private bool processSelectProperties;

        public DtoGeneratorUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService,
            UIEventBroker uiEventBroker, DtoGeneratorViewModel dtoGeneratorViewModel)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.uiEventBroker = uiEventBroker;
            this.vm = dtoGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }

        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(vm.ListEntities);
        }

        private void SelectProperties_Click(object sender, RoutedEventArgs e)
        {
            processSelectProperties = true;
            vm.RefreshMappingProperties();
            ResetMappingColumnsWidths();
            vm.ComputePropertiesValidity();
            processSelectProperties = false;
        }

        private void RemoveAllMappingProperties_Click(object sender, RoutedEventArgs e)
        {
            vm.RemoveAllMappingProperties();
        }

        private void ResetMappingColumnsWidths()
        {
            var gridView = PropertiesListView.View as GridView;
            foreach (var column in gridView.Columns)
            {
                column.Width = 0;
                column.Width = double.NaN;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(() => vm.GenerateAsync(new FileGeneratorDtoContext
            {
                CompanyName = vm.CurrentProjectCompanyName,
                ProjectName = vm.CurrentProjectName,
                DomainName = vm.EntityDomain,
                EntityName = vm.Entity.Name,
                EntityNamePlural = vm.Entity.NamePluralized,
                BaseKeyType = vm.SelectedBaseKeyType,
                Properties = [.. vm.MappingEntityProperties],
                IsTeam = vm.IsTeam,
                IsVersioned = vm.IsVersioned,
                IsArchivable = vm.IsArchivable,
                IsFixable = vm.IsFixable,
                AncestorTeamName = vm.AncestorTeam,
                HasAncestorTeam = !string.IsNullOrEmpty(vm.AncestorTeam),
                GenerateBack = true,
                HasAudit = vm.UseDedicatedAudit
            }));
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (processSelectProperties)
                return;

            vm.ComputePropertiesValidity();
        }

        private void EntitiesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (vm.Entity is null)
                return;

            vm.LoadFromHistory(vm.Entity);
        }

        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var behavior = GetDragDropBehavior();
            behavior?.HandleDragStart(sender, e);
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            var behavior = GetDragDropBehavior();
            behavior?.HandleDragMove(sender, e);
        }

        private ListViewDragDropBehavior GetDragDropBehavior()
        {
            return Interaction.GetBehaviors(PropertiesListView)
                              .OfType<ListViewDragDropBehavior>()
                              .FirstOrDefault();
        }
    }
}
