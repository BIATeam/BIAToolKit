namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.ViewModels;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for OptionGeneratorUC.xaml
    /// DataContext (OptionGeneratorViewModel) is set by the parent control.
    /// Code-behind contains NO business logic — interact with the VM directly.
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        public OptionGeneratorViewModel ViewModel => DataContext as OptionGeneratorViewModel;

        public OptionGeneratorUC()
        {
            InitializeComponent();
        }
    }
}
