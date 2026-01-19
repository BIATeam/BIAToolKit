namespace BIA.ToolKit.Dialogs
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for CustomRepoTemplate.xaml
    /// </summary>
    public partial class CustomRepoTemplateUC : Window
    {
        public CustomRepoTemplateUC()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}
