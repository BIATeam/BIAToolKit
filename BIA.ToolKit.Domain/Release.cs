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

        public string Name { get; init; } = name;
        public string OriginPath { get; init; } = originPath;
        public string RepositoryName { get; init; } = repositoryName;
        public bool IsDownloaded { get; private set; }
        public string LocalPath => Path.Combine(AppSettings.AppFolderPath, RepositoryName, Name);
    }
}
