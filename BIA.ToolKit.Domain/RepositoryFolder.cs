namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RepositoryFolder(string name, string path, string companyName = null, string projectName = null) : Repository(name, RepositoryType.Folder, companyName, projectName), IRepositoryFolder
    {
        public override string LocalPath => path;

        public override Task FillReleasesAsync()
        {
            if (!Directory.Exists(LocalPath))
            {
                throw new DirectoryNotFoundException(LocalPath);
            }

            var releases = Directory
                .EnumerateDirectories(LocalPath)
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name))
                .OrderByDescending(r => r.Name);

            Releases.Clear();
            Releases.AddRange(releases);

            return Task.CompletedTask;
        }
    }
}
