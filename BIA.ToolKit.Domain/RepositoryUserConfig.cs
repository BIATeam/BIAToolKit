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

        public string LocalPath { get; set; }

        public string Name { get; set; }

        public string ProjectName { get; set; }

        [JsonIgnore]
        public List<Release> Releases { get; set; }

        public RepositoryType RepositoryType { get; set; }

        public string GitRepositoryName { get; set; }

        public string LocalClonedFolderPath { get; set; }

        public string Owner { get; set; }

        public string ReleasesFolderRegexPattern { get; set; }

        public ReleaseType ReleaseType { get; set; }

        public string Url { get; set; }

        public bool UseLocalClonedFolder { get; set; }

        public Task FillReleasesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
