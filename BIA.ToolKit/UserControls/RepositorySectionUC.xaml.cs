namespace BIA.ToolKit.UserControls
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// One Config-tab section: header (icon + title + subtitle + [+ Add]),
    /// either a UniformGrid of RepositoryCardUC items or the empty-state
    /// panel that offers the Safran default bootstrap.
    /// </summary>
    public partial class RepositorySectionUC : UserControl
    {
        public RepositorySectionUC() { InitializeComponent(); }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

        public static readonly DependencyProperty EmptyHintProperty =
            DependencyProperty.Register(nameof(EmptyHint), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string EmptyHint { get => (string)GetValue(EmptyHintProperty); set => SetValue(EmptyHintProperty, value); }

        public static readonly DependencyProperty DefaultSourceNameProperty =
            DependencyProperty.Register(nameof(DefaultSourceName), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string DefaultSourceName { get => (string)GetValue(DefaultSourceNameProperty); set => SetValue(DefaultSourceNameProperty, value); }

        public static readonly DependencyProperty DefaultSourcePathProperty =
            DependencyProperty.Register(nameof(DefaultSourcePath), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string DefaultSourcePath { get => (string)GetValue(DefaultSourcePathProperty); set => SetValue(DefaultSourcePathProperty, value); }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(RepositorySectionUC),
                new PropertyMetadata(null, OnItemsChanged));
        public IEnumerable Items { get => (IEnumerable)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

        public static readonly DependencyProperty HasItemsProperty =
            DependencyProperty.Register(nameof(HasItems), typeof(bool), typeof(RepositorySectionUC), new PropertyMetadata(false));
        public bool HasItems { get => (bool)GetValue(HasItemsProperty); private set => SetValue(HasItemsProperty, value); }

        public static readonly DependencyProperty ShowAddButtonProperty =
            DependencyProperty.Register(nameof(ShowAddButton), typeof(bool), typeof(RepositorySectionUC), new PropertyMetadata(true));
        public bool ShowAddButton { get => (bool)GetValue(ShowAddButtonProperty); set => SetValue(ShowAddButtonProperty, value); }

        public static readonly DependencyProperty AddCommandProperty =
            DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(RepositorySectionUC), new PropertyMetadata(null));
        public ICommand AddCommand { get => (ICommand)GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }

        public static readonly DependencyProperty UseDefaultCommandProperty =
            DependencyProperty.Register(nameof(UseDefaultCommand), typeof(ICommand), typeof(RepositorySectionUC), new PropertyMetadata(null));
        public ICommand UseDefaultCommand { get => (ICommand)GetValue(UseDefaultCommandProperty); set => SetValue(UseDefaultCommandProperty, value); }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RepositorySectionUC self) return;

            if (e.OldValue is INotifyCollectionChanged oldNotify)
                oldNotify.CollectionChanged -= self.OnItemsCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newNotify)
                newNotify.CollectionChanged += self.OnItemsCollectionChanged;

            self.RefreshHasItems();
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RefreshHasItems();

        private void RefreshHasItems()
        {
            bool any = false;
            if (Items is not null)
            {
                foreach (var _ in Items) { any = true; break; }
            }
            HasItems = any;
        }
    }
}
