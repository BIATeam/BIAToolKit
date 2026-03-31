namespace BIA.ToolKit.Application.ViewModel
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using BIA.ToolKit.Domain.Settings;

    public partial class RepositorySettingsVM : ObservableObject
    {

        //public RepositorySettingsVM()
        //{
        //    RepositorySettings = new RepositorySettings();
        //}

        //private RepositorySettings repositorySettings { get; set; }

        //public RepositorySettings RepositorySettings {
        //    get { return repositorySettings; }
        //    set
        //    {
        //        if (repositorySettings != value)
        //        {
        //            repositorySettings = value;
        //            OnPropertyChanged("Name");
        //            OnPropertyChanged("UrlRepo");
        //            OnPropertyChanged("UseLocalFolder");
        //            OnPropertyChanged("LocalFolderPath");
        //            OnPropertyChanged("Versioning");
        //            OnPropertyChanged("UrlRelease");
        //            OnPropertyChanged("CompanyName");
        //            OnPropertyChanged("ProjectName");
        //        }
        //    }
        //}

        //public string Name
        //{
        //    get { return RepositorySettings.Name; }
        //    set
        //    {
        //        if (RepositorySettings.Name != value)
        //        {
        //            RepositorySettings.Name = value;
        //            OnPropertyChanged("Name");
        //        }
        //    }
        //}

        //public string UrlRepo
        //{
        //    get { return RepositorySettings.UrlRepo; }
        //    set
        //    {
        //        if (RepositorySettings.UrlRepo != value)
        //        {
        //            RepositorySettings.UrlRepo = value;
        //            OnPropertyChanged("UrlRepo");
        //        }
        //    }
        //}

        //public bool UseLocalFolder
        //{
        //    get { return RepositorySettings.UseLocalFolder; }
        //    set
        //    {
        //        if (RepositorySettings.UseLocalFolder != value)
        //        {
        //            RepositorySettings.UseLocalFolder = value;
        //            OnPropertyChanged("UseLocalFolder");
        //        }
        //    }
        //}

        //public string LocalFolderPath
        //{
        //    get { return RepositorySettings.LocalFolderPath; }
        //    set
        //    {
        //        if (RepositorySettings.LocalFolderPath != value)
        //        {
        //            RepositorySettings.LocalFolderPath = value;
        //            OnPropertyChanged("LocalFolderPath");
        //        }
        //    }
        //}

        //public VersioningType Versioning
        //{
        //    get { return RepositorySettings.Versioning; }
        //    set
        //    {
        //        if (RepositorySettings.Versioning != value)
        //        {
        //            RepositorySettings.Versioning = value;
        //            OnPropertyChanged("Versioning"); 
        //            OnPropertyChanged("IsEnabledUrlRelease");
        //        }
        //    }
        //}

        //public string UrlRelease
        //{
        //    get { return RepositorySettings.UrlRelease; }
        //    set
        //    {
        //        if (RepositorySettings.UrlRelease != value)
        //        {
        //            RepositorySettings.UrlRelease = value;
        //            OnPropertyChanged("UrlRelease");
        //        }
        //    }
        //}

        //public string CompanyName
        //{
        //    get { return RepositorySettings.CompanyName; }
        //    set
        //    {
        //        if (RepositorySettings.CompanyName != value)
        //        {
        //            RepositorySettings.CompanyName = value;
        //            OnPropertyChanged("CompanyName");
        //        }
        //    }
        //}

        //public string ProjectName
        //{
        //    get { return RepositorySettings.ProjectName; }
        //    set
        //    {
        //        if (RepositorySettings.ProjectName != value)
        //        {
        //            RepositorySettings.ProjectName = value;
        //            OnPropertyChanged("ProjectName");
        //        }
        //    }
        //}

        //public bool IsEnabledUrlRelease
        //{
        //    get
        //    {
        //        return Versioning==VersioningType.Release;
        //    }
        //}
    }
}
