namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;

    /// <summary>
    /// Interaction logic for VersionAndOptionUserControl.xaml
    /// DataContext (VersionAndOptionViewModel) is set by the parent control.
    /// Code-behind contains NO business logic — interact with the VM directly.
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        public VersionAndOptionViewModel ViewModel => DataContext as VersionAndOptionViewModel;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
        }
    }
}
