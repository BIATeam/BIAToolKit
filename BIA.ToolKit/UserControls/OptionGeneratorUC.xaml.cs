namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Interaction logic for OptionGenerator.xaml
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        public OptionGeneratorUC(OptionGeneratorViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
