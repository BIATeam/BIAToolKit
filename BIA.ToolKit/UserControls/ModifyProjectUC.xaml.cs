namespace BIA.ToolKit.UserControls
{
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        ModifyProjectViewModel _viewModel;
        UIEventBroker uiEventBroker;

        public ModifyProjectUC()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker, ModifyProjectViewModel modifyProjectViewModel,
            CRUDGeneratorViewModel crudGeneratorViewModel, DtoGeneratorViewModel dtoGeneratorViewModel, OptionGeneratorViewModel optionGeneratorViewModel,
            IMessenger messenger)
        {
            MigrateOriginVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, uiEventBroker, messenger);
            MigrateTargetVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, uiEventBroker, messenger);
            CRUDGenerator.Inject(uiEventBroker, crudGeneratorViewModel);
            OptionGenerator.Inject(uiEventBroker, optionGeneratorViewModel);
            DtoGenerator.Inject(settingsService, consoleWriter, fileGeneratorService, uiEventBroker, dtoGeneratorViewModel);
            this.uiEventBroker = uiEventBroker;

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

        private void Migrate_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.MigrateAsync);
        }

        private void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.MigrateGenerateOnlyAsync);
        }

        private void MigrateApplyDiff_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.MigrateApplyDiffAsync);
        }

        private void MigrateMergeRejected_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.MigrateMergeRejectedAsync);
        }

        private void MigrateOverwriteBIAFolder_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.MigrateOverwriteBIAFolderAsync);
        }

        private void MigrateOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppSettings.TmpFolderPath);
        }

        private void ModifyProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath, "Choose modify project root path");
        }

        private void RefreshProjectFolderList_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshProjetsList();
        }

        private void FixUsings_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(_viewModel.FixUsingsAsync);
        }
    }
}
