namespace BIA.ToolKit.Behaviors
{
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using System;
    using System.Windows.Media;
    using BIA.ToolKit.Common;

    public class ListViewDragDropBehavior : Behavior<ListView>
    {
        private Point _dragStartPoint;
        private object _draggedItem;

        public static readonly DependencyProperty MoveCommandProperty =
            DependencyProperty.Register(
                nameof(MoveCommand),
                typeof(ICommand),
                typeof(ListViewDragDropBehavior),
                new PropertyMetadata(null));

        public ICommand MoveCommand
        {
            get => (ICommand)GetValue(MoveCommandProperty);
            set => SetValue(MoveCommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.AllowDrop = true;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.Drop -= OnDrop;
        }

        public void HandleDragStart(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            if (sender is FrameworkElement fe)
            {
                _draggedItem = fe.DataContext;
            }
        }

        public void HandleDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
                return;

            Point pos = e.GetPosition(null);
            Vector diff = _dragStartPoint - pos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop((DependencyObject)sender, _draggedItem, DragDropEffects.Move);
                _draggedItem = null;
            }
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var diff = _dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var item = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (item != null)
                {
                    var data = AssociatedObject.ItemContainerGenerator.ItemFromContainer(item);
                    if (data != null)
                        DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
                }
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var listView = AssociatedObject;
            var listViewType = listView.Items[0]?.GetType();

            if (!e.Data.GetDataPresent(listViewType))
                return;

            var droppedData = e.Data.GetData(listViewType);
            var target = ((FrameworkElement)e.OriginalSource).DataContext;

            if (droppedData == null || target == null || droppedData == target)
                return;

            var oldIndex = listView.Items.IndexOf(droppedData);
            var newIndex = listView.Items.IndexOf(target);

            if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
                return;

            var args = new MoveItemArgs(oldIndex, newIndex);

            if (MoveCommand is ICommand command && command.CanExecute(args))
            {
                command.Execute(args);
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
                current = VisualTreeHelper.GetParent(current);

            return current as T;
        }
    }
}
