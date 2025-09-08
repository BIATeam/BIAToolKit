namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using Octokit;

    public class RepositoryGit : Repository
    {
        public string Url { get; init; }
        public string GitRepositoryName { get; init; }
        public string Product { get; init; }
        public string Owner { get; init; }
        public bool UseLocalClonedFolder { get; init; }
        public string LocalClonedFolderPath { get; init; }
        public ReleaseType ReleaseType { get; init; }

        public override string LocalPath => UseLocalClonedFolder ? LocalClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolder) : base(name, RepositoryType.Git, companyName, projectName)
        {
            Url = url;
            UseLocalClonedFolder = useLocalClonedFolder;
            ReleaseType = releaseType;
            LocalClonedFolderPath = localClonedFolder;
        }

        public RepositoryGit(string name, string url, bool useLocalClonedFolder, string companyName = null, string projectName = null, string localClonedFolder = null) : this(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolder)
        {
            
        }

        public RepositoryGit(string name, string url, string gitRepositoryName, string product, string owner, bool useLocalClonedFolder, string companyName = null, string projectName = null, string localClonedFolder = null) : this(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolder)
        {
            GitRepositoryName = gitRepositoryName;
            Product = product;
            Owner = owner;
        }

        public override async Task FillReleases()
        {
            switch (ReleaseType)
            {
                case ReleaseType.Git:
                    await FillReleasesGit();
                    break;
                case ReleaseType.Folder:
                    FillReleasesFolder();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task FillReleasesGit()
        {
            var github = new GitHubClient(new Octokit.ProductHeaderValue(Product));
            var repositoryReleases = await github.Repository.Release.GetAll(Owner, GitRepositoryName);

            Releases.Clear();
            foreach (var release in repositoryReleases)
            {
                Releases.Add(new ReleaseGit(release.TagName, release.HtmlUrl, Name, ReleaseType.Git, release.Assets));
            }
        }

        private void FillReleasesFolder()
        {
            if (!Directory.Exists(LocalPath))
            {
                throw new DirectoryNotFoundException(LocalPath);
            }

            var releases = Directory
                .EnumerateDirectories(LocalPath)
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name));

            Releases.Clear();
            Releases.AddRange(releases);
        }
    }
}
