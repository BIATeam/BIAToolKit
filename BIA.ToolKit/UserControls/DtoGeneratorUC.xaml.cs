namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;
    using Microsoft.Xaml.Behaviors;
    using BIA.ToolKit.Behaviors;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class DtoGeneratorUC : UserControl
    {
        private readonly DtoGeneratorViewModel vm;

        public DtoGeneratorUC(DtoGeneratorViewModel viewModel)
        {
            InitializeComponent();

            vm = viewModel;
            vm.RequestResetMappingColumnsWidths = ResetMappingColumnsWidths;
            DataContext = vm;
        }

        private void ResetMappingColumnsWidths()
        {
            var gridView = PropertiesListView.View as GridView;
            if (gridView != null)
            {
                foreach (var column in gridView.Columns)
                {
                    column.Width = 0;
                    column.Width = double.NaN;
                }
            }
        }

        private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm?.ComputePropertiesValidity();
        }

        private void MappingOptionId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm?.ComputePropertiesValidity();
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
