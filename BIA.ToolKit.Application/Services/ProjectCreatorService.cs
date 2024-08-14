namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using static BIA.ToolKit.Common.Constants;

    public class ProjectCreatorService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;

        public ProjectCreatorService(IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            SettingsService settingsService)
        {
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
        }

        public async Task Create(
            bool actionFinishedAtEnd,
            string companyName,
            string projectName,
            string projectPath,

            VersionAndOption versionAndOption,

            string[] angularFronts
            )
        {
            bool withFrontEnd = false;
            bool withFrontFeature = false;
            bool withServiceApi = true;
            bool withDeployDb = false;
            bool withWorkerService = false;
            bool withHangfire = false;

            if (withFrontFeature)
            {
                withDeployDb = true;
                withFrontEnd = true;
            }

            if (withHangfire)
            {
                withDeployDb = true;
                withWorkerService = true;
            }

            List<string> localFilesToExcludes = new List<string>();

            if (versionAndOption.WorkTemplate.Version == "VX.Y.Z")
            {
                // Copy from local folder
                versionAndOption.WorkTemplate.VersionFolderPath = versionAndOption.WorkTemplate.RepositorySettings.RootFolderPath;
                localFilesToExcludes = new List<string>() { "^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$" };
            }
            else
            {
                versionAndOption.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(versionAndOption.WorkTemplate.RepositorySettings, versionAndOption.WorkTemplate.Version);

                if (versionAndOption.WorkTemplate.RepositorySettings.Versioning == VersioningType.Tag)
                {
                    localFilesToExcludes = new List<string>() { "^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$" };
                }
            }

            if (!Directory.Exists(versionAndOption.WorkTemplate.VersionFolderPath))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + versionAndOption.WorkTemplate.VersionFolderPath, "Red");
            }
            else
            {
                List<string> foldersToExcludes = GetFoldersToExcludes(angularFronts, withFrontEnd, withDeployDb, withWorkerService);

                if (!withFrontFeature)
                {
                    List<string> filesToExcludes = GetFileToExcludeWithoutFrontFeature(versionAndOption);
                    localFilesToExcludes.AddRange(filesToExcludes);
                }

                consoleWriter.AddMessageLine("Start copy template files.", "Pink");
                FileTransform.CopyFilesRecursively(versionAndOption.WorkTemplate.VersionFolderPath, projectPath, "", localFilesToExcludes, foldersToExcludes);

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
                    FileTransform.CopyFilesRecursively(versionAndOption.WorkCompanyFile.VersionFolderPath, projectPath, versionAndOption.Profile, filesToExclude, foldersToExcludes);
                }

                if (filesToRemove.Count > 0)
                {
                    FileTransform.RemoveRecursively(projectPath, filesToRemove);
                }

                this.CleanSln(projectPath, versionAndOption, withDeployDb, withWorkerService);
                this.CleanCsProj(projectPath, versionAndOption);

                DirectoryHelper.DeleteEmptyDirectories(projectPath);

                if (!withFrontFeature)
                {
                    DirectoryHelper.DeleteEmptyDirectories(projectPath);
                    FileHelper.CleanFilesByTag(projectPath, new List<string>() { "#if BIA_FRONT_FEATURE"/*, "#if BIA_SERVICE_API"*/ }, new List<string>() { "#endif" }, "*.cs");
                }

                consoleWriter.AddMessageLine("Start rename.", "Pink");
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName, companyName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName, projectName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName.ToLower(), companyName.ToLower(), FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName.ToLower(), projectName.ToLower(), FileTransform.projectFileExtensions);


                consoleWriter.AddMessageLine("Start remove BIATemplate only.", "Pink");
                FileTransform.RemoveTemplateOnly(projectPath, "# Begin BIATemplate only", "# End BIATemplate only", new List<string>() { ".gitignore" });

                bool containsFrontAngular = false;
                if (withFrontEnd && angularFronts?.Length > 0)
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
                    if (Directory.Exists(projectPath + "\\Angular"))
                    {
                        Directory.Delete(projectPath + "\\Angular", true);
                    }
                }

                consoleWriter.AddMessageLine("Create project finished.", actionFinishedAtEnd ? "Green" : "Blue");
            }
        }

        public void OverwriteBIAFolder(string sourceFolder, string targetFolder, bool actionFinishedAtEnd)
        {
            consoleWriter.AddMessageLine("Start overwrite BIA Folder.", "Pink");
            Regex reg = new Regex(@"\\bia-.*\\", RegexOptions.IgnoreCase);
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

        private void CleanSln(string projectPath, VersionAndOption versionAndOption, bool withDeployDb, bool withWorkerService)
        {
            // TODO TEST
            bool without = !withDeployDb || !withWorkerService;

            if (without && !string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(versionAndOption?.WorkTemplate?.VersionFolderPath))
            {
                List<string> slnFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetSolution}", projectPath);
                List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}", projectPath);

                if (slnFiles?.Any() == true && csprojFiles?.Any() == true)
                {
                    List<string> csprojFilesToRemoves = new List<string>();

                    if (!withDeployDb)
                    {
                        csprojFilesToRemoves.AddRange(csprojFiles.Where(x => x.EndsWith($"{BiaProjectName.DeployDB}{FileExtensions.DotNetProject}")));
                    }

                    if (!withWorkerService)
                    {
                        csprojFilesToRemoves.AddRange(csprojFiles.Where(x => x.EndsWith($"{BiaProjectName.WorkerService}{FileExtensions.DotNetProject}")));
                    }

                    if (csprojFilesToRemoves.Any())
                    {
                        DotnetHelper.RemoveProjectsFromSolution(slnFiles[0], csprojFilesToRemoves);
                    }
                }
            }
        }

        private void CleanCsProj(string projectPath, VersionAndOption versionAndOption)
        {
            var csprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}", projectPath);

            // TODO
            var tags = new List<string> { "Bia_ItemGroup_BIA_FRONT_FEATURE" };

            foreach (var csprojFile in csprojFiles)
            {
                XDocument xDocument = XDocument.Load(csprojFile);
                XNamespace ns = xDocument.Root.Name.Namespace;

                List<XElement> itemGroups = xDocument.Descendants(ns + "ItemGroup")
                                .Where(x => tags.Contains((string)x.Attribute("Label"))).ToList();

                XElement defineConstantsElement = xDocument.Descendants(ns + "PropertyGroup")
                    .FirstOrDefault(x => (string)x.Attribute("Condition") == "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'")
                    ?.Element("DefineConstants");

                if (itemGroups.Count > 0 || !string.IsNullOrWhiteSpace(defineConstantsElement?.Value))
                {
                    itemGroups.ForEach(ig => ig.Remove());
                    if (!string.IsNullOrWhiteSpace(defineConstantsElement?.Value))
                    {
                        defineConstantsElement.Value = csprojFile.EndsWith($"{BiaProjectName.Test}{FileExtensions.DotNetProject}") ? "TRACE" : string.Empty;
                    }

                    xDocument.Save(csprojFile);
                }
            }
        }

        private List<string> GetFileToExcludeWithoutFrontFeature(VersionAndOption versionAndOption)
        {
            return this.GetFileToExcludeWithoutFeature(versionAndOption, "Bia_ItemGroup_BIA_FRONT_FEATURE");
        }

        private List<string> GetFileToExcludeWithoutFeature(VersionAndOption versionAndOption, string tagName)
        {
            List<string> filesToExcludes = new List<string>();

            string csprojFile = (FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}")).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(csprojFile))
            {
                XDocument document = XDocument.Load(csprojFile);
                XNamespace ns = document.Root.Name.Namespace;

                XElement itemGroup = document.Descendants(ns + "ItemGroup")
                                        .FirstOrDefault(x => (string)x.Attribute("Label") == tagName);

                if (itemGroup != null)
                {
                    List<string> compileRemoveItems = itemGroup.Elements(ns + "Compile")
                                                      .Where(x => x.Attribute("Remove") != null)
                                                      .Select(x => x.Attribute("Remove").Value)
                                                      .ToList();

                    foreach (string item in compileRemoveItems)
                    {
                        string newPattern;
                        if (item.Contains("**\\*"))
                        {
                            newPattern = ".*" + Regex.Escape(item.Replace("**\\*", "").Replace(".cs", "").Replace("*", "")) + ".*\\.cs$";
                        }
                        else
                        {
                            newPattern = "^" + Regex.Escape(item.Replace("**\\", "").Replace(".cs", "")) + "\\.cs$";
                        }
                        filesToExcludes.Add(newPattern);
                    }

                    itemGroup.Remove();

                    document.Save(csprojFile);
                }
            }

            return filesToExcludes;
        }

        private static List<string> GetFoldersToExcludes(string[] angularFronts, bool withFrontEnd, bool withDeployDb, bool withWorkerService)
        {
            List<string> foldersToExcludes = new List<string>();
            if (!withFrontEnd && angularFronts?.Any() == true)
            {
                foldersToExcludes.AddRange(angularFronts.ToList());
            }

            if (!withDeployDb)
            {
                foldersToExcludes.Add($".*{BiaProjectName.DeployDB}$");
            }

            if (!withWorkerService)
            {
                foldersToExcludes.Add($".*{BiaProjectName.WorkerService}$");
            }

            return foldersToExcludes;
        }
    }
}
