namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Infrastructure;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(RepositoryViewModel repository, GitService gitService, UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            DataContext = new RepositoryFormViewModel(repository, gitService, eventBroker, consoleWriter, new DialogService(this));
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
