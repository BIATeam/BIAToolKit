namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using Newtonsoft.Json;

    public enum RepositoryType
    {
        Git,
        Folder
    }

    public abstract class Repository(string name, RepositoryType repositoryType, string companyName, string projectName, bool useRepository) : IRepository
    {
        public string Name { get; set; } = name;
        public RepositoryType RepositoryType { get; set; } = repositoryType;
        public string CompanyName { get; set; } = companyName;
        public string ProjectName { get; set; } = projectName;
        public bool UseRepository { get; set; } = useRepository;
        public bool UseDownloadedReleases { get; protected set; }
        public abstract string LocalPath { get; }

        protected List<Release> releases = [];
        public IReadOnlyList<Release> Releases => releases;

        public abstract Task FillReleasesAsync(CancellationToken ct = default);

        protected void EnsureReleasesDownloaded()
        {
            if (UseDownloadedReleases)
                return;

            releases.ForEach(r => r.EnsureDownloaded());
        }

        public void CleanReleases()
        {
            releases.ForEach(r => r.Clean());
        }

        /// <summary>
        /// Whether the toolkit owns <see cref="LocalPath"/> and is therefore allowed to
        /// delete it when the repository configuration is removed. Repositories that point
        /// at a user-managed folder override this to return <c>false</c> so their content
        /// is never wiped by the toolkit.
        /// </summary>
        protected virtual bool OwnsLocalPath => true;

        public void Clean()
        {
            CleanReleases();

            // Only delete the local working copy when the toolkit owns it. A user-provided
            // local cloned folder must never be wiped when its configuration is deleted.
            if (RepositoryType == RepositoryType.Git && OwnsLocalPath && Directory.Exists(LocalPath))
            {
                var dirInfo = new DirectoryInfo(LocalPath);
                foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }

                Directory.Delete(LocalPath, true);
            }
        }

        protected void FillReleasesFromDownloadedReleases()
        {
            string releasesPath = Path.Combine(AppSettings.AppFolderPath, Name);
            if (!Directory.Exists(releasesPath))
            {
                return;
            }

            UseDownloadedReleases = true;
            var directoryInfo = new DirectoryInfo(releasesPath);
            DirectoryInfo[] directories = directoryInfo.GetDirectories();
            IOrderedEnumerable<ReleaseFolder> releases = directories
                .Where(dir => !dir.Name.Equals("Repo"))
                .Select(dir => new ReleaseFolder(dir.Name, dir.FullName, Name))
                .OrderByDescending(r => r.Name);

            this.releases.Clear();
            this.releases.AddRange(releases);
        }
    }
}
