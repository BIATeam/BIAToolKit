namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// MVVM Pure: All logic in ViewModel, code-behind only for DI and hosting child controls
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        private readonly ModifyProjectViewModel vm;
        private readonly VersionAndOptionViewModel originVersionVM;
        private readonly VersionAndOptionViewModel targetVersionVM;

        public ModifyProjectUC(
            ModifyProjectViewModel viewModel,
            VersionAndOptionViewModel originVersionControl,
            VersionAndOptionViewModel targetVersionControl,
            CRUDGeneratorUC crudGeneratorUC,
            OptionGeneratorUC optionGeneratorUC,
            DtoGeneratorUC dtoGeneratorUC)
        {
            InitializeComponent();

            vm = viewModel;
            originVersionVM = originVersionControl;
            targetVersionVM = targetVersionControl;

            DataContext = vm;

            // Host child controls resolved via DI
            MigrateOriginVersionAndOptionHost.Content = new VersionAndOptionUserControl(originVersionControl);
            MigrateTargetVersionAndOptionHost.Content = new VersionAndOptionUserControl(targetVersionControl);
            CRUDGeneratorHost.Content = crudGeneratorUC;
            OptionGeneratorHost.Content = optionGeneratorUC;
            DtoGeneratorHost.Content = dtoGeneratorUC;

            // Subscribe to events from ViewModel
            vm.SolutionClassesParsed += OnSolutionClassesParsed;
        }

        private void OnSolutionClassesParsed()
        {
            // Initialize version and option controls with current project data
            vm.InitializeVersionAndOption(originVersionVM, targetVersionVM);
        }
    }
}
