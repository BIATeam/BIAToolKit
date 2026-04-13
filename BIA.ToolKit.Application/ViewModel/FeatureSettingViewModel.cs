namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;

    public class FeatureSettingViewModel(FeatureSetting featureSetting) : ObservableObject
    {
        public FeatureSetting FeatureSetting => featureSetting;

        public bool IsSelected
        {
            get => featureSetting.IsSelected;
            set
            {
                featureSetting.IsSelected = value;
                RaisePropertyChanged(nameof(IsSelected));
                RaisePropertyChanged(nameof(DisplayDisabledFeatures));
                RaisePropertyChanged(nameof(ShowDefaultTeamConfigError));
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

        private bool isDefaultTeamConfigValid;

        /// <summary>
        /// Validité de la configuration DefaultTeam. Écrit par VersionAndOptionViewModel.
        /// </summary>
        public bool IsDefaultTeamConfigValid
        {
            get => isDefaultTeamConfigValid;
            set
            {
                isDefaultTeamConfigValid = value;
                RaisePropertyChanged(nameof(IsDefaultTeamConfigValid));
                RaisePropertyChanged(nameof(ShowDefaultTeamConfigError));
            }
        }

        /// <summary>
        /// Affiche l'icône d'erreur si CreateDefaultTeam est sélectionné mais non configuré.
        /// </summary>
        public bool ShowDefaultTeamConfigError => IsCreateDefaultTeam && IsSelected && !isDefaultTeamConfigValid;
    }
}
