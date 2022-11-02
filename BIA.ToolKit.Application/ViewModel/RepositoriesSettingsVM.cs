namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.Settings;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class RepositoriesSettingsVM : ObservableObject
    {
        ObservableCollection<RepositorySettings> repositoriesSettings;

        public RepositoriesSettingsVM()
        {
            repositoriesSettings = new ObservableCollection<RepositorySettings>();
        }

        public ObservableCollection<RepositorySettings> RepositoriesSettings {
            get { return repositoriesSettings; }
        }

        public void LoadSettings(List<RepositorySettings> repositoriesSettings)
        {
            this.repositoriesSettings.Clear();
            foreach(var repo in repositoriesSettings)
            {
                this.repositoriesSettings.Add(repo);
            }
            RaisePropertyChanged("RepositoriesSettings");
        }

        private RepositorySettings repositorySettings;

        public RepositorySettings RepositorySettings
        {
            get { return repositorySettings; }
            set
            {
                if (repositorySettings != value)
                {
                    repositorySettings = value;
                    RaisePropertyChanged("RepositorySettings");
                    RaisePropertyChanged("IsRepoSelected");
                }
            }
        }

        public bool IsRepoSelected
        {
            get { return repositorySettings != null; }
        }
    }
}
