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
    using BIA.ToolKit.Domain;

    public class UpdateService
    {
        private const string UpdaterName = "BIA.ToolKit.Updater.exe";

        private readonly string applicationPath;
        private readonly string tempPath;
        private readonly UIEventBroker eventBroker;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private Version currentVersion;
        private Release lastRelease;
        public Version NewVersion
        {
            get; private set;
        }
        public bool HasNewVersion { get; private set; }

        public UpdateService(UIEventBroker eventBroker, IConsoleWriter consoleWriter, SettingsService settingsService)
        {
            applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            tempPath = Path.GetTempPath() + "\\BIAToolkit";
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
        }

        public void SetAppVersion(Version version)
        {
            this.currentVersion = version;
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var toolkitRepository = settingsService.Settings.ToolkitRepository;
                await toolkitRepository.FillReleasesAsync();

                if (toolkitRepository.Releases.Count == 0)
                {
                    HasNewVersion = false;
                    return;
                }

                var lastRelease = toolkitRepository.Releases.FirstOrDefault();
                if (lastRelease.Name.StartsWith('V') && Version.TryParse(lastRelease.Name[1..], out Version newVersion))
                {
                    if (newVersion > currentVersion)
                    {
                        consoleWriter.AddMessageLine($"A new version of BIAToolKit is available: {lastRelease.Name}", "Yellow");
                        HasNewVersion = true;
                        NewVersion = newVersion;
                        this.lastRelease = lastRelease;
                        eventBroker.NotifyNewVersionAvailable();
                        return;
                    }
                }

                HasNewVersion = false;
                consoleWriter.AddMessageLine($"You have the last version of BIAToolKit.", "Yellow");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Check for updates failure : {ex.Message}", "Red");
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                consoleWriter.AddMessageLine($"Start download assets of release {lastRelease.Name}...", "Pink");
                await lastRelease.DownloadAsync();
                consoleWriter.AddMessageLine($"Assets downloaded successfully", "green");

                Directory.CreateDirectory(tempPath);
                await CopyRelease(ReleaseKind.Updater);
                await CopyRelease(ReleaseKind.Toolkit);

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

        private async Task CopyRelease(ReleaseKind releaseKind)
        {
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

            var assetFile = Path.Combine(lastRelease.LocalPath, zipName);
            if(!File.Exists(assetFile))
            {
                throw new FileNotFoundException(assetFile);
            }

            File.Copy(assetFile, destZipPath, true);

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
