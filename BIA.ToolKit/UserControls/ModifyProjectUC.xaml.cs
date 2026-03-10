namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Helper;

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

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            FileGeneratorService fileGeneratorService, ModifyProjectViewModel modifyProjectViewModel,
            CRUDGeneratorViewModel crudGeneratorViewModel, DtoGeneratorViewModel dtoGeneratorViewModel, OptionGeneratorViewModel optionGeneratorViewModel,
            IMessenger messenger)
        {
            MigrateOriginVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, messenger);
            MigrateTargetVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, messenger);
            CRUDGenerator.Inject(messenger, crudGeneratorViewModel);
            OptionGenerator.Inject(messenger, optionGeneratorViewModel);
            DtoGenerator.Inject(settingsService, consoleWriter, fileGeneratorService, messenger, dtoGeneratorViewModel);

            _viewModel = modifyProjectViewModel;
            _viewModel.MigrateOriginVm = MigrateOriginVersionAndOption.vm;
            _viewModel.MigrateTargetVm = MigrateTargetVersionAndOption.vm;
            _viewModel.OnSolutionParsedAction = InitVersionAndOptionComponents;
            DataContext = _viewModel;

            Loaded += (_, _) => _viewModel.Initialize();
            Unloaded += (_, _) => _viewModel.Cleanup();
        }

        private void InitVersionAndOptionComponents()
        {
            MigrateOriginVersionAndOption.vm.SelectVersion(_viewModel.CurrentProject?.FrameworkVersion);
            MigrateOriginVersionAndOption.vm.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, true, true);
            MigrateTargetVersionAndOption.vm.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, false, false,
                _viewModel.CurrentProject is null ?
                null :
                MigrateOriginVersionAndOption.vm.FeatureSettings.Select(x => x.FeatureSetting));
        }

        private void ModifyProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.ParameterModifyChange();
        }

        private void ModifyProjectRootFolderBrowse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath, "Choose modify project root path");
        }
    }
}
