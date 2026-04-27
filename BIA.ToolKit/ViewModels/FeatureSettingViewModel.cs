namespace BIA.ToolKit.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;

    public partial class FeatureSettingViewModel(FeatureSetting featureSetting) : ObservableObject
    {
        public FeatureSetting FeatureSetting => featureSetting;

        public bool IsSelected
        {
            get => featureSetting.IsSelected;
            set
            {
                featureSetting.IsSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(DisplayDisabledFeatures));
                OnPropertyChanged(nameof(ShowDefaultTeamConfigError));
            }
        }

        public string Name => featureSetting.DisplayName;
        public string Description => featureSetting.Description;
        public bool DisplayDisabledFeatures => !string.IsNullOrWhiteSpace(DisabledFeatures) && !IsSelected;
        public string DisabledFeatures { get; set; }

        /// <summary>
        /// Indique si cette feature est "CreateDefaultTeam".
        /// </summary>
        public bool IsCreateDefaultTeam => featureSetting.Id == (int)BiaFeatureSettingsEnum.CreateDefaultTeam;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDefaultTeamConfigError))]
        private bool isDefaultTeamConfigValid;

        /// <summary>
        /// Affiche l'icone d'erreur si CreateDefaultTeam est selectionne mais non configure.
        /// </summary>
        public bool ShowDefaultTeamConfigError => IsCreateDefaultTeam && IsSelected && !IsDefaultTeamConfigValid;
    }
}
