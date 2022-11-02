namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
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
        GitService gitService;
        //List<CustomRepoTemplate> CustomsRepoTemplate = new List<CustomRepoTemplate>();
        public ObservableCollection<RepositorySettings> CustomsRepoTemplate = new ObservableCollection<RepositorySettings>();

        public CustomsRepoTemplateUC(GitService gitService)
        {
            this.gitService = gitService;
            InitializeComponent();
        }

        internal bool? ShowDialog(List<RepositorySettings> customsRepoTemplate)
        {
            CustomsRepoTemplate = new ObservableCollection<RepositorySettings>(customsRepoTemplate);
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
                CustomsRepoTemplate.Add(((RepositorySettingsVM) dialog.DataContext).RepositorySettings);
            }

        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomsRepoTemplate.SelectedIndex >= 0)
            {
                RepositorySettings selectedItem = (RepositorySettings) dgCustomsRepoTemplate.Items[dgCustomsRepoTemplate.SelectedIndex];
                if (selectedItem != null)
                {
                    RepositorySettings clonedSelectedItem = JsonSerializer.Deserialize<RepositorySettings>(JsonSerializer.Serialize(selectedItem));
                    var dialog = new CustomRepoTemplateUC { Owner = this };
                    // Display the dialog box and read the response
                    bool? result = dialog.ShowDialog((RepositorySettings)clonedSelectedItem);

                    if (result == true)
                    {
                        CustomsRepoTemplate[dgCustomsRepoTemplate.SelectedIndex] = ((RepositorySettingsVM) dialog.DataContext).RepositorySettings;
                    }
                }
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomsRepoTemplate.SelectedIndex >= 0)
            {
                RepositorySettings selectedItem = (RepositorySettings)dgCustomsRepoTemplate.Items[dgCustomsRepoTemplate.SelectedIndex];
                if (selectedItem != null)
                {
                    CustomsRepoTemplate.Remove(selectedItem);
                }
            }
        }


        private void synchronizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomsRepoTemplate.SelectedIndex >= 0)
            {
                RepositorySettings selectedItem = (RepositorySettings)dgCustomsRepoTemplate.Items[dgCustomsRepoTemplate.SelectedIndex];
                _ = gitService.Synchronize(selectedItem);
            }
        }
    }
}
