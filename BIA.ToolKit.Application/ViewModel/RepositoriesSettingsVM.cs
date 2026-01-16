namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class RepositoriesSettingsVM : ObservableObject
    {
        //ObservableCollection<IRepositorySettings> repositoriesSettings;

        //public RepositoriesSettingsVM()
        //{
        //    repositoriesSettings = new ObservableCollection<IRepositorySettings>();
        //}

        //public ObservableCollection<IRepositorySettings> RepositoriesSettings {
        //    get { return repositoriesSettings; }
        //}

        //public void LoadSettings(IReadOnlyList<IRepositorySettings> repositoriesSettings)
        //{
        //    this.repositoriesSettings.Clear();
        //    foreach(var repo in repositoriesSettings)
        //    {
        //        this.repositoriesSettings.Add(repo);
        //    }
        //    RaisePropertyChanged(nameof(RepositoriesSettings));
        //}

        //private RepositorySettings repositorySettings;

        //public RepositorySettings RepositorySettings
        //{
        //    get { return repositorySettings; }
        //    set
        //    {
        //        if (repositorySettings != value)
        //        {
        //            repositorySettings = value;
        //            RaisePropertyChanged(nameof(RepositorySettings));
        //            RaisePropertyChanged(nameof(IsRepoSelected));
        //        }
        //    }
        //}

        //public bool IsRepoSelected
        //{
        //    get { return repositorySettings != null; }
        //}
    }
}
