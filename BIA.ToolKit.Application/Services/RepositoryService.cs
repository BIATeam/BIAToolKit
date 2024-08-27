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

    public class RepositoryService
    {
        private IConsoleWriter outPut;
        private GitService gitService;
        public RepositoryService(IConsoleWriter outPut, GitService gitService)
        {
            this.outPut = outPut;
            this.gitService = gitService;
        }

        public bool CheckRepoFolder(RepositorySettings repository, bool inSync)
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

        public void CleanVersionFolder(RepositorySettings repository)
        {
            if (repository.Versioning == VersioningType.Release)
            {
                var releasePath = Path.Combine(AppSettings.AppFolderPath, repository.Name);
                if (Directory.Exists(releasePath))
                {
                    Directory.Delete(releasePath, true);
                }
                outPut.AddMessageLine("Release Cleaned.", "Pink");
            }
        }

        public async Task<string> PrepareVersionFolder(RepositorySettings repository, string version)
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

                            if (File.Exists(zipPath))
                            {
                                if (IsTextFileEmpty(zipPath))
                                {
                                    File.Delete(zipPath);
                                }
                                else
                                {
                                    // Already downloaded
                                    return string.Empty;
                                }
                            }

                            if (!File.Exists(zipPath))
                            {
                                outPut.AddMessageLine("Begin downloading " + tag.CanonicalName + ".zip", "Pink");
                                var zipUrl = repository.UrlRelease + tag.CanonicalName + ".zip";
                                HttpClientHandler httpClientHandler = new HttpClientHandler
                                {
                                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                                };
                                using (var httpClient = new HttpClient(httpClientHandler))
                                {
                                    var response = await httpClient.GetAsync(zipUrl);
                                    using (var fs = new FileStream(
                                        zipPath,
                                        FileMode.CreateNew))
                                    {
                                        await response.Content.CopyToAsync(fs);
                                    }
                                }
                                outPut.AddMessageLine("Downloaded.", "Pink");
                            }

                            if (!File.Exists(zipPath))
                            {
                                outPut.AddMessageLine("Cannot download release: " + version, "Red");
                            }
                            else
                            {
                                //Force clean
                                if (Directory.Exists(biaTemplatePathVersionUnzip))
                                {
                                    Directory.Delete(biaTemplatePathVersionUnzip, true);
                                }

                                if (!Directory.Exists(biaTemplatePathVersionUnzip))
                                {
                                    ZipFile.ExtractToDirectory(zipPath, biaTemplatePathVersionUnzip);
                                    FileTransform.FolderUnix2Dos(biaTemplatePathVersionUnzip);
                                }
                                var dirContent = Directory.GetDirectories(biaTemplatePathVersionUnzip, "*.*", SearchOption.TopDirectoryOnly);
                                if (dirContent.Length == 1)
                                {
                                    return dirContent[0];
                                }
                            }
                            break;
                        }
                    }
                }
                return "";
            }
            else
            {
                this.gitService.CheckoutTag(repository, version);
                return repository.RootFolderPath;
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
    }
}
