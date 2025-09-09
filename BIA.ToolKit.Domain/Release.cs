namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;

    public enum ReleaseType
    {
        Git,
        Folder
    }

    public abstract class Release(string name, string originPath, string repositoryName)
    {
        public abstract ReleaseType ReleaseType { get; }
        public string Name { get; } = name;
        public string OriginPath { get;} = originPath;
        public string RepositoryName { get; } = repositoryName;
        public bool IsDownloaded { get; protected set; }
        public string LocalPath => Path.Combine(AppSettings.AppFolderPath, RepositoryName, Name);

        public abstract Task DownloadAsync();

        protected void InitDownload()
        {
            if (!Directory.Exists(OriginPath))
            {
                throw new DirectoryNotFoundException(OriginPath);
            }

            if (Directory.Exists(LocalPath))
            {
                Directory.Delete(LocalPath, true);
            }
        }
    }
}
