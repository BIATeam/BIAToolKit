namespace BIA.ToolKit.Dialogs
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Lightweight popup to edit just the Company Files (version / profile / options)
    /// without opening the full "More options" Advanced panel.
    /// DataContext is the shared <see cref="VersionAndOptionViewModel"/> so edits are live.
    /// </summary>
    public partial class CompanyFilesDialogUC : UserControl
    {
        public CompanyFilesDialogUC(VersionAndOptionViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
