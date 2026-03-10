namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModel;

    /// <summary>
    /// Interaction logic for CRUDGeneratorUC.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        private CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CRUDGeneratorViewModel crudGeneratorViewModel)
        {
            this.vm = crudGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }
    }
}
