namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        public override string LocalPath => UseLocalClonedFolder ? LocalClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");
        public string Url { get; set; }
        public bool UseLocalClonedFolder { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public string LocalClonedFolderPath { get; set; }
        public string GitRepositoryName { get; set; }
        public string Owner { get; set; }
        public string ReleasesFolderRegexPattern { get; set; }
        public RepositoryGitKind RepositoryGitKind { get; set; }
        public string UrlRelease { get; set; }

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolderPath)
            : base(name, RepositoryType.Git, companyName, projectName)
        {
            Url = url;
            UseLocalClonedFolder = useLocalClonedFolder;
            ReleaseType = releaseType;
            LocalClonedFolderPath = localClonedFolderPath;
        }

        public static RepositoryGit CreateWithReleaseTypeFolder(string name, string url, string releasesFolderRegexPattern, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolderPath)
            {
                ReleasesFolderRegexPattern = releasesFolderRegexPattern
            };
        }

        public static RepositoryGit CreateWithReleaseTypeGit (string name, RepositoryGitKind repositoryGitKind, string url, string gitRepositoryName, string owner, bool useLocalClonedFolder = false, string urlRelease = null, string companyName = null, string projectName = null, string localClonedFolderPath = null)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolderPath)
            {
                GitRepositoryName = gitRepositoryName,
                Owner = owner,
                UrlRelease = urlRelease,
                RepositoryGitKind = repositoryGitKind
            };
        }

        public static RepositoryGit CreateWithReleaseTypeTag(string name, string url, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Tag, companyName, projectName, localClonedFolderPath);
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
            EnsureReleasesDownloaded();
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
                var releaseArchive = $"{release.TagName}.zip";

                if (release.Assets.Any())
                {
                    assets.AddRange(release.Assets.Select(a => new ReleaseGitAsset(a.Name, a.BrowserDownloadUrl, a.Size)));
                }
                else if (!string.IsNullOrWhiteSpace(UrlRelease))
                {
                    assets.Add(new ReleaseGitAsset(releaseArchive, $"{UrlRelease}/{releaseArchive}"));
                }
                else
                {
                    assets.Add(new ReleaseGitAsset(releaseArchive, $"{release.ZipballUrl}"));
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

            var releasesFolderRegex = new Regex(ReleasesFolderRegexPattern);
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
