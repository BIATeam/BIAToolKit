namespace BIA.ToolKit.Application.ViewModel
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain.Model;

    public partial class FeatureSettingViewModel(FeatureSetting featureSetting) : ObservableObject
    {
        public FeatureSetting FeatureSetting => featureSetting;
        public bool IsSelected
        {
            get 
            { 
                return featureSetting.IsSelected; 
            }
            set 
            { 
                featureSetting.IsSelected = value; 
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(DisplayDisabledFeatures));
            }
        }

        public string Name => featureSetting.DisplayName;
        public string Description => featureSetting.Description;
        public bool DisplayDisabledFeatures => !string.IsNullOrWhiteSpace(DisabledFeatures) && !IsSelected;
        public string DisabledFeatures { get; set; }
    }
}
