namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Helper;

    /// <summary>
    /// Shared project-selector header used by both ModifyProjectUC and GenerateUC.
    /// DataContext is the singleton <see cref="ProjectViewModel"/>.
    /// </summary>
    public partial class ProjectSelectorUC : UserControl
    {
        private ProjectViewModel ViewModel => (ProjectViewModel)DataContext;

        public ProjectSelectorUC()
        {
            InitializeComponent();
        }

        private void BrowseRootFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.RootProjectsPath = FileDialog.BrowseFolder(ViewModel.RootProjectsPath, "Choose project root path");
        }
    }
}
