namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.ViewModels;
    using CommunityToolkit.Mvvm.Messaging;

    /// <summary>
    /// Interaction logic for RepositoryFormUC.xaml
    /// Refactored for complete DI - browse logic moved to ViewModel.
    /// </summary>
    public partial class RepositoryFormUC : Window
    {
        public RepositoryFormViewModel ViewModel => DataContext as RepositoryFormViewModel;

        public RepositoryFormUC(
            RepositoryViewModel repository,
            GitService gitService,
            IMessenger messenger,
            IConsoleWriter consoleWriter,
            IFileDialogService fileDialogService)
        {
            DataContext = new RepositoryFormViewModel(
                repository,
                gitService,
                messenger,
                consoleWriter,
                fileDialogService);
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
