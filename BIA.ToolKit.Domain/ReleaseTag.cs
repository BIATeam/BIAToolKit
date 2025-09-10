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
                LibGit2Sharp.Repository.Clone(repository.LocalPath, LocalPath, new CloneOptions
                {
                    Checkout = true,
                    BranchName = name
                });
            });

            IsDownloaded = true;
        }
    }
}
