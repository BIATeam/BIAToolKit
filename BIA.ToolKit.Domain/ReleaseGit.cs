namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Octokit;

    public sealed class ReleaseGit(string name, string repositoryName, IReadOnlyList<ReleaseGitAsset> assets) : Release(name, repositoryName)
    {
        public override ReleaseType ReleaseType => ReleaseType.Git;
        public IReadOnlyList<ReleaseGitAsset> Assets { get; } = assets;

        public override async Task DownloadAsync()
        {
            InitDownload();

            foreach (ReleaseGitAsset asset in Assets)
            {
                string fileName = string.IsNullOrWhiteSpace(asset.Name)
                    ? Path.GetFileName(asset.DownloadUrl)
                    : asset.Name;

                string targetPath = Path.Combine(LocalPath, fileName);

                using HttpResponseMessage resp = await Common.Helpers.HttpHelper.HttpClient.GetAsync(asset.DownloadUrl);
                resp.EnsureSuccessStatusCode();

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                await using Stream input = await resp.Content.ReadAsStreamAsync();
                await using FileStream output = File.Create(targetPath);

                await input.CopyToAsync(output);

                if (asset.HasSize && asset.Size != output.Length)
                {
                    throw new Exception($"Downloaded file {output.Length} has not the same size as origin asset");
                }
            }

            IsDownloaded = true;
        }
    }
}
