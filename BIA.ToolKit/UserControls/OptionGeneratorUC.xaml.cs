namespace BIA.ToolKit.UserControls
{
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Application.ViewModel.Interfaces;

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
        public void Inject(IMessenger messenger, OptionGeneratorViewModel optionGeneratorViewModel)
        {
            this.vm = optionGeneratorViewModel;
            DataContext = vm;
            Loaded += (_, _) => vm.Initialize();
            Unloaded += (_, _) => vm.Cleanup();
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
