namespace BIA.ToolKit.UserControls
{
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Horizontal stepper showing one cell per migration step.
    /// Bind <see cref="Steps"/> to an <c>IEnumerable&lt;MigrationStep&gt;</c>
    /// (typically <c>ModifyProjectViewModel.Steps</c>).
    /// </summary>
    public partial class MigrationStepperUC : UserControl
    {
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(
                nameof(Steps),
                typeof(IEnumerable),
                typeof(MigrationStepperUC),
                new PropertyMetadata(null));

        public IEnumerable Steps
        {
            get => (IEnumerable)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public MigrationStepperUC()
        {
            InitializeComponent();
        }
    }
}
