namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
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

    public class UpdateService(UIEventBroker eventBroker, IConsoleWriter consoleWriter, SettingsService settingsService)
    {
        private const string UpdateArchiveName = "BIAToolKit.zip";
        private const string UpdaterArchiveName = "BIAToolKitUpdater.zip";
        private const string UpdaterName = "BIA.ToolKit.Updater.exe";

        private readonly UIEventBroker eventBroker = eventBroker;
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly SettingsService settingsService = settingsService;
        private Version currentVersion;
        private Release lastRelease;
        public Version NewVersion
        {
            get; private set;
        }
        public bool HasNewVersion { get; private set; }

        public void SetAppVersion(Version version)
        {
            currentVersion = version;
            BiaToolkitVersion.ApplicationVersion = version.ToString();
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                IRepository toolkitRepository = settingsService.Settings.ToolkitRepository;
                await toolkitRepository.FillReleasesAsync();

                if (toolkitRepository.Releases.Count == 0)
                {
                    HasNewVersion = false;
                    return;
                }

                var releases = new List<(Release Release, Version Version)>();
                foreach (Release repositoryRelease in toolkitRepository.Releases)
                {
                    if (repositoryRelease.Name.StartsWith('V') && Version.TryParse(repositoryRelease.Name[1..], out Version version))
                    {
                        releases.Add((repositoryRelease, version));
                    }
                    else if (Version.TryParse(repositoryRelease.Name, out version))
                    {
                        releases.Add((repositoryRelease, version));
                    }
                }

                if (releases.Count == 0)
                {
                    HasNewVersion = false;
                    consoleWriter.AddMessageLine($"No valid release found in repository {toolkitRepository.Name}.", "Yellow");
                    return;
                }

                (Release Release, Version Version) lastRelease = releases.OrderByDescending(r => r.Version).First();
                if (lastRelease.Version > currentVersion)
                {
                    consoleWriter.AddMessageLine($"A new version of BIAToolKit is available: {lastRelease.Release.Name}", "Yellow");
                    HasNewVersion = true;
                    NewVersion = lastRelease.Version;
                    this.lastRelease = lastRelease.Release;
                    eventBroker.NotifyNewVersionAvailable();
                    return;
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

                string updateArchivePath = Path.Combine(lastRelease.LocalPath, UpdateArchiveName);
                if (!File.Exists(updateArchivePath))
                    throw new FileNotFoundException(updateArchivePath);

                string updaterArchivePath = Path.Combine(lastRelease.LocalPath, UpdaterArchiveName);
                if (!File.Exists(updaterArchivePath))
                    throw new FileNotFoundException(updaterArchivePath);

                string updaterFolderPath = Path.Combine(lastRelease.LocalPath, Path.GetFileNameWithoutExtension(UpdaterArchiveName));
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
