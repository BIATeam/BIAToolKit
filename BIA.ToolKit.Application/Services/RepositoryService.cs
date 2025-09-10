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
    using BIA.ToolKit.Domain;

    public class RepositoryService
    {
        private readonly IConsoleWriter outPut;

        public RepositoryService(IConsoleWriter outPut)
        {
            this.outPut = outPut;
        }

        public bool CheckRepoFolder(Domain.IRepository repository, bool inSync)
        {
            if (string.IsNullOrEmpty(repository.LocalPath))
            {
                outPut.AddMessageLine("Error on " + repository.Name + " local folder :\r\nThe path is not define.\r\n Correct it in config tab.", "Red");
            }
            if (repository is RepositoryGit repositoryGit && !inSync && !Directory.Exists(repository.LocalPath))
            {
                if (repositoryGit.UseLocalClonedFolder)
                {
                    outPut.AddMessageLine("Error on " + repository.Name + " local folder :\r\nThe path " + repository.LocalPath + " do not exist.\r\n Correct it in config tab.", "Red");
                }
                else
                {
                    outPut.AddMessageLine("Error on " + repository.Name + " :\r\nThe path " + repository.LocalPath + " do not exist.\r\n Please synchronize the repository.", "Red");
                }
                return false;
            }
            return true;
        }

        public async Task CleanRepository(Domain.IRepository repository)
        {
            try
            {
                outPut.AddMessageLine($"Cleaning repository {repository.LocalPath}...", "pink");
                await Task.Run(() =>
                {
                    if (Directory.Exists(repository.LocalPath))
                    {
                        var dirInfo = new DirectoryInfo(repository.LocalPath);

                        foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                        {
                            file.Attributes = FileAttributes.Normal;
                        }

                        Directory.Delete(repository.LocalPath, true);
                    }
                });

                outPut.AddMessageLine("Repository cleaned.", "green");
            }
            catch (Exception ex)
            {
                outPut.AddMessageLine($"Error when cleaning repository : {ex.Message}", "red");
            }
        }

        public async Task CleanReleases(Domain.Repository repository)
        {
            try
            {
                outPut.AddMessageLine($"Cleaning releases of repository {repository.Name}...", "pink");

                await Task.Run(() =>
                {
                    foreach (var release in repository.Releases.Where(r => r.IsDownloaded))
                    {
                        release.Clean();
                        outPut.AddMessageLine($"Release {release.Name} cleaned", "green");
                    }
                });

                outPut.AddMessageLine($"Releases of repository {repository.Name} cleaned", "green");
            }
            catch (Exception ex)
            {
                outPut.AddMessageLine($"Error when cleaning releases : {ex.Message}", "red");
            }
        }

        public async Task<string> PrepareVersionFolder(Domain.IRepository repository, string version)
        {
            try
            {
                var release = repository.Releases.FirstOrDefault(r => r.Name == version)
                        ?? throw new Exception($"Release {version} not found for repository {repository.Name}");

                if (!release.IsDownloaded)
                {
                    outPut.AddMessageLine($"Downloading release {version}...", "pink");
                    await release.DownloadAsync();
                    outPut.AddMessageLine($"Release {version} downloaded", "green");

                    if (repository.RepositoryType == RepositoryType.Git && repository is RepositoryGit repositoryGit && repositoryGit.ReleaseType == ReleaseType.Git)
                    {
                        var assetArchivePath = Path.Combine(release.LocalPath, $"{release.Name}.zip");
                        if (!File.Exists(assetArchivePath))
                        {
                            throw new FileNotFoundException(assetArchivePath);
                        }

                        outPut.AddMessageLine($"Unzipping {Path.GetFileName(assetArchivePath)}...", "pink");
                        await Task.Run(() =>
                        {
                            ZipFile.ExtractToDirectory(assetArchivePath, release.LocalPath);
                            FileTransform.FolderUnix2Dos(release.LocalPath);
                            File.Delete(assetArchivePath);

                            var contentDirectories = Directory.GetDirectories(release.LocalPath, "*.*", SearchOption.TopDirectoryOnly);
                            if (contentDirectories.Length == 1)
                            {
                                var contentDirectory = contentDirectories[0];
                                foreach(var file in Directory.EnumerateFiles(contentDirectory, "*", SearchOption.AllDirectories))
                                {
                                    var dest = file.Replace(contentDirectory, release.LocalPath);
                                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                                    File.Copy(file, dest);
                                }
                                Directory.Delete(contentDirectory, true);
                            }
                        });
                        outPut.AddMessageLine($"{Path.GetFileName(assetArchivePath)} unzipped", "green");
                    }
                }

                return release.LocalPath;
            }
            catch (Exception ex)
            {
                outPut.AddMessageLine($"Error: {ex.Message}", "red");
                return string.Empty;
            }
        }

        private class VersionDownload
        {
            public string Version { get; set; }
            public bool IsDownloading { get; set; }
        }
    }
}
