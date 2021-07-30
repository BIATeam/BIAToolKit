namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.CompanyFiles;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Application.Helper;
    using LibGit2Sharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class ProjectCreatorService
    {
        private IConsoleWriter consoleWriter;
        public ProjectCreatorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void Create(string companyName, string projectName, string projectPath, string biaTemplatePath, string frameworkVersion /*frameworkVersion;*/,
            bool useCompanyFile, CFSettings cfSettings, string companyFilesPath, string companyFileProfile /* CreateCompanyFileProfile.Text*/,
            string appFolderPath
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
                            biaTemplatePathVersion = appFolderPath + "\\BIATemplate\\" + tag.FriendlyName;
                            Directory.CreateDirectory(appFolderPath + "\\BIATemplate\\");
                            if (!File.Exists(zipPath))
                            {
                                var zipUrl = Constants.BIATemplateReleaseUrl + tag.CanonicalName + ".zip";
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(zipUrl, zipPath);
                                }
                            }

                            //Force clean
                            if (Directory.Exists(biaTemplatePathVersion))
                            {
                                Directory.Delete(biaTemplatePathVersion, true);
                            }

                            if (!Directory.Exists(biaTemplatePathVersion))
                            {
                                
                                ZipFile.ExtractToDirectory(zipPath, biaTemplatePathVersion);

                                FileTransform.FolderUnix2Dos(biaTemplatePathVersion);
                            }
                            var dirContent = Directory.GetDirectories(biaTemplatePathVersion, "*.*", SearchOption.TopDirectoryOnly);
                            if (dirContent.Length == 1)
                            {
                                biaTemplatePathVersion = dirContent[0];
                            }
                            break;
                        }
                    }
                }
            }

            consoleWriter.AddMessageLine("Start copy template files.", "Pink");
            FileTransform.CopyFilesRecursively(biaTemplatePathVersion, projectPath, "", localFilesToExclude);

            /*
            string dotnetZipPath = biaTemplateBIATemplatePathVersion + "\\BIA.DotNetTemplate." + frameworkVersion.Substring(1) + ".zip";
            string angularZipPath = biaTemplateBIATemplatePathVersion + "\\BIA.AngularTemplate." + frameworkVersion.Substring(1) + ".zip";

            consoleWriter.AddMessageLine("Unzip dotnet.", "Pink");
            ZipFile.ExtractToDirectory(dotnetZipPath, projectPath);
            consoleWriter.AddMessageLine("Unzip angular.", "Pink");
            ZipFile.ExtractToDirectory(angularZipPath, projectPath);
            */


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

            consoleWriter.AddMessageLine("Create project finished.", "Green");
        }
    }
}
