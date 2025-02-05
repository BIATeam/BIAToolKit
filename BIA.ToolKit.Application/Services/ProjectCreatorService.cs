﻿namespace BIA.ToolKit.Application.Services
{
    using System;
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
        private readonly FeatureSettingService featureSettingService;

        public ProjectCreatorService(IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            SettingsService settingsService,
            FeatureSettingService featureSettingService)
        {
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            this.featureSettingService = featureSettingService;
        }

        public async Task Create(
            bool actionFinishedAtEnd,
            string companyName,
            string projectName,
            string projectPath,
            VersionAndOption versionAndOption,
            List<string> angularFronts
            )
        {
            // Ensure to have namespaces correctly formated
            projectName = $"{char.ToUpper(projectName[0])}{projectName[1..]}";

            List<FeatureSetting> featureSettings = versionAndOption?.FeatureSettings?.ToList();

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
                List<string> foldersToExcludes = null;
                if (!featureSettingService.HasAllFeature(featureSettings))
                {
                    foldersToExcludes = featureSettingService.GetFoldersToExcludes(featureSettings);
                    List<string> filesToExcludes = this.GetFileToExcludes(versionAndOption, featureSettings);
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


                this.CleanProject(projectPath, versionAndOption, featureSettings);


                consoleWriter.AddMessageLine("Start rename.", "Pink");
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName, companyName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName, projectName, FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.CompanyName.ToLower(), companyName.ToLower(), FileTransform.projectFileExtensions);
                FileTransform.ReplaceInFileAndFileName(projectPath, versionAndOption.WorkTemplate.RepositorySettings.ProjectName.ToLower(), projectName.ToLower(), FileTransform.projectFileExtensions);


                consoleWriter.AddMessageLine("Start remove BIATemplate only.", "Pink");
                FileTransform.RemoveTemplateOnly(projectPath, "# Begin BIATemplate only", "# End BIATemplate only", new List<string>() { ".gitignore" });

                if(Version.TryParse(versionAndOption.WorkTemplate.Version.Replace("V", ""), out Version projectVersion) && projectVersion >= new Version("3.10.0"))
                {
                    FileTransform.OrderUsing(projectPath);
                }

                bool containsFrontAngular = false;
                if (angularFronts.Count > 0)
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

                this.featureSettingService.Save(featureSettings, projectPath);

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

        private void CleanSln(string projectPath, VersionAndOption versionAndOption)
        {
            if (!string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(versionAndOption?.WorkTemplate?.VersionFolderPath))
            {
                List<string> slnFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetSolution}");
                if (!slnFiles.Any()) return;

                List<string> templateCsprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}", projectPath);

                List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetProject}");

                List<string> csprojFilesToRemoves = templateCsprojFiles?.Except(csprojFiles).ToList();

                if (!csprojFilesToRemoves.Any()) return;

                DotnetHelper.RemoveProjectsFromSolution(slnFiles[0], csprojFilesToRemoves);
            }
        }

        private void CleanCsProj(string projectPath)
        {
            List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetProject}");

            foreach (string csprojFile in csprojFiles)
            {
                XDocument xDocument = XDocument.Load(csprojFile);
                XNamespace ns = xDocument.Root.Name.Namespace;

                List<XElement> itemGroups = xDocument.Descendants(ns + "ItemGroup")
                                .Where(x => ((string)x.Attribute("Label"))?.StartsWith(BiaFeatureTag.ItemGroupTag) == true).ToList();

                XElement defineConstantsElement = xDocument.Descendants(ns + "PropertyGroup")
                    .FirstOrDefault(x => (string)x.Attribute("Condition") == "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'")
                    ?.Element(ns + "DefineConstants");

                bool shouldSaveDocument = false;

                if (itemGroups.Count > 0)
                {
                    itemGroups.ForEach(ig => ig.Remove());
                    shouldSaveDocument = true;
                }

                if (!string.IsNullOrWhiteSpace(defineConstantsElement?.Value))
                {
                    defineConstantsElement.Value = csprojFile.EndsWith($"{BiaProjectName.Test}{FileExtensions.DotNetProject}") ? "TRACE" : string.Empty;
                    shouldSaveDocument = true;
                }

                if (shouldSaveDocument)
                {
                    xDocument.Save(csprojFile);
                }
            }
        }

        private List<string> GetFileToExcludes(VersionAndOption versionAndOption, List<FeatureSetting> settings)
        {
            List<string> tags = featureSettingService.GetBiaFeatureTagToDeletes(settings, BiaFeatureTag.ItemGroupTag);

            List<string> filesToExcludes = new List<string>();

            string csprojFile = (FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}")).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(csprojFile))
            {
                XDocument document = XDocument.Load(csprojFile);
                XNamespace ns = document.Root.Name.Namespace;

                XElement itemGroup = document.Descendants(ns + "ItemGroup")
                                        .FirstOrDefault(x => tags.Contains((string)x.Attribute("Label")));

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

                        if (!filesToExcludes.Contains(newPattern))
                        {
                            filesToExcludes.Add(newPattern);
                        }
                    }
                }
            }

            return filesToExcludes;
        }

        private void CleanProject(string projectPath, VersionAndOption versionAndOption, List<FeatureSetting> featureSettings)
        {
            if (!featureSettingService.HasAllFeature(versionAndOption.FeatureSettings.ToList()))
            {
                this.CleanSln(projectPath, versionAndOption);
                this.CleanCsProj(projectPath);

                DirectoryHelper.DeleteEmptyDirectories(projectPath);

                List<string> tagToDeletes = featureSettingService.GetBiaFeatureTagToDeletes(featureSettings, "#if ");
                FileHelper.CleanFilesByTag(projectPath, tagToDeletes, new List<string>() { "#endif" }, $"*{FileExtensions.DotNetClass}", true);
            }

            List<string> tags = featureSettingService.GetAllBiaFeatureTag(featureSettings, "#if ");
            FileHelper.CleanFilesByTag(projectPath, tags, new List<string>() { "#endif" }, $"*{FileExtensions.DotNetClass}", false);
        }
    }
}
