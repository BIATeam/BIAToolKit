namespace BIA.ToolKit.Domain.Work
{
    using System;
    using BIA.ToolKit.Domain.Settings;

    public class WorkRepository
    {
        public IRepository Repository { get; private set; }
        public string Version { get; private set; }
        public Version VersionData { get; private set; }
        public string VersionFolderPath { get; set; }

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

        public WorkRepository(IRepository template, string version)
        {
            Repository = template;
            Version = version;
            SetVersionData();
        }

        private void SetVersionData()
        {
            try
            {
                VersionData = new Version(Version.Remove(0, 1));
            }
            catch
            {
                VersionData = new Version();
            }
        }

        public class VersionComparer : IComparer<WorkRepository>
        {
            public int Compare(WorkRepository x, WorkRepository y)
            {
                if (x == null || y == null)
                    throw new ArgumentNullException("Cannot compare null objects.");

                return x.VersionData.CompareTo(y.VersionData);
            }
        }
    }
}
