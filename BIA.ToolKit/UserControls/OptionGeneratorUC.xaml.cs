namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.ViewModels;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for OptionGeneratorUC.xaml
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        public OptionGeneratorViewModel ViewModel => DataContext as OptionGeneratorViewModel;

        public OptionGeneratorUC()
        {
            InitializeComponent();
        }

        private void RootScrollViewer_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
