namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public sealed class RepositoryFolder(string name, string path, string releasesFolderRegexPattern = null, string companyName = null, string projectName = null, bool useRepository = false) : Repository(name, RepositoryType.Folder, companyName, projectName, useRepository), IRepositoryFolder
    {
        public override string LocalPath => Path;
        public string Path { get; set; } = path;
        public string ReleasesFolderRegexPattern { get; set; } = releasesFolderRegexPattern;

        public override Task FillReleasesAsync()
        {
            try
            {
                UseDownloadedReleases = false;
                if (!Directory.Exists(LocalPath))
                {
                    throw new DirectoryNotFoundException(LocalPath);
                }

                var repositoryFolders = Directory.EnumerateDirectories(LocalPath);

                if (!string.IsNullOrWhiteSpace(ReleasesFolderRegexPattern))
                {
                    var regex = new Regex(ReleasesFolderRegexPattern);
                    repositoryFolders = repositoryFolders.Where(dir => regex.IsMatch(System.IO.Path.GetFileName(dir)));
                }

                var releases = repositoryFolders
                    .Select(directoryPath => new ReleaseFolder(System.IO.Path.GetFileName(directoryPath), directoryPath, Name))
                    .OrderByDescending(r => r.Name);

                this.releases.Clear();
                this.releases.AddRange(releases);
                EnsureReleasesDownloaded();
            }
            catch
            {
                FillReleasesFromDownloadedReleases();
            }

            return Task.CompletedTask;
        }
    }
}
