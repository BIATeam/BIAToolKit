namespace BIA.ToolKit.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Behavior to reset GridView column widths when triggered via a bound property.
    /// This eliminates the need for a callback/delegate in the ViewModel.
    /// </summary>
    public class ResetColumnsWidthBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Gets or sets the trigger property. When this changes to true, 
        /// the columns widths are reset and the property is set back to false.
        /// </summary>
        public static readonly DependencyProperty ResetTriggerProperty =
            DependencyProperty.Register(
                nameof(ResetTrigger),
                typeof(bool),
                typeof(ResetColumnsWidthBehavior),
                new PropertyMetadata(false, OnResetTriggerChanged));

        public bool ResetTrigger
        {
            get => (bool)GetValue(ResetTriggerProperty);
            set => SetValue(ResetTriggerProperty, value);
        }

        private static void OnResetTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResetColumnsWidthBehavior behavior && e.NewValue is bool isTriggered && isTriggered)
            {
                behavior.ResetColumnsWidths();
            }
        }

        /// <summary>
        /// Resets all column widths in the associated ListView's GridView.
        /// Setting width to 0 then to NaN forces the column to auto-resize.
        /// </summary>
        private void ResetColumnsWidths()
        {
            if (AssociatedObject?.View is GridView gridView)
            {
                foreach (var column in gridView.Columns)
                {
                    column.Width = 0;
                    column.Width = double.NaN;
                }
            }
        }
    }
}
