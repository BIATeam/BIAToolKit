namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using LibGit2Sharp;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text.RegularExpressions;

    public class ProjectCreatorService
    {
        private IConsoleWriter consoleWriter;
        private RepositoryService repositoryService;

        public ProjectCreatorService(IConsoleWriter consoleWriter, RepositoryService repositoryService)
        {
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
        }

        public void Create(
            bool actionFinishedAtEnd, 
            string companyName, 
            string projectName, 
            string projectPath,

            VersionAndOption versionAndOption,

            string[] angularFronts
            )
        {

            versionAndOption.WorkTemplate.VersionFolderPath = "";
            IList<string> localFilesToExclude = new List<string> ();

            if (versionAndOption.WorkTemplate.Version == "VX.Y.Z")
            {
                // Copy from local folder
                versionAndOption.WorkTemplate.VersionFolderPath = versionAndOption.WorkTemplate.RepositorySettings.RootFolderPath;
                localFilesToExclude = new List<string>() { "^\\.git$", "^\\.vs$" , "\\.csproj\\.user$" , "^bin$", "^obj$", "^node_modules$", "^dist$" };
            }
            else
            {
                versionAndOption.WorkTemplate.VersionFolderPath = this.repositoryService.PrepareVersionFolder(versionAndOption.WorkTemplate.RepositorySettings, versionAndOption.WorkTemplate.Version);
            }

            if (!Directory.Exists(versionAndOption.WorkTemplate.VersionFolderPath))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + versionAndOption.WorkTemplate.VersionFolderPath, "Red");
            }
            else
            {
                consoleWriter.AddMessageLine("Start copy template files.", "Pink");
                FileTransform.CopyFilesRecursively(versionAndOption.WorkTemplate.VersionFolderPath, projectPath, "", localFilesToExclude);

                IList<string> filesToRemove = new List<string>() { "^new-angular-project\\.ps1$" };

                if (versionAndOption.UseCompanyFiles)
                {
                    IList<string> filesToExclude = new List<string>() { "^biaCompanyFiles\\.json$" };
                    foreach (CFOption option in versionAndOption.Options)
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
                    FileTransform.CopyFilesRecursively(versionAndOption.WorkCompanyFile.VersionFolderPath, projectPath, versionAndOption.Profile, filesToExclude);
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
