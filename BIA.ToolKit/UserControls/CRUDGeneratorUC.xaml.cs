namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    public partial class CRUDGeneratorUC : UserControl
    {
        public CRUDGeneratorUC(CRUDGeneratorViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
