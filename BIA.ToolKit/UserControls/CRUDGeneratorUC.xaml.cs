namespace BIA.ToolKit.UserControls
{
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;

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
        public void Inject(IMessenger messenger, CRUDGeneratorViewModel crudGeneratorViewModel)
        {
            this.vm = crudGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
        }

        /// <summary>
        /// Clears the plural name when the singular name is edited by the user.
        /// </summary>
        private void ModifyEntitySingular_TextChange(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlural = string.Empty;
        }

        /// <summary>
        /// Shows a confirmation dialog before deleting BIAToolkit annotations.
        /// </summary>
        private async void DeleteBIAToolkitAnnotations_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder message = new();
            message.AppendLine("Do you want to permanently remove all BIAToolkit annotations in code?");
            message.AppendLine("After that you will no longer be able to regenerate old CRUDs.");
            message.AppendLine();
            message.AppendLine("Be careful, this action is irreversible.");
            if (MessageBox.Show(message.ToString(), "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                await vm.DeleteAnnotationsAsync();
            }
        }
    }
}
