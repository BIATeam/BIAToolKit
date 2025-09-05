namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;

    public class LocalReleaseRepositorySettingsViewModel : ObservableObject
    {
        private BIATKSettings settings;

        public void LoadSettings(BIATKSettings settings)
        {
            this.settings = settings;
            RaisePropertyChanged(nameof(UseLocalReleaseRepository));
            RaisePropertyChanged(nameof(LocalReleaseRepositoryPath));
        }

        public bool? UseLocalReleaseRepository
        {
            get => settings?.UseLocalReleaseRepository;
            set 
            { 
                settings.UseLocalReleaseRepository = value.GetValueOrDefault(); 
                RaisePropertyChanged(nameof(UseLocalReleaseRepository));
            }
        }

        public string LocalReleaseRepositoryPath
        {
            get => settings?.LocalReleaseRepositoryPath;
            set
            {
                settings.LocalReleaseRepositoryPath = value;
                RaisePropertyChanged(nameof(LocalReleaseRepositoryPath));
            }
        }
    }
}
