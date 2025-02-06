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
    using Newtonsoft.Json;

    internal class UpdateService
    {
        private readonly string updaterName;
        private readonly string applicationPath;
        private readonly string tempPath;

        private PackageConfig packageConfig;

        public UpdateService()
        {
            updaterName = ConfigurationManager.AppSettings["UpdaterName"];
            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath();
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                ParsePackageFile();

                var updateVersion = await GetLatestVersionAsync();
                if (updateVersion > Assembly.GetExecutingAssembly().GetName().Version)
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"A new version ({updateVersion}) is available.\nInstall now ?",
                        "Update available",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await DownloadUpdateAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failure : {ex.Message}", "Update failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParsePackageFile()
        {
            string packageJsonPath = Path.Combine(applicationPath, "package.json");

            if (!File.Exists(packageJsonPath))
            {
                throw new FileNotFoundException("package.json not found.");
            }

            string jsonContent = File.ReadAllText(packageJsonPath);
            packageConfig = JsonConvert.DeserializeObject<PackageConfig>(jsonContent);

            if (packageConfig == null || string.IsNullOrEmpty(packageConfig.DistributionServer)
                || string.IsNullOrEmpty(packageConfig.PackageVersionFileName)
                || string.IsNullOrEmpty(packageConfig.PackageArchiveName))
            {
                throw new InvalidOperationException("Missing required values in package.json.");
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

            var updaterSource = Path.Combine(packageConfig.DistributionServer, updaterName);
            var updaterTarget = Path.Combine(applicationPath, updaterName);
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException($"Unable to find {updaterName} in {packageConfig.DistributionServer}.");

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

        private class PackageConfig
        {
            public string DistributionServer { get; set; }
            public string PackageVersionFileName { get; set; }
            public string PackageArchiveName { get; set; }
        }
    }
}
