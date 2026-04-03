namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using LibGit2Sharp;

    public sealed class ReleaseTag(string name, IRepositoryGit repository) : Release(name, repository.Name)
    {
        public override ReleaseType ReleaseType => ReleaseType.Tag;

        public override async Task DownloadAsync()
        {
            InitDownload();

            await Task.Run(() =>
            {

                string gitCopyFolder = LocalPath;
                if (!string.IsNullOrEmpty(repository.ReleasesTagContentFolder))
                {
                    gitCopyFolder = Path.Combine(Path.GetDirectoryName(LocalPath), "temp");
                    CleanFolder(gitCopyFolder);
                }


                LibGit2Sharp.Repository.Clone(repository.LocalPath, gitCopyFolder);
                using (var gitRepository = new LibGit2Sharp.Repository(gitCopyFolder))
                {
                    Tag tag = gitRepository.Tags.FirstOrDefault(t => t.FriendlyName == Name)
                        ?? throw new NotFoundException($"Tag {Name} not found in repository {repository.Name}");
                    Commit commit = tag.Target.Peel<Commit>()
                        ?? throw new NotFoundException($"Tag {Name} does not resolve to a commit.");
                    Commands.Checkout(gitRepository, commit);
                }

                if (!string.IsNullOrEmpty(repository.ReleasesTagContentFolder))
                {
                    CopyDirectory(Path.Combine(gitCopyFolder, repository.ReleasesTagContentFolder), LocalPath);
                    CleanFolder(gitCopyFolder);
                }
            });

            IsDownloaded = true;
        }

        private static void CleanFolder(string gitCopyFolder)
        {
            if (Directory.Exists(gitCopyFolder))
            {
                var dirInfo = new DirectoryInfo(gitCopyFolder);
                foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }

                Directory.Delete(gitCopyFolder, true);
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            if (recursive)
            {
                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                    CopyDirectory(subDir, destSubDir, recursive);
                }
            }
        }
    }
}
