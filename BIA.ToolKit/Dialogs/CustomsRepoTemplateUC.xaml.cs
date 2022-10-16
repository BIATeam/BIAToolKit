namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
    public partial class CustomsRepoTemplateUC : Window
    {
        //List<CustomRepoTemplate> CustomsRepoTemplate = new List<CustomRepoTemplate>();
        ObservableCollection<CustomRepoTemplate> CustomsRepoTemplate { get; set; }

        public CustomsRepoTemplateUC()
        {
            InitializeComponent();
        }

        internal bool? ShowDialog(List<CustomRepoTemplate> customsRepoTemplate)
        {
            CustomsRepoTemplate = new ObservableCollection<CustomRepoTemplate>();
            foreach(var customRepoTemplate in customsRepoTemplate)
            {
                CustomsRepoTemplate.Add(customRepoTemplate);
            }
            dgCustomsRepoTemplate.ItemsSource = CustomsRepoTemplate;
            return ShowDialog();
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void addButton_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new CustomRepoTemplateUC { Owner = this };

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                CustomsRepoTemplate.Add((CustomRepoTemplate) dialog.DataContext);
            }

        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomsRepoTemplate.SelectedIndex >= 0)
            {
                var selectedItem = dgCustomsRepoTemplate.Items[dgCustomsRepoTemplate.SelectedIndex];
                if (selectedItem != null)
                {
                    var dialog = new CustomRepoTemplateUC { Owner = this };
                    // Display the dialog box and read the response
                    bool? result = dialog.ShowDialog((CustomRepoTemplate)selectedItem);

                    /*if (result == true)
                    {
                        dgCustomsRepoTemplate.Items[dgCustomsRepoTemplate.SelectedIndex] = dialog.customRepoTemplate;
                    }*/
                }
            }

        }
        private void synchronizeButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
