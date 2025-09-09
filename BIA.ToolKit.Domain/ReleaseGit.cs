namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Octokit;

    public class ReleaseGit(string name, string url, string repositoryName, IReadOnlyList<ReleaseAsset> assets) : Release(name, url, repositoryName)
    {
        public override ReleaseType ReleaseType => ReleaseType.Git;
        public IReadOnlyList<ReleaseAsset> Assets { get; } = assets;

        public override async Task DownloadAsync()
        {
            InitDownload();

            var httpClient = new HttpClient(
                new HttpClientHandler
                {
                    UseProxy = true,
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials
                },
                disposeHandler: true);

            foreach (var asset in Assets)
            {
                var fileName = string.IsNullOrWhiteSpace(asset.Name)
                    ? Path.GetFileName(asset.BrowserDownloadUrl)
                    : asset.Name;

                var targetPath = Path.Combine(LocalPath, fileName);

                using var resp = await httpClient.GetAsync(asset.BrowserDownloadUrl);
                resp.EnsureSuccessStatusCode();

                await using var input = await resp.Content.ReadAsStreamAsync();
                await using var output = File.Create(targetPath);

                await input.CopyToAsync(output);

                if (output.Length != asset.Size)
                {
                    throw new Exception($"Downloaded file {output.Length} has not the same size as origin asset");
                }
            }

            IsDownloaded = true;
        }
    }
}
