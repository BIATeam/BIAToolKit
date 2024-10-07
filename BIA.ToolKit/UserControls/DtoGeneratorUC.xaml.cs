namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Services;
    using System.Data.Common;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using static BIA.ToolKit.Services.UIEventBroker;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        private IConsoleWriter consoleWriter;
        private CSharpParserService parserService;
        private FileGeneratorService fileGeneratorService;
        private CRUDSettings settings;
        private Project project;
        private UIEventBroker uiEventBroker;

        public DtoGeneratorUC()
        {
            InitializeComponent();
            vm = (DtoGeneratorViewModel)DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService parserService,SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService,
            UIEventBroker uiEventBroker)
        {
            this.consoleWriter = consoleWriter;
            this.parserService = parserService;
            this.settings = new(settingsService);
            this.fileGeneratorService = fileGeneratorService;
            this.uiEventBroker = uiEventBroker;
            this.uiEventBroker.OnProjectChanged += UIEventBroker_OnProjectChanged;

            vm.Inject(consoleWriter);
        }

        private void UIEventBroker_OnProjectChanged(Project project, TabItemModifyProjectEnum currentTabItem)
        {
            if (currentTabItem != TabItemModifyProjectEnum.DtoGenerator)
                return;

            SetCurrentProject(project);
        }

        public void SetCurrentProject(Project project)
        {
            if (project == this.project)
                return;

            this.project = project;
            vm.SetProject(project);
            ListEntities();
        }

        private void ListEntities()
        {
            var domainEntities = parserService.GetDomainEntities(project, settings);
            vm.SetEntities(domainEntities);
        }

        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            ListEntities();
        }

        private void SelectProperties_Click(object sender, RoutedEventArgs e)
        {
            vm.RefreshMappingProperties();
            ResetMappingColumnsWidths();
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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await fileGeneratorService.GenerateDto(project, vm.SelectedEntityInfo, vm.EntityDomain, vm.MappingEntityProperties);
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }
    }
}
