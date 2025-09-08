namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using System.Threading.Tasks;
    using LibGit2Sharp;
    using System.IO;
    using BIA.ToolKit.Domain.Settings;
    using System.Net;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Linq;
    using System;
    using System.Collections.Generic;

    public class RepositoryService
    {
        private IConsoleWriter outPut;
        private GitService gitService;
        private readonly LocalReleaseRepositoryService localReleaseRepositoryService;
        private List<VersionDownload> versionDownloads = new();

        public RepositoryService(IConsoleWriter outPut, GitService gitService, LocalReleaseRepositoryService localReleaseRepositoryService)
        {
            this.outPut = outPut;
            this.gitService = gitService;
            this.localReleaseRepositoryService = localReleaseRepositoryService;
        }

        public bool CheckRepoFolder(IRepositorySettings repository, bool inSync)
        {
            if (string.IsNullOrEmpty(repository.RootFolderPath))
            {
                outPut.AddMessageLine("Error on " + repository.Name + " local folder :\r\nThe path is not define.\r\n Correct it in config tab.", "Red");
            }
            if (!inSync && !Directory.Exists(repository.RootFolderPath))
            {
                if (repository.UseLocalFolder)
                {
                    outPut.AddMessageLine("Error on " + repository.Name + " local folder :\r\nThe path " + repository.RootFolderPath + " do not exist.\r\n Correct it in config tab.", "Red");
                }
                else
                {
                    outPut.AddMessageLine("Error on " + repository.Name + " :\r\nThe path " + repository.RootFolderPath + " do not exist.\r\n Please synchronize the repository.", "Red");
                }
                return false;
            }
            return true;
        }

        public async Task CleanRepository(IRepositorySettings repository)
        {
            try
            {
                outPut.AddMessageLine($"Cleaning repository {repository.RootFolderPath}...", "pink");
                await Task.Run(() =>
                {
                    if (Directory.Exists(repository.RootFolderPath))
                    {
                        var dirInfo = new DirectoryInfo(repository.RootFolderPath);

                        foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                        {
                            file.Attributes = FileAttributes.Normal;
                        }

                        Directory.Delete(repository.RootFolderPath, true);
                    }
                });

                outPut.AddMessageLine("Repository cleaned.", "green");
            }
            catch (Exception ex)
            {
                outPut.AddMessageLine($"Error when cleaning repository : {ex.Message}", "red");
            }
        }

        public async Task CleanReleases(IRepositorySettings repository)
        {
            if (repository.Versioning == VersioningType.Release)
            {
                try
                {
                    var releasesFolderPath = Path.Combine(AppSettings.AppFolderPath, repository.Name);
                    outPut.AddMessageLine($"Cleaning releases in {releasesFolderPath}...", "pink");
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(releasesFolderPath))
                        {
                            var dirInfo = new DirectoryInfo(releasesFolderPath);

                            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                            {
                                file.Attributes = FileAttributes.Normal;
                                File.Delete(file.FullName);
                            }

                            foreach (var innerDirectory in dirInfo.GetDirectories("*"))
                            {
                                if (innerDirectory.Name.Equals(Path.GetFileName(repository.RootFolderPath)))
                                    continue;

                                Directory.Delete(innerDirectory.FullName, true);
                            }
                        }
                    });

                    outPut.AddMessageLine("Releases cleaned.", "green");
                }
                catch (Exception ex)
                {
                    outPut.AddMessageLine($"Error when cleaning releases : {ex.Message}", "red");
                }
            }
        }

        public async Task<string> PrepareVersionFolder(IRepositorySettings repository, string version)
        {
            try
            {
                if (repository.Versioning == VersioningType.Folder)
                {
                    return repository.RootFolderPath + "\\" + version;
                }
                else if (repository.Versioning == VersioningType.Release)
                {
                    using (var repo = new Repository(repository.RootFolderPath))
                    {
                        foreach (var tag in repo.Tags)
                        {
                            if (tag.FriendlyName == version)
                            {
                                var zipPath = AppSettings.AppFolderPath + "\\" + repository.Name + "\\" + tag.FriendlyName + ".zip";
                                string biaTemplatePathVersionUnzip = AppSettings.AppFolderPath + "\\" + repository.Name + "\\" + tag.FriendlyName;
                                Directory.CreateDirectory(AppSettings.AppFolderPath + "\\" + repository.Name + "\\");

                                if (!Directory.Exists(biaTemplatePathVersionUnzip))
                                {
                                    var versionDownload = versionDownloads.Find(x => x.Version == version);
                                    if (versionDownload == null)
                                    {
                                        versionDownload = new VersionDownload { Version = version };
                                        versionDownloads.Add(versionDownload);
                                    }

                                    if (!versionDownload.IsDownloading)
                                    {
                                        versionDownload.IsDownloading = true;
                                        try
                                        {
                                            outPut.AddMessageLine($"Downloading release {tag.FriendlyName}...", "Pink");
                                            if (localReleaseRepositoryService.UseLocalReleaseRepository)
                                            {
                                                outPut.AddMessageLine("Using local release repository", "gray");
                                                var localReleaseZipPath = localReleaseRepositoryService.GetBiaTemplateReleaseArchivePath(tag.FriendlyName);
                                                File.Copy(localReleaseZipPath, zipPath, true);
                                            }
                                            else
                                            {
                                                var zipUrl = repository.UrlRelease + tag.CanonicalName + ".zip";
                                                if (File.Exists(zipPath))
                                                {
                                                    File.Delete(zipPath);
                                                }
                                                HttpClientHandler httpClientHandler = new()
                                                {
                                                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                                                };
                                                using var httpClient = new HttpClient(httpClientHandler);
                                                var response = await httpClient.GetAsync(zipUrl);
                                                using var fs = new FileStream(zipPath, FileMode.CreateNew);
                                                await response.Content.CopyToAsync(fs);
                                            }

                                            if (!File.Exists(zipPath))
                                            {
                                                outPut.AddMessageLine($"Release {version} not downloaded", "Red");
                                                break;
                                            }

                                            outPut.AddMessageLine($"Release {version} downloaded", "green");

                                            await UnzipIfNotExist(zipPath, biaTemplatePathVersionUnzip, version);
                                        }
                                        catch (Exception e)
                                        {
                                            outPut.AddMessageLine("Cannot download release: " + version + $" ({e.Message})", "Red");
                                        }

                                        versionDownload.IsDownloading = false;
                                    }
                                    else
                                    {
                                        while (versionDownload.IsDownloading)
                                        {
                                            await Task.Delay(TimeSpan.FromSeconds(1));
                                        }
                                    }
                                }

                                var dirContent = Directory.GetDirectories(biaTemplatePathVersionUnzip, "*.*", SearchOption.TopDirectoryOnly);
                                if (dirContent.Length == 1)
                                {
                                    return dirContent[0];
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                else
                {
                    await this.gitService.CheckoutTag(repository, version);
                    return repository.RootFolderPath;
                }
            }
            catch (Exception ex)
            {
                outPut.AddMessageLine($"Error: {ex.Message}", "red");
                return string.Empty;
            }
        }

        private async Task UnzipIfNotExist(string zipPath, string biaTemplatePathVersionUnzip, string version)
        {
            if (!Directory.Exists(biaTemplatePathVersionUnzip))
            {
                outPut.AddMessageLine($"Unzipping {version}...", "pink");
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, biaTemplatePathVersionUnzip);
                    FileTransform.FolderUnix2Dos(biaTemplatePathVersionUnzip);
                    File.Delete(zipPath);
                });
                outPut.AddMessageLine($"{version} unzipped", "green");
            }
        }

        private static bool IsTextFileEmpty(string fileName)
        {
            var info = new FileInfo(fileName);
            if (info.Length == 0)
                return true;

            // only if your use case can involve files with 1 or a few bytes of content.
            if (info.Length < 6)
            {
                var content = File.ReadAllText(fileName);
                return content.Length == 0;
            }
            return false;
        }

        private class VersionDownload
        {
            public string Version { get; set; }
            public bool IsDownloading { get; set; }
        }
    }
}
