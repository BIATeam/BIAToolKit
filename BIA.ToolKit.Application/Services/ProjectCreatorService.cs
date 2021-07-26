namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.CompanyFiles;
    using BIA.ToolKit.Application.Helper;
    using BIAToolKit.ToolKit.Application.Helper;
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ProjectCreatorService
    {
        private IConsoleWriter consoleWriter;
        public ProjectCreatorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void Create(string companyName, string projectName, string projectPath, string biaDemoBIATemplatePath, string frameworkVersion /*frameworkVersion;*/,
            bool useCompanyFile, CFSettings cfSettings, string companyFilesPath, string companyFileProfile /* CreateCompanyFileProfile.Text*/
            )
        {


            string biaDemoBIATemplatePathVersion = biaDemoBIATemplatePath + "\\" + frameworkVersion;

            string dotnetZipPath = biaDemoBIATemplatePathVersion + "\\BIA.DotNetTemplate." + frameworkVersion.Substring(1) + ".zip";
            string angularZipPath = biaDemoBIATemplatePathVersion + "\\BIA.AngularTemplate." + frameworkVersion.Substring(1) + ".zip";

            consoleWriter.AddMessageLine("Unzip dotnet.", "Pink");
            ZipFile.ExtractToDirectory(dotnetZipPath, projectPath);
            consoleWriter.AddMessageLine("Unzip angular.", "Pink");
            ZipFile.ExtractToDirectory(angularZipPath, projectPath);

            IList<string> filesToRemove = new List<string>() { "new-angular-project.ps1" };

            if (useCompanyFile)
            {
                consoleWriter.AddMessageLine("Start copy company files.", "Pink");

                IList<string> filesToExclude = new List<string>() { "\\.biaCompanyFiles" };
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
                FileTransform.CopyFilesRecursively(companyFilesPath, projectPath, companyFileProfile, filesToExclude);
            }

            if (filesToRemove.Count > 0)
            {
                FileTransform.RemoveRecursively(projectPath, filesToRemove);
            }

            consoleWriter.AddMessageLine("Start rename.", "Pink");
            FileTransform.ReplaceInFileAndFileName(projectPath, "TheBIADevCompany", companyName, FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "BIATemplate", projectName, FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "thebiadevcompany", companyName.ToLower(), FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "biatemplate", projectName.ToLower(), FileTransform.replaceInFileExtenssions);

            consoleWriter.AddMessageLine("Create project finished.", "Green");
        }
    }
}
