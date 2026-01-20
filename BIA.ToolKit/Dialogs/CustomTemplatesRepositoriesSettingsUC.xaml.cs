namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Domain.Settings;
    using System.Windows;

    /// <summary>
    /// Interaction logic for CustomRepoTemplate.xaml
    /// </summary>
    public partial class CustomsRepoTemplateUC : Window
    {
        GitService gitService;
        public RepositoriesSettingsVM vm;
        private RepositoryService repositoryService;

        public CustomsRepoTemplateUC(GitService gitService, RepositoryService repositoryService)
        {
            InitializeComponent();
            this.gitService = gitService;
            this.repositoryService = repositoryService;
            vm = (RepositoriesSettingsVM)base.DataContext;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomRepoTemplateUC { Owner = this };
            dialog.ShowDialog();
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement edit functionality when needed
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement delete functionality when needed
        }

        private void synchronizeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement synchronize functionality when needed
        }
    }
}
