namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using Octokit;

    public class RepositoryGit : Repository
    {
        private readonly bool useLocalClonedFolder;
        private readonly ReleaseType releaseType;
        private readonly string localClonedFolderPath;
        private readonly string gitRepositoryName;
        private readonly string product;
        private readonly string owner;
        private readonly Regex releasesFolderPrefixRegex;

        public string Url { get; set; }
        public override string LocalPath => useLocalClonedFolder ? localClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolderPath) : base(name, RepositoryType.Git, companyName, projectName)
        {
            Url = url;
            this.useLocalClonedFolder = useLocalClonedFolder;
            this.releaseType = releaseType;
            this.localClonedFolderPath = localClonedFolderPath;
        }

        public RepositoryGit(string name, string url, bool useLocalClonedFolder, string releasesFolderRegexPattern, string companyName = null, string projectName = null, string localClonedFolderPath = null) : this(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolderPath)
        {
            releasesFolderPrefixRegex = new Regex(releasesFolderRegexPattern);
        }

        public RepositoryGit(string name, string url, string gitRepositoryName, string product, string owner, bool useLocalClonedFolder, string companyName = null, string projectName = null, string localClonedFolderPath = null) : this(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolderPath)
        {
            this.gitRepositoryName = gitRepositoryName;
            this.product = product;
            this.owner = owner;
        }

        public override async Task FillReleases()
        {
            switch (releaseType)
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
            var github = new GitHubClient(new Octokit.ProductHeaderValue(product));
            var repositoryReleases = await github.Repository.Release.GetAll(owner, gitRepositoryName);

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
                .Where(directoryPath => releasesFolderPrefixRegex.IsMatch(Path.GetFileName(directoryPath)))
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name));

            Releases.Clear();
            Releases.AddRange(releases);
        }
    }
}
