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
        public string Name { get; init; } = name;
        public RepositoryType RepositoryType { get; init; } = repositoryType;
        public string CompanyName { get; init; } = companyName;
        public string ProjectName { get; init; } = projectName;
        public abstract string LocalPath { get; }
        public List<Release> Releases { get; } = [];

        public abstract Task FillReleases();
    }
}
