namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Model;

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
                featureSetting.IsSelected = value; 
                RaisePropertyChanged(nameof(IsSelected));
                RaisePropertyChanged(nameof(DisplayDisabledFeatures));
            }
        }

        public string Name => featureSetting.DisplayName;
        public string Description => featureSetting.Description;
        public bool DisplayDisabledFeatures => !string.IsNullOrWhiteSpace(DisabledFeatures) && !IsSelected;
        public string DisabledFeatures { get; set; }
    }
}
