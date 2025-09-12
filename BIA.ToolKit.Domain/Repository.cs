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
        public abstract string LocalPath { get; }

        protected List<Release> releases = [];
        [JsonIgnore]
        public IReadOnlyList<Release> Releases => releases;

        public abstract Task FillReleasesAsync();

        protected void EnsureReleasesDownloaded()
        {
            releases.ForEach(r => r.EnsureDownloaded());
        }

        public void CleanReleases()
        {
            releases.ForEach(r => r.Clean());
        }

        public void Clean()
        {
            CleanReleases();

            var dirInfo = new DirectoryInfo(LocalPath);
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
                File.Delete(file.FullName);
            }

            Directory.Delete(LocalPath, true);
        }
    }
}
