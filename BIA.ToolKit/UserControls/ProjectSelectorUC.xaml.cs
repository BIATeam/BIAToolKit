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
        private ProjectViewModel ViewModel => (ProjectViewModel)DataContext;

        /// <summary>
        /// Raised when the root path TextBox content changes.
        /// Previously used by ModifyProjectUC to reset migration buttons;
        /// now handled in ModifyProjectViewModel.CurrentProject setter.
        /// Kept as a public extension point for parent UCs.
        /// </summary>
        public event EventHandler RootPathTextChanged;

        public ProjectSelectorUC()
        {
            InitializeComponent();
        }

        private void BrowseRootFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.RootProjectsPath = FileDialog.BrowseFolder(ViewModel.RootProjectsPath, "Choose project root path");
        }

        private void RefreshProjectList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.RefreshProjetsList();
        }

        private void RootPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RootPathTextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
