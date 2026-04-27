namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.ViewModels;
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
