namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        public ModifyProjectUC()
        {
            InitializeComponent();

            var viewModel = App.GetService<ModifyProjectViewModel>();
            DataContext = viewModel;

            viewModel.OriginVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            viewModel.TargetVersionAndOptionVM = App.GetService<VersionAndOptionViewModel>();
            MigrateOriginVersionAndOption.DataContext = viewModel.OriginVersionAndOptionVM;
            MigrateTargetVersionAndOption.DataContext = viewModel.TargetVersionAndOptionVM;

            RegenerateFeatures.DataContext = App.GetService<RegenerateFeaturesViewModel>();

            var projectViewModel = App.GetService<ProjectViewModel>();
            ProjectSelector.DataContext = projectViewModel;
        }
    }
}
