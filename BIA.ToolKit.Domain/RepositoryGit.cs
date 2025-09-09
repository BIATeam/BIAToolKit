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
        public override string LocalPath => UseLocalClonedFolder ? LocalClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");
        public string Url { get; }
        public bool UseLocalClonedFolder { get; }
        public ReleaseType ReleaseType { get; }
        public string LocalClonedFolderPath { get; }
        public string GitRepositoryName { get; }
        public string Product { get; }
        public string Owner { get; }
        public Regex ReleasesFolderPrefixRegex { get; }

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolderPath)
            : base(name, RepositoryType.Git, companyName, projectName)
        {
            Url = url;
            this.UseLocalClonedFolder = useLocalClonedFolder;
            this.ReleaseType = releaseType;
            this.LocalClonedFolderPath = localClonedFolderPath;
        }

        /// <summary>
        /// Constructor for <see cref="RepositoryGit"/> with <see cref="ReleaseType.Folder"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <param name="useLocalClonedFolder"></param>
        /// <param name="releasesFolderRegexPattern"></param>
        /// <param name="companyName"></param>
        /// <param name="projectName"></param>
        /// <param name="localClonedFolderPath"></param>
        public RepositoryGit(string name, string url, bool useLocalClonedFolder, string releasesFolderRegexPattern, string companyName = null, string projectName = null, string localClonedFolderPath = null)
            : this(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolderPath)
        {
            ReleasesFolderPrefixRegex = new Regex(releasesFolderRegexPattern);
        }

        /// <summary>
        /// Constructor for <see cref="RepositoryGit"/> with <see cref="ReleaseType.Git"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <param name="gitRepositoryName"></param>
        /// <param name="product"></param>
        /// <param name="owner"></param>
        /// <param name="useLocalClonedFolder"></param>
        /// <param name="companyName"></param>
        /// <param name="projectName"></param>
        /// <param name="localClonedFolderPath"></param>
        public RepositoryGit(string name, string url, string gitRepositoryName, string product, string owner, bool useLocalClonedFolder, string companyName = null, string projectName = null, string localClonedFolderPath = null)
            : this(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolderPath)
        {
            this.GitRepositoryName = gitRepositoryName;
            this.Product = product;
            this.Owner = owner;
        }

        public override async Task FillReleasesAsync()
        {
            switch (ReleaseType)
            {
                case ReleaseType.Git:
                    await FillReleasesGitAsync();
                    break;
                case ReleaseType.Folder:
                    FillReleasesFolder();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task FillReleasesGitAsync()
        {
            var github = new GitHubClient(new Octokit.ProductHeaderValue(Product));
            var repositoryReleases = await github.Repository.Release.GetAll(Owner, GitRepositoryName);

            Releases.Clear();
            foreach (var release in repositoryReleases)
            {
                Releases.Add(new ReleaseGit(release.TagName, release.HtmlUrl, Name, release.Assets));
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
                .Where(directoryPath => ReleasesFolderPrefixRegex.IsMatch(Path.GetFileName(directoryPath)))
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name));

            Releases.Clear();
            Releases.AddRange(releases);
        }
    }
}
