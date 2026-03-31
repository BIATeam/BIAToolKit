namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        private ModifyProjectViewModel _viewModel;

        public ModifyProjectUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called from MainWindow to wire DI-resolved VM and child controls that still use the Inject pattern.
        /// </summary>
        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker)
        {
            // Resolve the DI-built VM and set as DataContext
            _viewModel = App.GetService<ModifyProjectViewModel>();
            DataContext = _viewModel;

            // Create and assign dedicated ViewModel instances for each VersionAndOption control
            _viewModel.OriginVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            _viewModel.TargetVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            MigrateOriginVersionAndOption.DataContext = _viewModel.OriginVersionAndOptionVM;
            MigrateTargetVersionAndOption.DataContext = _viewModel.TargetVersionAndOptionVM;

            // Child controls that still use the Inject pattern
            CRUDGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService);
            OptionGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService);
            DtoGenerator.Inject(cSharpParserService, settingsService, consoleWriter, fileGeneratorService, uiEventBroker);
        }
    }
}
