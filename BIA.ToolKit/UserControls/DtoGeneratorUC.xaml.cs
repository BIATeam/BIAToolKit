namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.ViewModel;
    using BIA.ToolKit.Behaviors;
    using Microsoft.Xaml.Behaviors;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : ViewModelUserControl<DtoGeneratorViewModel>
    {
        private bool processSelectProperties;

        public DtoGeneratorUC()
        {
            InitializeComponent();
        }

        private void SelectProperties_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            processSelectProperties = true;
            ViewModel?.RefreshMappingProperties();
            ResetMappingColumnsWidths();
            ViewModel?.ComputePropertiesValidity();
            processSelectProperties = false;
        }

        private void ResetMappingColumnsWidths()
        {
            var gridView = PropertiesListView.View as GridView;
            foreach (var column in gridView.Columns)
            {
                column.Width = 0;
                column.Width = double.NaN;
            }
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel?.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (processSelectProperties)
                return;

            ViewModel?.ComputePropertiesValidity();
        }

        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var behavior = GetDragDropBehavior();
            behavior?.HandleDragStart(sender, e);
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            var behavior = GetDragDropBehavior();
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
