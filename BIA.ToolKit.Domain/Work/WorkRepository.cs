namespace BIA.ToolKit.Domain.Work
{
    using BIA.ToolKit.Domain.Settings;

    public class WorkRepository
    {
        public RepositorySettings RepositorySettings { get; private set; }
        public string Version { get; private set; }

        public string? VersionFolderPath { get; set; }

        //public string? VersionFolderPath
        //{
        //    get
        //    {
        //        if (Template.Versioning == RepositorySettings.VersioningType.Folder)
        //        {
        //            return Template.RootFolderPath + "\\" + Version;
        //        }
        //        else if (Template.Versioning == RepositorySettings.VersioningType.Release)
        //        {
        //            return AppSettings.AppFolderPath + 
        //        }
        //    }
        //}

        public WorkRepository(RepositorySettings template, string version)
        {
            RepositorySettings = template;
            Version = version;
        }
    }
}
