namespace BIA.ToolKit.Dialogs
{
    using System.Windows.Controls;
    using BIA.ToolKit.ViewModels;

    public partial class DefaultTeamSettingsWindow : UserControl
    {
        public DefaultTeamSettingsViewModel ViewModel => DataContext as DefaultTeamSettingsViewModel;

        public DefaultTeamSettingsWindow(string teamName, string teamNamePlural, string domainName)
        {
            DataContext = new DefaultTeamSettingsViewModel(teamName, teamNamePlural, domainName);
            InitializeComponent();
        }
    }
}
