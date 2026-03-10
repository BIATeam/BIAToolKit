namespace BIA.ToolKit.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain.Model;

    public partial class FeatureSettingViewModel(FeatureSetting featureSetting) : ObservableObject
    {
        public FeatureSetting FeatureSetting => featureSetting;

        private bool _isSelected = featureSetting.IsSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    featureSetting.IsSelected = value;
                    OnPropertyChanged(nameof(DisplayDisabledFeatures));
                }
            }
        }

        public string Name => featureSetting.DisplayName;
        public string Description => featureSetting.Description;
        public bool DisplayDisabledFeatures => !string.IsNullOrWhiteSpace(DisabledFeatures) && !IsSelected;
        public string DisabledFeatures { get; set; }
    }
}
