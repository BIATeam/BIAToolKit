namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModel;

    /// <summary>
    /// Interaction logic for OptionGenerator.xaml
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        private OptionGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public OptionGeneratorUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(OptionGeneratorViewModel optionGeneratorViewModel)
        {
            this.vm = optionGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }
    }
}
