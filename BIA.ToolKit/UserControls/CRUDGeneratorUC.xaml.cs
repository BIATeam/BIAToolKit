namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;

    /// <summary>
    /// Interaction logic for CRUDGeneratorUC.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        private UIEventBroker uiEventBroker;
        private readonly CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, UIEventBroker uiEventBroker, FileGeneratorService fileGeneratorService)
        {
            this.uiEventBroker = uiEventBroker;
            this.uiEventBroker.OnProjectChanged += UiEventBroker_OnProjectChanged;
            this.uiEventBroker.OnSolutionClassesParsed += UiEventBroker_OnSolutionClassesParsed;

            vm.Inject(service, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService, App.GetService<IDialogService>());
        }

        private void UiEventBroker_OnSolutionClassesParsed()
        {
            vm.OnSolutionClassesParsed();
        }

        private void UiEventBroker_OnProjectChanged(Project project)
        {
            vm.SetCurrentProject(project);
        }
    }
}
