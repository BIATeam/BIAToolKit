namespace BIA.ToolKit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows;
    using BIA.ToolKit.Package;
    using Newtonsoft.Json;

    public class UpdateService
    {
        private const string UpdaterName = "BIA.ToolKit.Updater.exe";

        private readonly string applicationPath;
        private readonly string tempPath;
        private readonly UIEventBroker eventBroker;

        private PackageConfig packageConfig;
        private Version NewVersion;

        public UpdateService(UIEventBroker eventBroker)
        {
            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath();
            this.eventBroker = eventBroker;
        }

        public async Task CheckForUpdatesAsync(bool autoUpdate)
        {
            try
            {
                packageConfig = Helper.GetPackageConfig(applicationPath);

                var updateVersion = await GetLatestVersionAsync();
                if (updateVersion > Assembly.GetExecutingAssembly().GetName().Version)
                {
                    NewVersion = updateVersion;
                    eventBroker.NotifyNewVersionAvailable();

                    if (autoUpdate && !Debugger.IsAttached)
                    {
                        await InitUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failure : {ex.Message}", "Update failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task InitUpdate()
        {
            var result = MessageBox.Show(
                                    $"A new version ({NewVersion}) is available.\nInstall now ?",
                                    "Update available",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                await DownloadUpdateAsync();
            }
        }

        private async Task<Version> GetLatestVersionAsync()
        {
            string versionFilePath = Path.Combine(packageConfig.DistributionServer, packageConfig.PackageVersionFileName);
            if (!File.Exists(versionFilePath))
                throw new FileNotFoundException($"Unable to find verison file {packageConfig.PackageVersionFileName} in {packageConfig.DistributionServer}.");

            var versionFileContent = await File.ReadAllTextAsync(versionFilePath);
            if (!Version.TryParse(versionFileContent, out Version version))
                throw new Exception($"Version file content is not a valid version.");

            return version;
        }

        private async Task DownloadUpdateAsync()
        {
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var updaterSource = Path.Combine(packageConfig.DistributionServer, UpdaterName);
            var updaterTarget = Path.Combine(applicationPath, UpdaterName);
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException($"Unable to find {UpdaterName} in {packageConfig.DistributionServer}.");

            var updateArchiveSource = Path.Combine(packageConfig.DistributionServer, packageConfig.PackageArchiveName);
            var updateArchiveTarget = Path.Combine(tempPath, packageConfig.PackageArchiveName);
            if (!File.Exists(updateArchiveSource))
                throw new FileNotFoundException($"Unable to find {packageConfig.PackageArchiveName} in {packageConfig.DistributionServer}.");

            await Task.Run(() =>
            {
                File.Copy(updaterSource, updaterTarget, true);
                File.Copy(updateArchiveSource, updateArchiveTarget, true);
            });

            Process.Start(updaterTarget, [$"\"{AppDomain.CurrentDomain.BaseDirectory}\"", $"\"{updateArchiveTarget}\""]);
        }
    }
}
