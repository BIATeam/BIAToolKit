namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;

    public class DefaultTeamSettingsViewModel(string teamName, string teamNamePlural, string domainName) : ObservableObject
    {
        private string defaultTeamName = teamName;
        public string DefaultTeamName
        {
            get => defaultTeamName;
            set
            {
                defaultTeamName = value;
                RaisePropertyChanged(nameof(DefaultTeamName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        private string defaultTeamNamePlural = teamNamePlural;
        public string DefaultTeamNamePlural
        {
            get => defaultTeamNamePlural;
            set
            {
                defaultTeamNamePlural = value;
                RaisePropertyChanged(nameof(DefaultTeamNamePlural));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        private string defaultTeamDomainName = domainName;
        public string DefaultTeamDomainName
        {
            get => defaultTeamDomainName;
            set
            {
                defaultTeamDomainName = value;
                RaisePropertyChanged(nameof(DefaultTeamDomainName));
                RaisePropertyChanged(nameof(IsValid));
            }
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(DefaultTeamName) &&
            !string.IsNullOrWhiteSpace(DefaultTeamNamePlural) &&
            !string.IsNullOrWhiteSpace(DefaultTeamDomainName);
    }
}
