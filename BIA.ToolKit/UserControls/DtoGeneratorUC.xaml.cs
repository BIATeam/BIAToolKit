namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Behaviors;
    using Microsoft.Xaml.Behaviors;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// DataContext (DtoGeneratorViewModel) is set by the parent control.
    /// Code-behind contains NO business logic — interact with the VM directly.
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        public DtoGeneratorViewModel ViewModel => DataContext as DtoGeneratorViewModel;

        public DtoGeneratorUC()
        {
            InitializeComponent();

            // Subscribe to VM event for UI-only operation (GridView column width reset)
            DataContextChanged += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.OnMappingRefreshed += ResetMappingColumnsWidths;
                }
            };
        }

        /// <summary>
        /// UI-only operation: Reset GridView column widths (non-bindable).
        /// CONSERVER - manipulation de controle non-bindable.
        /// </summary>
        private void ResetMappingColumnsWidths()
        {
            var gridView = PropertiesListView.View as GridView;
            if (gridView == null) return;

            foreach (var column in gridView.Columns)
            {
                column.Width = 0;
                column.Width = double.NaN;
            }
        }

        /// <summary>
        /// UI-only operation: Drag-drop delegates to Behavior.
        /// CONSERVER - UI pure, aucun acces au VM.
        /// </summary>
        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewDragDropBehavior behavior = GetDragDropBehavior();
            behavior?.HandleDragStart(sender, e);
        }

        /// <summary>
        /// UI-only operation: Drag-drop delegates to Behavior.
        /// CONSERVER - UI pure, aucun acces au VM.
        /// </summary>
        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewDragDropBehavior behavior = GetDragDropBehavior();
            behavior?.HandleDragMove(sender, e);
        }

        private ListViewDragDropBehavior GetDragDropBehavior()
        {
            return Interaction.GetBehaviors(PropertiesListView)
                              .OfType<ListViewDragDropBehavior>()
                              .FirstOrDefault();
        }
    }
}
