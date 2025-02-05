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
                string latestVersion = await GetLatestVersionAsync();
                string currentVersion = GetCurrentVersion();

                if (latestVersion != currentVersion)
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"Une nouvelle version ({latestVersion}) est disponible.\nVoulez-vous l'installer maintenant ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await DownloadUpdateAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de mise à jour : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GetLatestVersionAsync()
        {
            string versionFilePath = Path.Combine(updateServer, updateVersionFileName);
            if (!File.Exists(versionFilePath))
                throw new FileNotFoundException("Fichier de version introuvable.");

            return await File.ReadAllTextAsync(versionFilePath);
        }

        private string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        }

        private async Task DownloadUpdateAsync()
        {
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var updaterSource = Path.Combine(updateServer, updaterName);
            var updaterTarget = Path.Combine(applicationPath, updaterName);
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException("Le programme de mise à jour est manquant.");

            var updateArchiveSource = Path.Combine(updateServer, updateArchiveName);
            var updateArchiveTarget = Path.Combine(tempPath, updateArchiveName);

            if (!File.Exists(updateArchiveSource))
                throw new FileNotFoundException("Fichier de mise à jour introuvable.");

            await Task.Run(() =>
            {
                File.Copy(updaterSource, updaterTarget, true);
                File.Copy(updateArchiveSource, updateArchiveTarget, true);
            });

            Process.Start(updaterTarget, [$"\"{AppDomain.CurrentDomain.BaseDirectory}\"", $"\"{updateArchiveTarget}\""]);
        }
    }
}
