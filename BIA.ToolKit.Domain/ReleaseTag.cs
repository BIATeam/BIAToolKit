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

    public class ReleaseTag(string name, IRepository repository) : Release(name, repository.Name)
    {
        public override ReleaseType ReleaseType => ReleaseType.Tag;

        public override async Task DownloadAsync()
        {
            InitDownload();

            await Task.Run(() =>
            {
                LibGit2Sharp.Repository.Clone(repository.LocalPath, LocalPath);
                using var gitRepository = new LibGit2Sharp.Repository(repository.LocalPath);
                var tag = gitRepository.Tags.FirstOrDefault(t => t.FriendlyName == Name) 
                    ?? throw new NotFoundException($"Tag {Name} not found in repository {repository.Name}");
                var commit = tag.Target.Peel<Commit>()
                    ?? throw new NotFoundException($"Tag {Name} does not resolve to a commit.");
                Commands.Checkout(gitRepository, commit);
            });

            IsDownloaded = true;
        }
    }
}
