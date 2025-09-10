namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Octokit;

    public class ReleaseGit(string name, string repositoryName, IReadOnlyList<ReleaseGitAsset> assets) : Release(name, repositoryName)
    {
        public override ReleaseType ReleaseType => ReleaseType.Git;
        public IReadOnlyList<ReleaseGitAsset> Assets { get; } = assets;

        public override async Task DownloadAsync()
        {
            InitDownload();

            var httpClient = new HttpClient(
                new HttpClientHandler
                {
                    UseProxy = true,
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                },
                disposeHandler: true);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BiaToolKit");

            foreach (var asset in Assets)
            {
                var fileName = string.IsNullOrWhiteSpace(asset.Name)
                    ? Path.GetFileName(asset.DownloadUrl)
                    : asset.Name;

                var targetPath = Path.Combine(LocalPath, fileName);

                using var resp = await httpClient.GetAsync(asset.DownloadUrl);
                resp.EnsureSuccessStatusCode();

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                await using var input = await resp.Content.ReadAsStreamAsync();
                await using var output = File.Create(targetPath);

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
