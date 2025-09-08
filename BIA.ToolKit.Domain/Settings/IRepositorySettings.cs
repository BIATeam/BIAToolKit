namespace BIA.ToolKit.Domain.Settings
{
    public interface IRepositorySettings
    {
        string CompanyName { get;}
        bool HasUrlRelease { get; }
        string LocalFolderPath { get;}
        string Name { get;}
        string ProjectName { get;}
        string RootFolderPath { get; }
        string UrlRelease { get;}
        string UrlRepo { get;}
        bool UseLocalFolder { get;}
        VersioningType Versioning { get;}
    }
}