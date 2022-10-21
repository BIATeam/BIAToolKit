namespace BIA.ToolKit.Domain.Settings
{
    public class RepositorySettings
    {
        public enum VersioningType
        {
            Folder,
            Tag,
            Release
        }

        /// The folder of the project.
        public string? Name { get; set; }

        /// The name of the project.
        public string? UrlRepo { get; set; }

        /// The name of the project. Do not use this path for manipulation on repo => use RootFolderPath
        public string? LocalFolderPath { get; set; }

        /// The Bia framework version of the project.
        public bool UseLocalFolder { get; set; }

        /// Specify if we use tag or sub folder
        public VersioningType Versioning { get; set; }

        /// The name of the project.
        public string? UrlRelease { get; set; }

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
