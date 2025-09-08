namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using BIA.ToolKit.Application.Helper;
    using Octokit;

    public class UpdateService
    {
        private const string UpdaterName = "BIA.ToolKit.Updater.exe";

        private readonly string applicationPath;
        private readonly string tempPath;
        private readonly UIEventBroker eventBroker;
        private readonly IConsoleWriter consoleWriter;
        private readonly LocalReleaseRepositoryService localReleaseRepositoryService;
        private Version currentVersion;
        private Release lastRelease;
        public Version NewVersion
        {
            get; private set;
        }
        public bool HasNewVersion { get; private set; }

        public UpdateService(UIEventBroker eventBroker, IConsoleWriter consoleWriter, LocalReleaseRepositoryService localReleaseRepositoryService)
        {
            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath() + "\\BIAToolkit";
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
            this.localReleaseRepositoryService = localReleaseRepositoryService;
        }

        public void SetAppVersion(Version version)
        {
            this.currentVersion = version;
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("BIAToolKit"));
                string owner = "BIATeam";
                string repoName = "BIAToolKit";

                var releases = await github.Repository.Release.GetAll(owner, repoName);
                if (releases.Count > 0)
                {
                    foreach (Release release in releases)
                    {
                        var lastRelease = releases[0];
                        if (lastRelease.TagName.StartsWith('V') && Version.TryParse(lastRelease.TagName[1..], out Version updateVersion))
                        {
                            if (updateVersion > this.currentVersion)
                            {
                                consoleWriter.AddMessageLine($"A new version of BIAToolKit is available: {lastRelease.TagName}", "Yellow");
                                HasNewVersion = true;
                                NewVersion = updateVersion;
                                this.lastRelease = lastRelease;
                                eventBroker.NotifyNewVersionAvailable();
                                return;
                            }
                        }
                    }
                    HasNewVersion = false;
                }
                else
                {
                    consoleWriter.AddMessageLine($"No release found on GitHub.", "Red");
                }
                consoleWriter.AddMessageLine($"You have the last version of BIAToolKit.", "Yellow");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Check For Updates failure : {ex.Message}", "Red");
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                if(localReleaseRepositoryService.UseLocalReleaseRepository)
                {
                    consoleWriter.AddMessageLine("Using local release repository", "gray");
                }

                await DownloadLastRelease(ReleaseKind.Updater);
                await DownloadLastRelease(ReleaseKind.Toolkit);

                Process.Start(
                    Path.Combine(tempPath, "BIAToolKitUpdater", UpdaterName),
                    [
                        $"\"{AppDomain.CurrentDomain.BaseDirectory}\"",
                    $"\"{Path.Combine(tempPath, "BIAToolKit.zip")}\""
                    ]
                );
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Download update failure : {ex.Message}", "red");
            }
        }

        private enum ReleaseKind
        {
            Updater,
            Toolkit
        }

        private async Task DownloadLastRelease(ReleaseKind releaseKind)
        {
            consoleWriter.AddMessageLine($"Start download {releaseKind} from release {lastRelease.TagName}", "Pink");

            var zipName = string.Empty;
            var destDir = string.Empty;
            switch (releaseKind)
            {
                case ReleaseKind.Updater:
                    destDir = Path.Combine(tempPath, "BIAToolKitUpdater");
                    zipName = "BIAToolKitUpdater.zip";
                    break;
                case ReleaseKind.Toolkit:
                    destDir = Path.Combine(tempPath, "BIAToolKit");
                    zipName = "BIAToolKit.zip";
                    break;
                default:
                    throw new NotImplementedException($"Unknown release kind {releaseKind}");
            }
            var destZipPath = Path.Combine(tempPath, zipName);

            if (localReleaseRepositoryService.UseLocalReleaseRepository)
            {
                var localReleaseZipPath = releaseKind switch
                {
                    ReleaseKind.Updater => localReleaseRepositoryService.GetBiaToolkitUpdaterReleaseArchivePath(lastRelease.TagName),
                    ReleaseKind.Toolkit => localReleaseRepositoryService.GetBiaToolkitReleaseArchivePath(lastRelease.TagName),
                    _ => throw new NotImplementedException($"Unknown release kind {releaseKind}"),
                };

                if (!File.Exists(localReleaseZipPath))
                {
                    throw new FileNotFoundException($"File {localReleaseZipPath} not found");
                }

                if (File.Exists(destZipPath))
                {
                    File.Delete(destZipPath);
                }

                File.Copy(localReleaseZipPath, destZipPath, true);

                if (new FileInfo(destZipPath).Length != new FileInfo(localReleaseZipPath).Length)
                {
                    throw new Exception($"Downloaded file {destZipPath} has not the same size as origin file");
                }
            }
            else
            {
                var asset = lastRelease.Assets.FirstOrDefault(a => a.Name == zipName) 
                    ?? throw new FileNotFoundException($"Asset {zipName} not found for release {lastRelease.TagName}");
                
                HttpClientHandler httpClientHandler = new()
                {
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                };

                using var httpClient = new HttpClient(httpClientHandler);
                var response = await httpClient.GetAsync(asset.BrowserDownloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Fail to download {zipName}: {response.ReasonPhrase}");
                }

                if (File.Exists(destZipPath))
                {
                    File.Delete(destZipPath);
                }

                using (var fileStream = new FileStream(destZipPath, System.IO.FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                if (new FileInfo(destZipPath).Length != asset.Size)
                {
                    throw new Exception($"Downloaded file {destZipPath} has not the same size as origin asset");
                }
            }

            consoleWriter.AddMessageLine($"File {zipName} downloaded successfuly", "Green");

            if (releaseKind == ReleaseKind.Updater)
            {
                if (Directory.Exists(destDir))
                {
                    Directory.Delete(destDir, true);
                }
                ZipFile.ExtractToDirectory(destZipPath, destDir);
                consoleWriter.AddMessageLine($"{zipName} extracted", "Green");
            }
        }
    }
}
