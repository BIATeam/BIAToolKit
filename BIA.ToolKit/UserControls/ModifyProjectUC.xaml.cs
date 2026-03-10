namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModel;

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

        public void Inject(ModifyProjectViewModel modifyProjectViewModel,
            CRUDGeneratorViewModel crudGeneratorViewModel, DtoGeneratorViewModel dtoGeneratorViewModel, OptionGeneratorViewModel optionGeneratorViewModel,
            VersionAndOptionViewModel migrateOriginVm, VersionAndOptionViewModel migrateTargetVm)
        {
            MigrateOriginVersionAndOption.Inject(migrateOriginVm);
            MigrateTargetVersionAndOption.Inject(migrateTargetVm);
            CRUDGenerator.Inject(crudGeneratorViewModel);
            OptionGenerator.Inject(optionGeneratorViewModel);
            DtoGenerator.Inject(dtoGeneratorViewModel);

            _viewModel = modifyProjectViewModel;
            _viewModel.MigrateOriginVm = MigrateOriginVersionAndOption.vm;
            _viewModel.MigrateTargetVm = MigrateTargetVersionAndOption.vm;
            DataContext = _viewModel;

            Loaded += (_, _) => _viewModel.Initialize();
            Unloaded += (_, _) => _viewModel.Cleanup();
        }

        private void ModifyProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.ParameterModifyChange();
        }
    }
}
