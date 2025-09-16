namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class RepositoryUserConfig : IRepositoryFolder, IRepositoryGit
    {
        public string CompanyName { get; set; }

        [JsonIgnore]
        public string LocalPath { get; set; }

        public string Name { get; set; }

        public string ProjectName { get; set; }

        [JsonIgnore]
        public IReadOnlyList<Release> Releases { get; }

        public RepositoryType RepositoryType { get; set; }

        public string GitRepositoryName { get; set; }

        public string LocalClonedFolderPath { get; set; }

        public string Owner { get; set; }

        public string ReleasesFolderRegexPattern { get; set; }

        public ReleaseType ReleaseType { get; set; }

        public string Url { get; set; }

        public bool UseLocalClonedFolder { get; set; }

        public RepositoryGitKind RepositoryGitKind { get; set; }

        public string UrlRelease { get; set; }

        public string Path { get; set; }

        public bool UseRepository { get; set; }

        public bool IsVersionXYZ { get; set; }

        public void Clean()
        {
            throw new NotImplementedException();
        }

        public void CleanReleases()
        {
            throw new NotImplementedException();
        }

        public Task FillReleasesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
