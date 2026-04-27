namespace BIA.ToolKit.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class DefaultTeamSettingsViewModel(string teamName, string teamNamePlural, string domainName) : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private string defaultTeamName = teamName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private string defaultTeamNamePlural = teamNamePlural;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private string defaultTeamDomainName = domainName;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(DefaultTeamName) &&
            !string.IsNullOrWhiteSpace(DefaultTeamNamePlural) &&
            !string.IsNullOrWhiteSpace(DefaultTeamDomainName);
    }
}
