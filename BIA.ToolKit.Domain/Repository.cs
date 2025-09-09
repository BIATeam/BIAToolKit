namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;

    public enum RepositoryType
    {
        Git,
        Folder
    }

    public abstract class Repository(string name, RepositoryType repositoryType, string companyName = null, string projectName = null)
    {
        public string Name { get; } = name;
        public RepositoryType RepositoryType { get; } = repositoryType;
        public string CompanyName { get; } = companyName;
        public string ProjectName { get; } = projectName;
        public abstract string LocalPath { get; }
        public List<Release> Releases { get; } = [];

        public abstract Task FillReleasesAsync();
    }
}
