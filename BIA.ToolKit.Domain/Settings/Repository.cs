namespace BIA.ToolKit.Domain.Settings
{
    public class Repository
    {
        /// The folder of the project.
        public string? Name { get; set; }

        /// The name of the project.
        public string? UrlRepo { get; set; }

        /// The name of the project.
        public string? LocalFolderPath { get; set; }

        /// The Bia framework version of the project.
        public bool UseLocalFolder { get; set; }

        /// Specify if we use tag or sub folder
        public bool WorkWithSubFolder { get; set; }
    }
}
