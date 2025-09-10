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
        public override string LocalPath => path;
        public string ReleaseFolderRegexPattern { get; } = releaseFolderRegexPattern;

        public override Task FillReleasesAsync()
        {
            if (!Directory.Exists(LocalPath))
            {
                throw new DirectoryNotFoundException(LocalPath);
            }

            var repositoryFolders = Directory.EnumerateDirectories(LocalPath);

            if(!string.IsNullOrWhiteSpace(ReleaseFolderRegexPattern))
            {
                var regex = new Regex(ReleaseFolderRegexPattern);
                repositoryFolders = repositoryFolders.Where(dir => regex.IsMatch(Path.GetFileName(dir)));
            }

            var releases = repositoryFolders
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name))
                .OrderByDescending(r => r.Name);

            Releases.Clear();
            Releases.AddRange(releases);

            return Task.CompletedTask;
        }
    }
}
