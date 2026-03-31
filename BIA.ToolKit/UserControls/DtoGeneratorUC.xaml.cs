namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Behaviors;
    using BIA.ToolKit.Domain.ModifyProject;
    using Microsoft.Xaml.Behaviors;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        private CSharpParserService parserService;
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
        public void Inject(CSharpParserService parserService, SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService,
            UIEventBroker uiEventBroker)
        {
            this.parserService = parserService;
            this.uiEventBroker = uiEventBroker;
            this.uiEventBroker.OnProjectChanged += UIEventBroker_OnProjectChanged;
            this.uiEventBroker.OnSolutionClassesParsed += UiEventBroker_OnSolutionClassesParsed;

            vm.Inject(fileGeneratorService, consoleWriter, settingsService, uiEventBroker);
            vm.OnMappingRefreshed += () => ResetMappingColumnsWidths();
        }

        private void UiEventBroker_OnSolutionClassesParsed()
        {
            ListEntities();
        }

        private void UIEventBroker_OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        public void SetCurrentProject(Project project)
        {
            if (project == this.project)
                return;

            vm.IsProjectChosen = false;
            vm.Clear();

            if (project is null)
                return;

            InitProject(project);
        }

        private void InitProject(Project project)
        {
            this.project = project;
            vm.SetProject(project);
        }

        private Task ListEntities()
        {
            if (project is null)
                return Task.CompletedTask;

            var domainEntities = parserService.GetDomainEntities(project).ToList();
            vm.SetEntities(domainEntities);
            return Task.CompletedTask;
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.ComputePropertiesValidity();
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
