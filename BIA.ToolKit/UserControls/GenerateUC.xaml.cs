namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for GenerateUC.xaml
    /// </summary>
    public partial class GenerateUC : UserControl
    {
        public GenerateUC()
        {
            InitializeComponent();

            ProjectViewModel projectViewModel = App.GetService<ProjectViewModel>();
            DataContext = projectViewModel;
            ProjectSelector.DataContext = projectViewModel;

            CRUDGenerator.DataContext = App.GetService<CRUDGeneratorViewModel>();
            DtoGenerator.DataContext = App.GetService<DtoGeneratorViewModel>();
            OptionGenerator.DataContext = App.GetService<OptionGeneratorViewModel>();
        }
    }
}
