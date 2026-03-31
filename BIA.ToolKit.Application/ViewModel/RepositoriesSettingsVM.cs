namespace BIA.ToolKit.Application.ViewModel
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain.Settings;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public partial class RepositoriesSettingsVM : ObservableObject
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
        //    OnPropertyChanged(nameof(RepositoriesSettings));
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
        //            OnPropertyChanged(nameof(RepositorySettings));
        //            OnPropertyChanged(nameof(IsRepoSelected));
        //        }
        //    }
        //}

        //public bool IsRepoSelected
        //{
        //    get { return repositorySettings != null; }
        //}
    }
}
