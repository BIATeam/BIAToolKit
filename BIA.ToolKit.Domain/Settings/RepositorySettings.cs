namespace BIA.ToolKit.Domain.Settings
{
    using System.Text.Json.Serialization;

    public enum VersioningType
    {
        Folder,
        Tag,
        Release
    }

    public class RepositorySettings
    {
        /// The folder of the repository.
        public string? Name { get; set; }

        /// The url of the repository.
        public string? UrlRepo { get; set; }

        /// true if use a local cloned folder.
        public bool UseLocalFolder { get; set; }

        /// The local cloned folder path. Do not use this path for manipulation on repo => use RootFolderPath
        public string? LocalFolderPath { get; set; }

        /// Specify if we use release, tag or sub folder
        public VersioningType Versioning { get; set; }

        /// Url where to find release.
        public string? UrlRelease { get; set; }

        /// The name of the company to rename.
        public string? CompanyName { get; set; }

        /// The name of the project to rename.
        public string? ProjectName { get; set; }

        public bool HasUrlRelease => !string.IsNullOrWhiteSpace(UrlRelease);

        [JsonIgnore]
        // The path where is the root repository (it can be LocalFolderPath or in AppFolder is not UseLocalFolder)
        public string? RootFolderPath
        {
            get
            {
                if (UseLocalFolder)
                {
                    return LocalFolderPath;
                }
                else 
                {
                    return AppSettings.AppFolderPath + "\\" + Name + "\\Repo";
                }
            }
        }
    }
}
