namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.CompanyFiles;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common;
    using LibGit2Sharp;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text.RegularExpressions;

    public class ProjectCreatorService
    {
        private IConsoleWriter consoleWriter;
        public ProjectCreatorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void Create(bool actionFinishedAtEnd, string companyName, string projectName, string projectPath, string biaTemplatePath, string frameworkVersion /*frameworkVersion;*/,
            bool useCompanyFile, CFSettings cfSettings, string companyFilesPath, string companyFileProfile /* CreateCompanyFileProfile.Text*/,
            string appFolderPath,
            string[] angularFronts
            )
        {

            string biaTemplatePathVersion = biaTemplatePath;
            IList<string> localFilesToExclude = new List<string> ();

            if (frameworkVersion == "VX.Y.Z")
            {
                // Copy from local folder
                biaTemplatePathVersion = biaTemplatePath;
                localFilesToExclude = new List<string>() { "^\\.git$", "^\\.vs$" , "\\.csproj\\.user$" , "^bin$", "^obj$", "^node_modules$", "^dist$" };
            }
            else
            {
                using (var repo = new Repository(biaTemplatePath))
                {
                    foreach (var tag in repo.Tags)
                    {
                        if (tag.FriendlyName == frameworkVersion)
                        {
                            var zipPath = appFolderPath + "\\BIATemplate\\" + tag.FriendlyName + ".zip";
                            string biaTemplatePathVersionUnzip = appFolderPath + "\\BIATemplate\\" + tag.FriendlyName;
                            Directory.CreateDirectory(appFolderPath + "\\BIATemplate\\");
                            if (!File.Exists(zipPath))
                            {
                                var zipUrl = Constants.BIATemplateReleaseUrl + tag.CanonicalName + ".zip";
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(zipUrl, zipPath);
                                }
                            }

                            if (!File.Exists(zipPath))
                            {
                                consoleWriter.AddMessageLine("Cannot download release: "+ frameworkVersion, "Red");
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
                                    biaTemplatePathVersion = dirContent[0];
                                }
                            }
                            break;
                        }
                    }
                }
            }

            if (!Directory.Exists(biaTemplatePathVersion))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + biaTemplatePathVersion, "Red");
            }
            else
            {
                consoleWriter.AddMessageLine("Start copy template files.", "Pink");
                FileTransform.CopyFilesRecursively(biaTemplatePathVersion, projectPath, "", localFilesToExclude);

                IList<string> filesToRemove = new List<string>() { "^new-angular-project\\.ps1$" };

                if (useCompanyFile)
                {
                    IList<string> filesToExclude = new List<string>() { "^biaCompanyFiles\\.json$" };
                    foreach (CFOption option in cfSettings.Options)
                    {
                        if (option.IsChecked)
                        {
                            if (option.FilesToRemove != null)
                            {
                                // Remove file of this profile
                                foreach (string fileToRemove in option.FilesToRemove)
                                {
                                    filesToRemove.Add(fileToRemove);
                                }
                            }
                        }
                        else
                        {
                            if (option.Files != null)
                            {
                                // Exclude file of this profile
                                foreach (string fileToExclude in option.Files)
                                {
                                    filesToExclude.Add(fileToExclude);
                                }
                            }
                        }
                    }
                    consoleWriter.AddMessageLine("Start copy company files.", "Pink");
                    FileTransform.CopyFilesRecursively(companyFilesPath, projectPath, companyFileProfile, filesToExclude);
                }

                if (filesToRemove.Count > 0)
                {
                    FileTransform.RemoveRecursively(projectPath, filesToRemove);
                }

                consoleWriter.AddMessageLine("Start rename.", "Pink");
                FileTransform.ReplaceInFileAndFileName(projectPath, "TheBIADevCompany", companyName, FileTransform.replaceInFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, "BIATemplate", projectName, FileTransform.replaceInFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, "thebiadevcompany", companyName.ToLower(), FileTransform.replaceInFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, "biatemplate", projectName.ToLower(), FileTransform.replaceInFileExtensions);


                consoleWriter.AddMessageLine("Start remove BIATemplate only.", "Pink");
                FileTransform.RemoveTemplateOnly(projectPath, "# Begin BIATemplate only", "# End BIATemplate only", new List<string>() { ".gitignore" });

                bool containsFrontAngular = false;
                if (angularFronts?.Length > 0)
                {
                    foreach (var angularFront in angularFronts)
                    {
                        if (angularFront.ToLower() != "angular")
                        {
                            Directory.CreateDirectory(projectPath + "\\" + angularFront);
                            FileTransform.CopyFilesRecursively(projectPath + "\\Angular", projectPath + "\\" + angularFront);
                        }
                        else
                        {
                            containsFrontAngular = true;
                        }
                    }
                }
                if (!containsFrontAngular)
                {
                    Directory.Delete(projectPath + "\\Angular", true);
                }

                consoleWriter.AddMessageLine("Create project finished.", actionFinishedAtEnd ? "Green" : "Blue");
            }
        }

        public void OverwriteBIAFolder(string sourceFolder, string targetFolder, bool actionFinishedAtEnd)
        {
            consoleWriter.AddMessageLine("Start overwrite BIA Folder.", "Pink");
            Regex reg = new Regex( @"\\bia-.*\\", RegexOptions.IgnoreCase);
            string[] biaDirectories = Directory.GetDirectories(sourceFolder, "bia-*", SearchOption.AllDirectories);
            foreach (var biaDirectory in biaDirectories)
            {
                var relativePath = biaDirectory.Substring(sourceFolder.Length);
                var matchBia = reg.Match(relativePath);

                // treat only root folder 
                if (matchBia.Length == 0)
                {
                    var targetDirectory = targetFolder + relativePath;

                    Directory.Delete(targetDirectory, true);
                    Directory.CreateDirectory(targetDirectory);
                    FileTransform.CopyFilesRecursively(biaDirectory, targetDirectory);
                }


            }
            consoleWriter.AddMessageLine("Overwrite BIA Folder finished.", actionFinishedAtEnd ? "Green" : "Blue");

        }

    }
}
