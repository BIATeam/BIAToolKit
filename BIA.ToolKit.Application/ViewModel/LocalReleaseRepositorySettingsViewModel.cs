namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;

    public class LocalReleaseRepositorySettingsViewModel : ObservableObject
    {
        public void LoadSettings(IBIATKSettings settings)
        {
            UseLocalReleaseRepository = settings.UseLocalReleaseRepository;
            LocalReleaseRepositoryPath = settings.LocalReleaseRepositoryPath;
        }

        private bool useLocalReleaseRepository;
        public bool UseLocalReleaseRepository
        {
            get => useLocalReleaseRepository;
            set 
            {
                useLocalReleaseRepository = value; 
                RaisePropertyChanged(nameof(UseLocalReleaseRepository));
            }
        }

        private string localReleaseRepositoryPath;
        public string LocalReleaseRepositoryPath
        {
            get => localReleaseRepositoryPath;
            set
            {
                localReleaseRepositoryPath = value;
                RaisePropertyChanged(nameof(LocalReleaseRepositoryPath));
            }
        }
    }
}
