namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using LibGit2Sharp;
    using Newtonsoft.Json;
    using Octokit;
    using static System.Net.WebRequestMethods;

    public enum RepositoryGitKind
    {
        Github
    }

    public sealed class RepositoryGit : Repository, IRepositoryGit
    {
        public override string LocalPath => UseLocalClonedFolder ? LocalClonedFolderPath : Path.Combine(AppSettings.AppFolderPath, Name, "Repo");
        public string Url { get; set; }
        public bool UseLocalClonedFolder { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public string LocalClonedFolderPath { get; set; }
        public string GitRepositoryName { get; set; }
        public string Owner { get; set; }
        public string ReleasesFolderRegexPattern { get; set; }
        public string ReleasesTagContentFolder { get; set; }
        public RepositoryGitKind RepositoryGitKind { get; set; }
        public string UrlRelease { get; set; }
        public bool IsVersionXYZ { get; set; }

        private RepositoryGit(string name, string url, bool useLocalClonedFolder, ReleaseType releaseType, string companyName, string projectName, string localClonedFolderPath, bool useRepository, bool isVersionXYZ)
            : base(name, RepositoryType.Git, companyName, projectName, useRepository)
        {
            Url = url;
            UseLocalClonedFolder = useLocalClonedFolder;
            ReleaseType = releaseType;
            LocalClonedFolderPath = localClonedFolderPath;
            IsVersionXYZ = isVersionXYZ;
        }

        public static RepositoryGit CreateEmpty()
        {
            return new RepositoryGit(string.Empty, string.Empty, false, ReleaseType.Git, string.Empty, string.Empty, string.Empty, true, false);
        }

        public static RepositoryGit CreateWithReleaseTypeFolder(string name, string url, string releasesFolderRegexPattern, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null, bool useRepository = false, bool isVersionXYZ = false)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Folder, companyName, projectName, localClonedFolderPath, useRepository, isVersionXYZ)
            {
                ReleasesFolderRegexPattern = releasesFolderRegexPattern
            };
        }

        public static RepositoryGit CreateWithReleaseTypeGit(string name, RepositoryGitKind repositoryGitKind, string url, string gitRepositoryName, string owner, bool useLocalClonedFolder = false, string urlRelease = null, string companyName = null, string projectName = null, string localClonedFolderPath = null, bool useRepository = false, bool isVersionXYZ = false)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Git, companyName, projectName, localClonedFolderPath, useRepository, isVersionXYZ)
            {
                GitRepositoryName = gitRepositoryName,
                Owner = owner,
                UrlRelease = urlRelease,
                RepositoryGitKind = repositoryGitKind
            };
        }

        public static RepositoryGit CreateWithReleaseTypeTag(string name, string url, string releasesTagContentFolder, bool useLocalClonedFolder = false, string companyName = null, string projectName = null, string localClonedFolderPath = null, bool useRepository = false, bool isVersionXYZ = false)
        {
            return new RepositoryGit(name, url, useLocalClonedFolder, ReleaseType.Tag, companyName, projectName, localClonedFolderPath, useRepository, isVersionXYZ)
            {
                ReleasesTagContentFolder = releasesTagContentFolder
            };
        }

        public override async Task FillReleasesAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            try
            {
                UseDownloadedReleases = false;
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
            catch
            {
                FillReleasesFromDownloadedReleases();
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
            IReadOnlyList<Octokit.Release> repositoryReleases = await github.Repository.Release.GetAll(Owner, GitRepositoryName);

            releases.Clear();
            foreach (Octokit.Release release in repositoryReleases)
            {
                var assets = new List<ReleaseGitAsset>();
                string releaseArchive = $"{release.TagName}.zip";

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

                releases.Add(new ReleaseGit(release.TagName, Name, assets));
            }
        }

        private void FillReleasesFolder()
        {
            if (!Directory.Exists(LocalPath))
            {
                throw new DirectoryNotFoundException(LocalPath);
            }

            var releasesFolderRegex = new Regex(ReleasesFolderRegexPattern);
            IOrderedEnumerable<ReleaseFolder> releases = Directory
                .EnumerateDirectories(LocalPath)
                .Where(directoryPath => releasesFolderRegex.IsMatch(Path.GetFileName(directoryPath)))
                .Select(directoryPath => new ReleaseFolder(Path.GetFileName(directoryPath), directoryPath, Name))
                .OrderByDescending(r => r.Name);

            this.releases.Clear();
            this.releases.AddRange(releases);
        }

        private void FillReleasesTag()
        {
            using var gitRepo = new LibGit2Sharp.Repository(LocalPath);

            Remote remoteOrigin = gitRepo.Network.Remotes["origin"] ?? throw new NullReferenceException();
            static LibGit2Sharp.Credentials credHandler(string url, string usernameFromUrl, SupportedCredentialTypes types) => new DefaultCredentials();
            IEnumerable<LibGit2Sharp.Reference> remoteRefs = gitRepo.Network.ListReferences(remoteOrigin, credHandler) ?? throw new Exception();

            IEnumerable<ReleaseTag> releases = remoteRefs
                .Where(r => r.CanonicalName.StartsWith("refs/tags/") && !r.CanonicalName.EndsWith("^{}"))
                .Select(r => new ReleaseTag(r.CanonicalName["refs/tags/".Length..], this));

            this.releases.Clear();
            this.releases.AddRange(releases);
        }
    }
}
