namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;

    public class RepositorySettingsVM : ObservableObject
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
        //            RaisePropertyChanged("Name");
        //            RaisePropertyChanged("UrlRepo");
        //            RaisePropertyChanged("UseLocalFolder");
        //            RaisePropertyChanged("LocalFolderPath");
        //            RaisePropertyChanged("Versioning");
        //            RaisePropertyChanged("UrlRelease");
        //            RaisePropertyChanged("CompanyName");
        //            RaisePropertyChanged("ProjectName");
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
        //            RaisePropertyChanged("Name");
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
        //            RaisePropertyChanged("UrlRepo");
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
        //            RaisePropertyChanged("UseLocalFolder");
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
        //            RaisePropertyChanged("LocalFolderPath");
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
        //            RaisePropertyChanged("Versioning"); 
        //            RaisePropertyChanged("IsEnabledUrlRelease");
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
        //            RaisePropertyChanged("UrlRelease");
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
        //            RaisePropertyChanged("CompanyName");
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
        //            RaisePropertyChanged("ProjectName");
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
