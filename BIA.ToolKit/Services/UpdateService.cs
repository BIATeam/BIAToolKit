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
    using System.Threading.Tasks;
    using System.Windows;

    internal class UpdateService
    {
        private readonly string updateServer;
        private readonly string updateVersionFileName;
        private readonly string updateArchiveName;
        private readonly string updaterName;
        private readonly string applicationPath;
        private readonly string tempPath;

        public UpdateService()
        {
            updateServer = ConfigurationManager.AppSettings["UpdateServer"];
            updateVersionFileName = ConfigurationManager.AppSettings["UpdateVersionFileName"];
            updateArchiveName = ConfigurationManager.AppSettings["UpdateArchiveName"];
            updaterName = ConfigurationManager.AppSettings["UpdaterName"];

            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath();
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
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

        private async Task<Version> GetLatestVersionAsync()
        {
            string versionFilePath = Path.Combine(updateServer, updateVersionFileName);
            if (!File.Exists(versionFilePath))
                throw new FileNotFoundException($"Unable to find verison file {updateVersionFileName} in {updateServer}.");

            var versionFileContent = await File.ReadAllTextAsync(versionFilePath);
            if (!Version.TryParse(versionFileContent, out Version version))
                throw new Exception($"Version file content is not a valid version.");

            return version;
        }

        private async Task DownloadUpdateAsync()
        {
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var updaterSource = Path.Combine(updateServer, updaterName);
            var updaterTarget = Path.Combine(applicationPath, updaterName);
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException($"Unable to find {updaterName} in {updateServer}.");

            var updateArchiveSource = Path.Combine(updateServer, updateArchiveName);
            var updateArchiveTarget = Path.Combine(tempPath, updateArchiveName);
            if (!File.Exists(updateArchiveSource))
                throw new FileNotFoundException($"Unable to find {updateArchiveName} in {updateServer}.");

            await Task.Run(() =>
            {
                File.Copy(updaterSource, updaterTarget, true);
                File.Copy(updateArchiveSource, updateArchiveTarget, true);
            });

            Process.Start(updaterTarget, [$"\"{AppDomain.CurrentDomain.BaseDirectory}\"", $"\"{updateArchiveTarget}\""]);
        }
    }
}
