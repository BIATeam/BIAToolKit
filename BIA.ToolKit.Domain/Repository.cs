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

    public abstract class Repository(string name, RepositoryType repositoryType, string companyName = null, string projectName = null) : IRepository
    {
        public string Name { get; } = name;
        public RepositoryType RepositoryType { get; } = repositoryType;
        public string CompanyName { get; } = companyName;
        public string ProjectName { get; } = projectName;
        public abstract string LocalPath { get; }
        [JsonIgnore]
        public List<Release> Releases { get; } = [];

        public abstract Task FillReleasesAsync();

        protected void EnsureReleasesDownloaded()
        {
            Releases.ForEach(r => r.EnsureDownloaded());
        }

        public void CleanReleases()
        {
            Releases.ForEach(r => r.Clean());
        }
    }
}
