namespace BIA.ToolKit.Dialogs
{
    using System.Windows;
    using BIA.ToolKit.Application.ViewModel;

    public partial class DefaultTeamSettingsWindow : Window
    {
        public DefaultTeamSettingsViewModel ViewModel => DataContext as DefaultTeamSettingsViewModel;

        public DefaultTeamSettingsWindow(string teamName, string teamNamePlural, string domainName)
        {
            DataContext = new DefaultTeamSettingsViewModel(teamName, teamNamePlural, domainName);
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
