namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Model;
    using CommunityToolkit.Mvvm.ComponentModel;

    public class FeatureSettingViewModel(FeatureSetting featureSetting) : ObservableObject
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
                if (featureSetting.IsSelected == value)
                {
                    return;
                }

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
