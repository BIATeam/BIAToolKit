namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// MVVM Pure: All event handlers replaced by Commands via EventTriggers in XAML.
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        private readonly VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl(VersionAndOptionViewModel viewModel)
        {
            InitializeComponent();

            vm = viewModel;
            DataContext = vm;
        }
    }
}
