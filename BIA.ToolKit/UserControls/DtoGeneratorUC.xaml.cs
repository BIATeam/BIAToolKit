namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        private IConsoleWriter consoleWriter;
        private CSharpParserService parserService;
        private CRUDSettings settings;
        private Project project;



        public DtoGeneratorUC()
        {
            InitializeComponent();
            vm = (DtoGeneratorViewModel)DataContext;
            vm.EntityChanged += Vm_EntityChanged;
        }

        private void Vm_EntityChanged(object sender, EntityInfo e)
        {
            consoleWriter.AddMessageLine($"Selected entity = {e.Name}");
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService parserService,SettingsService settingsService, IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            this.parserService = parserService;
            this.settings = new(settingsService);
        }

        public void SetCurrentProject(Project project)
        {
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
    }
}
