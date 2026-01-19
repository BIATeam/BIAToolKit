namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;

    public enum RepositoryType
    {
        Git,
        Folder
    }

    public abstract class Repository(string name, RepositoryType repositoryType, string companyName, string projectName, bool useRepository) : IRepository
    {
        public string Name { get; set; } = name;
        public RepositoryType RepositoryType { get; set; } = repositoryType;
        public string CompanyName { get; set; } = companyName;
        public string ProjectName { get; set; } = projectName;
        public bool UseRepository { get; set; } = useRepository;
        public bool UseDownloadedReleases { get; protected set; }
        public abstract string LocalPath { get; }

        protected List<Release> releases = [];
        public IReadOnlyList<Release> Releases => releases;

        public abstract Task FillReleasesAsync();

        protected void EnsureReleasesDownloaded()
        {
            if (UseDownloadedReleases)
                return;

            releases.ForEach(r => r.EnsureDownloaded());
        }

        public void CleanReleases()
        {
            releases.ForEach(r => r.Clean());
        }

        public void Clean()
        {
            CleanReleases();

            if (RepositoryType == RepositoryType.Git && Directory.Exists(LocalPath))
            {
                var dirInfo = new DirectoryInfo(LocalPath);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }

                Directory.Delete(LocalPath, true);
            }
        }

        protected void FillReleasesFromDownloadedReleases()
        {
            var releasesPath = Path.Combine(AppSettings.AppFolderPath, Name);
            if (!Directory.Exists(releasesPath))
            {
                return;
            }

            UseDownloadedReleases = true;
            var directoryInfo = new DirectoryInfo(releasesPath);
            var directories = directoryInfo.GetDirectories();
            var releases = directories
                .Where(dir => !dir.Name.Equals("Repo"))
                .Select(dir => new ReleaseFolder(dir.Name, dir.FullName, Name))
                .OrderByDescending(r => r.Name);

            this.releases.Clear();
            this.releases.AddRange(releases);
        }
    }
}
