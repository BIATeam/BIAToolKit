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
        public RepositoriesSettingsVM vm;
        private RepositoryService repositoryService;
        private readonly UIEventBroker uiEventBroker;

        public CustomsRepoTemplateUC(GitService gitService, RepositoryService repositoryService, UIEventBroker uiEventBroker)
        {
            InitializeComponent();
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            this.uiEventBroker = uiEventBroker;
            vm = (RepositoriesSettingsVM)base.DataContext;
        }

        internal bool? ShowDialog(IReadOnlyList<IRepositorySettings> customsRepoTemplate)
        {
            vm.LoadSettings(customsRepoTemplate);
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
                vm.RepositoriesSettings.Add(((RepositorySettingsVM)dialog.DataContext).RepositorySettings);
            }
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (vm.RepositorySettings != null)
            {
                RepositorySettings clonedSelectedItem = JsonSerializer.Deserialize<RepositorySettings>(JsonSerializer.Serialize(vm.RepositorySettings));
                var dialog = new CustomRepoTemplateUC { Owner = this };
                // Display the dialog box and read the response
                bool? result = dialog.ShowDialog((RepositorySettings)clonedSelectedItem);

                if (result == true)
                {
                    vm.RepositoriesSettings[vm.RepositoriesSettings.IndexOf(vm.RepositorySettings)] = ((RepositorySettingsVM)dialog.DataContext).RepositorySettings;
                }
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (vm.RepositorySettings != null)
            {
                vm.RepositoriesSettings.Remove(vm.RepositorySettings);
            }
        }


        private void synchronizeButton_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(async () =>
            {
                if (vm.RepositorySettings != null)
                {
                    if (!vm.RepositorySettings.UseLocalFolder)
                    {
                        await this.repositoryService.CleanRepository(vm.RepositorySettings);
                    }
                    await gitService.Synchronize(vm.RepositorySettings);
                }
            });
        }
    }
}
