namespace BIA.ToolKit.Domain
{
    using System.Text.RegularExpressions;

    public interface IRepositoryGit : IRepository
    {
        string GitRepositoryName { get; }
        string LocalClonedFolderPath { get; }
        string Owner { get; }
        string ReleasesFolderRegexPattern { get; }
        ReleaseType ReleaseType { get; }
        string Url { get; }
        bool UseLocalClonedFolder { get; }
        RepositoryGitKind RepositoryGitKind { get; }
        string UrlRelease { get; }
    }
}