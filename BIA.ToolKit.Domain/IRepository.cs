namespace BIA.ToolKit.Domain
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public interface IRepository
    {
        string CompanyName { get; }
        string LocalPath { get; }
        string Name { get; }
        string ProjectName { get; }
        IReadOnlyList<Release> Releases { get; }
        RepositoryType RepositoryType { get; }
        bool UseRepository { get; }

        Task FillReleasesAsync();
        void CleanReleases();
    }
}