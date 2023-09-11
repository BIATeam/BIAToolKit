namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Management.Automation;
    using System;
    using LibGit2Sharp;
    using System.Linq;
    using LibGit2Sharp.Handlers;
    using System.Diagnostics;
    using System.IO;
    using BIA.ToolKit.Domain.Settings;
    using System.Net;
    using System.IO.Compression;

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
                var releasePath = AppSettings.AppFolderPath + "\\" + repository.Name;
                if (Directory.Exists(releasePath))
                {
                    Directory.Delete(releasePath,true);
                }
            }
        }

        public string PrepareVersionFolder (RepositorySettings repository, string version)
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
                            if (!File.Exists(zipPath))
                            {
                                var zipUrl = repository.UrlRelease + tag.CanonicalName + ".zip";
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(zipUrl, zipPath);
                                }
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
    }
}
