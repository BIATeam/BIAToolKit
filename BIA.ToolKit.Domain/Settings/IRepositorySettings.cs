namespace BIA.ToolKit.Domain.Settings
{
    public interface IRepositorySettings
    {
        string Name { get;}
        string CompanyName { get;}
        string LocalFolderPath { get;}
        string ProjectName { get;}
        string RootFolderPath { get; }
        string UrlRelease { get;}
        string UrlRepo { get;}
        bool UseLocalFolder { get;}
        VersioningType Versioning { get;}
    }
}