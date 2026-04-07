namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for GenerateUC.xaml
    /// </summary>
    public partial class GenerateUC : UserControl
    {
        public GenerateUC()
        {
            InitializeComponent();
        }

        public void Inject(ProjectViewModel projectViewModel, CSharpParserService cSharpParserService, ZipParserService zipService, GenerateCrudService crudService,
            SettingsService settingsService, IConsoleWriter consoleWriter, FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker)
        {
            // DataContext = ProjectViewModel so tab IsEnabled bindings resolve directly.
            DataContext = projectViewModel;

            ProjectSelector.Inject(projectViewModel);

            DtoGenerator.DataContext = App.GetService<DtoGeneratorViewModel>();
            CRUDGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService);
            OptionGenerator.DataContext = App.GetService<OptionGeneratorViewModel>();
        }
    }
}

