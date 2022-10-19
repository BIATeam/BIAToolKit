namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for CustomRepoTemplate.xaml
    /// </summary>
    public partial class CustomRepoTemplateUC : Window
    {

        public CustomRepoTemplateUC()
        {
            InitializeComponent();
            this.DataContext = new Repository() { UseLocalFolder = false };
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        internal bool? ShowDialog(Repository currentItem)
        {
            this.DataContext = currentItem;
            return ShowDialog();
        }
    }
}
