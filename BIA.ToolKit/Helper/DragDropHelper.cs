namespace BIA.ToolKit.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public static class DragDropHelper
    {
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsDropTarget", typeof(bool), typeof(DragDropHelper), new PropertyMetadata(false));

        public static void SetIsDropTarget(DependencyObject element, bool value)
            => element.SetValue(IsDropTargetProperty, value);

        public static bool GetIsDropTarget(DependencyObject element)
            => (bool)element.GetValue(IsDropTargetProperty);
    }
}
