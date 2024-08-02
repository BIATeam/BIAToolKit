namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using static BIA.ToolKit.Common.Constants;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    public class ProjectCreatorService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;
        private readonly string projectCreatorSettingFileName;

        public ProjectCreatorService(IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            SettingsService settingsService)
        {
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            projectCreatorSettingFileName = settingsService.ReadSetting("ProjectCreatorSettingFileName");
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
            bool withAppFeature = false;
            bool withHangfire = false;
            bool withDatabase = withAppFeature || withHangfire;

            List<string> localFilesToExclude = new List<string>();
            List<string> foldersToExcludes = new List<string>();

            if (versionAndOption.WorkTemplate.Version == "VX.Y.Z")
            {
                // Copy from local folder
                versionAndOption.WorkTemplate.VersionFolderPath = versionAndOption.WorkTemplate.RepositorySettings.RootFolderPath;
                localFilesToExclude = new List<string>() { "^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$" };
            }
            else
            {
                versionAndOption.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(versionAndOption.WorkTemplate.RepositorySettings, versionAndOption.WorkTemplate.Version);

                if (versionAndOption.WorkTemplate.RepositorySettings.Versioning == VersioningType.Tag)
                {
                    localFilesToExclude = new List<string>() { "^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$" };
                }
            }

            if (!withFrontEnd && angularFronts?.Any() == true)
            {
                foldersToExcludes.AddRange(angularFronts.ToList());
                angularFronts = null;
            }

            if (!withDatabase)
            {
                foldersToExcludes.AddRange(
                    new List<string>
                    {
                        $".*{BiaProjectName.DeployDB}$",
                        $".*{BiaProjectName.WorkerService}$",
                    });
            }

            if (!withAppFeature)
            {
                ProjectCreatorSetting projectCreatorSetting = this.GetProjectCreatorSetting(versionAndOption.WorkTemplate.VersionFolderPath);
                localFilesToExclude.AddRange(projectCreatorSetting.WithoutAppFeature.FilesToExcludes);
            }

            if (!Directory.Exists(versionAndOption.WorkTemplate.VersionFolderPath))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + versionAndOption.WorkTemplate.VersionFolderPath, "Red");
            }
            else
            {
                consoleWriter.AddMessageLine("Start copy template files.", "Pink");
                FileTransform.CopyFilesRecursively(versionAndOption.WorkTemplate.VersionFolderPath, projectPath, "", localFilesToExclude, foldersToExcludes);

                IList<string> filesToRemove = new List<string>() { "^new-angular-project\\.ps1$", projectCreatorSettingFileName };

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

                if (!withDatabase)
                {
                    List<string> slnFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetSolution}", projectPath);
                    List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}", projectPath);

                    csprojFiles = csprojFiles
                        .Where(x => x.EndsWith($"{BiaProjectName.DeployDB}{FileExtensions.DotNetProject}") ||
                                    x.EndsWith($"{BiaProjectName.WorkerService}{FileExtensions.DotNetProject}"))
                        .ToList();

                    DotnetHelper.RemoveProjectsFromSolution(slnFiles[0], csprojFiles);
                }

                if (!withAppFeature)
                {
                    DirectoryHelper.DeleteEmptyDirectories(projectPath);
                    FileHelper.CleanFilesByTag(projectPath, @"// BIAToolKit - Begin AppFeature", @"// BIAToolKit - End AppFeature", "*.cs");
                }

                consoleWriter.AddMessageLine("Start rename.", "Pink");
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName, companyName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName, projectName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName.ToLower(), companyName.ToLower(), FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName.ToLower(), projectName.ToLower(), FileTransform.projectFileExtensions);


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

        private ProjectCreatorSetting GetProjectCreatorSetting(string versionFolderPath)
        {
            //ProjectCreatorSetting projectCreatorSettingInit = new ProjectCreatorSetting();
            //projectCreatorSettingInit.WithoutAppFeature.FilesToExcludes = new List<string>()
            //{
            //    ".*Audit.*\\.cs$",
            //    "^AuthController\\.cs$",
            //    ".*AuthAppService\\.cs$",
            //    ".*Error.*\\.cs$",
            //    ".*LogsController.*\\.cs$",
            //    ".*Member.*\\.cs$",
            //    ".*ModelBuilder.*\\.cs$",
            //    ".*Notification.*\\.cs$",
            //    ".*Query.*\\.cs$",
            //    "^(?!RoleId\\.cs$).*Role.*\\.cs$",
            //    ".*SearchExpressionService.*\\.cs$",
            //    ".*Site.*\\.cs$",
            //    ".*Synchronize.*\\.cs$",
            //    ".*Team.*\\.cs$",
            //    ".*Translation.*\\.cs$",
            //    "^(?!UserFromDirectory\\.cs$|SearchUserResponseDto\\.cs$|.*UserIdentityKey.*\\.cs$|.*UserPermissionDomainService.*\\.cs$).*User.*\\.cs$",
            //    ".*View.*\\.cs$",
            //};

            //var jsonInit = JsonSerializer.Serialize(projectCreatorSettingInit);
            //await File.WriteAllTextAsync("D:\\Source\\GitHub\\BIATeam\\BIADemo\\DotNet\\BiaToolKit_ProjectCreator.json", jsonInit);

            string json = File.ReadAllText(versionFolderPath + "\\DotNet\\" + projectCreatorSettingFileName);
            ProjectCreatorSetting projectCreatorSetting = JsonSerializer.Deserialize<ProjectCreatorSetting>(json);

            return projectCreatorSetting;
        }
    }
}
