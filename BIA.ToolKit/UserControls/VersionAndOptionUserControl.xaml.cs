namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// MVVM: All logic in ViewModel, code-behind only for DI and delegating events to ViewModel
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

        // Simple event handlers that delegate to ViewModel
        // These cannot be pure bindings due to WPF SelectionChanged limitations
        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.HandleFrameworkVersionSelectionChanged();
        }

        private void CFVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.HandleCompanyFilesSelectionChanged();
        }
    }
}
