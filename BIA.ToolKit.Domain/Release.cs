namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using Octokit;

    public enum ReleaseType
    {
        Git,
        Folder,
        Tag
    }

    public abstract class Release(string name, string repositoryName)
    {
        public abstract ReleaseType ReleaseType { get; }
        public string Name { get; } = name;
        public string RepositoryName { get; } = repositoryName;
        public bool IsDownloaded { get; protected set; }
        public string LocalPath => Path.Combine(AppSettings.AppFolderPath, RepositoryName, Name);

        public abstract Task DownloadAsync();

        public void Clean()
        {
            if (!IsDownloaded)
                return;

            var dirInfo = new DirectoryInfo(LocalPath);
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
                File.Delete(file.FullName);
            }

            Directory.Delete(LocalPath, true);
            IsDownloaded = false;
        }

        public void EnsureDownloaded()
        {
            IsDownloaded = Directory.Exists(LocalPath);
        }

        protected virtual void InitDownload()
        {
            if (Directory.Exists(LocalPath))
            {
                Directory.Delete(LocalPath, true);
            }
        }
    }
}
