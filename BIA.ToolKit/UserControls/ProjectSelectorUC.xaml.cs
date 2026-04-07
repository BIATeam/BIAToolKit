namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Helper;

    /// <summary>
    /// Shared project-selector header used by both ModifyProjectUC and GenerateUC.
    /// DataContext is the singleton <see cref="ProjectViewModel"/>.
    /// </summary>
    public partial class ProjectSelectorUC : UserControl
    {
        private ProjectViewModel viewModel;

        /// <summary>
        /// Raised when the root path TextBox content changes.
        /// Subscribe to this in parent UCs that need to react (e.g. ModifyProjectUC resets migration buttons).
        /// </summary>
        public event EventHandler RootPathTextChanged;

        public ProjectSelectorUC()
        {
            InitializeComponent();
        }

        public void Inject(ProjectViewModel vm)
        {
            viewModel = vm;
            DataContext = vm;
        }

        private void BrowseRootFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            viewModel.RootProjectsPath = FileDialog.BrowseFolder(viewModel.RootProjectsPath, "Choose project root path");
        }

        private void RefreshProjectList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            viewModel.RefreshProjetsList();
        }

        private void RootPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RootPathTextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
