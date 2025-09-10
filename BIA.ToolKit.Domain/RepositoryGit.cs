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
    using static System.Net.WebRequestMethods;

    public enum RepositoryGitKind
    {
        Github
    }

    public class RepositoryGit : Repository, IRepositoryGit
    {
        private readonly Regex releasesFolderRegex;

        public override string LocalPath => UseLocalClonedFolder ? LocalClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");
        public string Url { get; }
        public bool UseLocalClonedFolder { get; }
        public ReleaseType ReleaseType { get; }
        public string LocalClonedFolderPath { get; }
        public string GitRepositoryName { get; }
        public string Owner { get; }
        public string ReleasesFolderRegexPattern { get; }
        public RepositoryGitKind RepositoryGitKind { get; }
        public string UrlRelease { get; }

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolderPath)
            : base(name, RepositoryType.Git, companyName, projectName)
        {
            Url = url;
            UseLocalClonedFolder = useLocalClonedFolder;
            ReleaseType = releaseType;
            LocalClonedFolderPath = localClonedFolderPath;
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
        public RepositoryGit(string name, string url, string releasesFolderRegexPattern, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null)
            : this(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolderPath)
        {
            ReleasesFolderRegexPattern = releasesFolderRegexPattern;
            releasesFolderRegex = new Regex(releasesFolderRegexPattern);
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
        public RepositoryGit(string name, RepositoryGitKind repositoryGitKind, string url, string gitRepositoryName, string owner, bool useLocalClonedFolder = false, string urlRelease = null, string companyName = null, string projectName = null, string localClonedFolderPath = null)
            : this(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolderPath)
        {
            GitRepositoryName = gitRepositoryName;
            Owner = owner;
            UrlRelease = urlRelease;
            RepositoryGitKind = repositoryGitKind;
        }

        /// <summary>
        /// Constructor for <see cref="RepositoryGit"/> with <see cref="ReleaseType.Tag"/>.
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
        public RepositoryGit(string name, string url, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null)
            : this(name, url, useLocalClonedFolder, ReleaseType.Tag, companyName, projectName, localClonedFolderPath)
        {
            
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
                case ReleaseType.Tag:
                    FillReleasesTag();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task FillReleasesGitAsync()
        {
            switch (RepositoryGitKind)
            {
                case RepositoryGitKind.Github:
                    await FillReleasesGithubAsync();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task FillReleasesGithubAsync()
        {
            var github = new GitHubClient(new Octokit.ProductHeaderValue("BIAToolKit"));
            var repositoryReleases = await github.Repository.Release.GetAll(Owner, GitRepositoryName);

            Releases.Clear();
            foreach (var release in repositoryReleases)
            {
                var assets = new List<ReleaseGitAsset>();
                if(release.Assets.Any())
                {
                    assets.AddRange(release.Assets.Select(a => new ReleaseGitAsset(a.Name, a.BrowserDownloadUrl, a.Size)));
                }
                else
                {
                    if(string.IsNullOrWhiteSpace(UrlRelease))
                    {
                        throw new Exception($"Release URL not provided");
                    }
                    var releaseArchive = $"{release.TagName}.zip";
                    assets.Add(new ReleaseGitAsset(releaseArchive, $"{UrlRelease}/{releaseArchive}"));
                }

                Releases.Add(new ReleaseGit(release.TagName, Name, assets));
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
                .Where(directoryPath => releasesFolderRegex.IsMatch(Path.GetFileName(directoryPath)))
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name))
                .OrderByDescending(r => r.Name);

            Releases.Clear();
            Releases.AddRange(releases);
        }

        private void FillReleasesTag()
        {
            using var gitRepo = new LibGit2Sharp.Repository(LocalPath);
            var releases = gitRepo.Tags.Select(t => new ReleaseTag(t.FriendlyName, this));
            
            Releases.Clear();
            Releases.AddRange(releases);
        }
    }
}
