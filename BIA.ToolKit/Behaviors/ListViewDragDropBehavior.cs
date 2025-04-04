namespace BIA.ToolKit.Behaviors
{
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using System;
    using System.Windows.Media;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Helper;

    public class ListViewDragDropBehavior : Behavior<ListView>
    {
        private Point _dragStartPoint;
        private object _draggedItem;
        private ListViewItem _lastTarget;

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
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.RequestBringIntoView += OnRequestBringIntoView;
            AssociatedObject.AllowDrop = true;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.Drop -= OnDrop;
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.RequestBringIntoView -= OnRequestBringIntoView;
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

            if (_lastTarget != null)
            {
                DragDropHelper.SetIsDropTarget(_lastTarget, false);
                _lastTarget = null;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            var listView = AssociatedObject;
            var pos = e.GetPosition(listView);
            var hitTestResult = VisualTreeHelper.HitTest(listView, pos);
            var itemContainer = FindAncestor<ListViewItem>(hitTestResult.VisualHit);

            if (_lastTarget != null && _lastTarget != itemContainer)
            {
                DragDropHelper.SetIsDropTarget(_lastTarget, false);
            }

            if (itemContainer != null)
            {
                DragDropHelper.SetIsDropTarget(itemContainer, true);
                _lastTarget = itemContainer;
            }
        }

        private void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
                current = VisualTreeHelper.GetParent(current);

            return current as T;
        }
    }
}
