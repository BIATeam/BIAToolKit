namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;

    /// <summary>
    /// Unified card for a single repository entry. Bound directly to a
    /// RepositoryViewModel via its DataContext; consumed by RepositorySectionUC.
    /// </summary>
    public partial class RepositoryCardUC : UserControl
    {
        public RepositoryCardUC() { InitializeComponent(); }
    }
}
