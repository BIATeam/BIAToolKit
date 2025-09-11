namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class RepositoryFolder(string name, string path, string releaseFolderRegexPattern = null, string companyName = null, string projectName = null) : Repository(name, RepositoryType.Folder, companyName, projectName), IRepositoryFolder
    {
        public override string LocalPath => Path;
        public string Path { get; set; } = path;
        public string ReleaseFolderRegexPattern { get; set; } = releaseFolderRegexPattern;

        public override Task FillReleasesAsync()
        {
            if (!Directory.Exists(LocalPath))
            {
                throw new DirectoryNotFoundException(LocalPath);
            }

            var repositoryFolders = Directory.EnumerateDirectories(LocalPath);

            if (!string.IsNullOrWhiteSpace(ReleaseFolderRegexPattern))
            {
                var regex = new Regex(ReleaseFolderRegexPattern);
                repositoryFolders = repositoryFolders.Where(dir => regex.IsMatch(System.IO.Path.GetFileName(dir)));
            }

            var releases = repositoryFolders
                .Select(directoryPath => new ReleaseFolder(System.IO.Path.GetFileName(directoryPath), directoryPath, Name))
                .OrderByDescending(r => r.Name);

            this.releases.Clear();
            this.releases.AddRange(releases);
            EnsureReleasesDownloaded();

            return Task.CompletedTask;
        }
    }
}
