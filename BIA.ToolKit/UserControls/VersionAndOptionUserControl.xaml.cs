namespace BIA.ToolKit.UserControls
{
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Interaction logic for VersionAndOptionUserControl.xaml
    /// DataContext (VersionAndOptionViewModel) is set by the parent control.
    /// Code-behind contains NO business logic — interact with the VM directly.
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        public VersionAndOptionViewModel ViewModel => DataContext as VersionAndOptionViewModel;

        /// <summary>
        /// DependencyProperty exposant la validite de la configuration des features au parent (binding XAML).
        /// </summary>
        public static readonly DependencyProperty IsFeatureConfigValidProperty =
            DependencyProperty.Register(
                nameof(IsFeatureConfigValid),
                typeof(bool),
                typeof(VersionAndOptionUserControl),
                new PropertyMetadata(true));

        public bool IsFeatureConfigValid
        {
            get => (bool)GetValue(IsFeatureConfigValidProperty);
            private set => SetValue(IsFeatureConfigValidProperty, value);
        }

        public VersionAndOptionUserControl()
        {
            InitializeComponent();

            // Synchroniser la DependencyProperty IsFeatureConfigValid avec le ViewModel
            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is VersionAndOptionViewModel vm)
                {
                    vm.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(VersionAndOptionViewModel.IsDefaultTeamSetupValid))
                            IsFeatureConfigValid = vm.IsDefaultTeamSetupValid;
                    };
                }
            };
        }
    }
}
