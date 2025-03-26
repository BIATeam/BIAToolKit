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
        private Version currentVersion;
        public Version NewVersion
        {
            get; private set;
        }
        Release LastRelease;

        public UpdateService(UIEventBroker eventBroker, IConsoleWriter consoleWriter)
        {
            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath() + "\\BIAToolkit";
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
        }

        public void SetAppVersion(Version version)
        {
            this.currentVersion = version;
        }

        public async Task<Boolean> CheckForUpdatesAsync(bool autoUpdate)
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("BIAToolKit"));
                string owner = "BIATeam";
                string repoName = "BIAToolKit";

                var releases = await github.Repository.Release.GetAll(owner, repoName);
                if (releases.Count > 0)
                {
                    foreach(Release release in releases)
                    {
                        var lastRelease = releases[0];
                        if (lastRelease.TagName.StartsWith("V"))
                        {
                             Version.TryParse(lastRelease.TagName.Substring(1), out Version updateVersion);
                            if (updateVersion > this.currentVersion)
                            {
                                consoleWriter.AddMessageLine($"A new version of BIAToolKit is available: {lastRelease.TagName}", "Yellow");
                                NewVersion = updateVersion;
                                LastRelease = lastRelease;
                                eventBroker.NotifyNewVersionAvailable();

                                if (autoUpdate && !Debugger.IsAttached)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
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
            return false;
        }

        public async Task DownloadUpdateAsync()
        {

            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var updaterSourceDir = Path.Combine(tempPath, "BIAToolKitUpdater");
            var updaterSource = Path.Combine(updaterSourceDir, UpdaterName);
            var updaterZipName = "BIAToolKitUpdater.zip";
            var updaterFilePath = Path.Combine(tempPath, updaterZipName);

            await DownloadFromRelease(updaterSourceDir, updaterZipName, updaterFilePath);
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException($"Unable to find {updaterZipName} in last release.");

            var toolkitSourceDir = Path.Combine(tempPath, "BIAToolKit");
            var toolkitZipName = "BIAToolKit.zip";
            var toolkitFilePath = Path.Combine(tempPath, toolkitZipName);
            await DownloadFromRelease(toolkitSourceDir, toolkitZipName, toolkitFilePath);

            if (!File.Exists(toolkitFilePath))
                throw new FileNotFoundException($"Unable to find {toolkitZipName} in last release.");

            var updaterTarget = Path.Combine(applicationPath, UpdaterName);

            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(updaterSourceDir))
                    File.Copy(file, Path.Combine(applicationPath, Path.GetFileName(file)),true);
            });

            Process.Start(updaterTarget, [$"\"{AppDomain.CurrentDomain.BaseDirectory}\"", $"\"{toolkitFilePath}\""]);
        }

        private async Task DownloadFromRelease(string sourceDir, string zipName,string filePath)
        {
            consoleWriter.AddMessageLine($"Start download from release: {zipName}", "Pink");
            var asset = LastRelease.Assets.FirstOrDefault(a => a.Name == zipName);
            if (asset != null)
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler
                {
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                };
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    var response = await httpClient.GetAsync(asset.BrowserDownloadUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }

                        using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        consoleWriter.AddMessageLine($"File downloaded: {zipName}", "Green");

                        if (Directory.Exists(sourceDir))
                        {
                            Directory.Delete(sourceDir,true);
                        }
                        ZipFile.ExtractToDirectory(filePath, sourceDir);

                        consoleWriter.AddMessageLine($"File extarcted: {zipName}", "Green");
                    }
                    else
                    {
                        consoleWriter.AddMessageLine("Fail to download updater.", "Red");
                    }
                }
            }
        }
    }
}
