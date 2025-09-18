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
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;

    public class UpdateService
    {
        private const string UpdateArchiveName = "BIAToolKit.zip";
        private const string UpdaterArchiveName = "BIAToolKitUpdater.zip";
        private const string UpdaterName = "BIA.ToolKit.Updater.exe";

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
            this.eventBroker = eventBroker;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
        }

        public void SetAppVersion(Version version)
        {
            this.currentVersion = version;
            BiaToolkitVersion.ApplicationVersion = version.ToString();
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

                var updateArchivePath = Path.Combine(lastRelease.LocalPath, UpdateArchiveName);
                if (!File.Exists(updateArchivePath))
                    throw new FileNotFoundException(updateArchivePath);

                var updaterArchivePath = Path.Combine(lastRelease.LocalPath, UpdaterArchiveName);
                if (!File.Exists(updaterArchivePath))
                    throw new FileNotFoundException(updaterArchivePath);

                var updaterFolderPath = Path.Combine(lastRelease.LocalPath, Path.GetFileNameWithoutExtension(UpdaterArchiveName));
                ZipFile.ExtractToDirectory(updaterArchivePath, updaterFolderPath);
                consoleWriter.AddMessageLine($"{UpdaterArchiveName} extracted", "Gray");

                consoleWriter.AddMessageLine($"Launching update...", "yellow");
                Process.Start(
                    Path.Combine(updaterFolderPath, UpdaterName),
                    [
                        $"\"{AppDomain.CurrentDomain.BaseDirectory}\"",
                    $"\"{updateArchivePath}\""
                    ]
                );
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Download update failure : {ex.Message}", "red");
            }
        }
    }
}
