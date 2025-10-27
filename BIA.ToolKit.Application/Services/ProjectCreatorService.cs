namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;
    using static BIA.ToolKit.Common.Constants;

    public class ProjectCreatorService
    {
        private readonly IConsoleWriter consoleWriter;
        private readonly RepositoryService repositoryService;
        private readonly CSharpParserService parserService;
        private readonly SettingsService settingsService;


        public ProjectCreatorService(IConsoleWriter consoleWriter,
            RepositoryService repositoryService,
            SettingsService settingsService,
            CSharpParserService parserService)
        {
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            this.parserService = parserService;
            this.settingsService = settingsService;
        }

        public async Task Create(
            bool actionFinishedAtEnd,
            string projectPath,
            ProjectParameters projectParameters
            )
        {
            // Ensure to have namespaces correctly formated
            projectParameters.ProjectName = $"{char.ToUpper(projectParameters.ProjectName[0])}{projectParameters.ProjectName[1..]}";

            List<FeatureSetting> featureSettings = projectParameters.VersionAndOption?.FeatureSettings.ToList();

            List<string> localFilesToExcludes = new List<string>();

            if (projectParameters.VersionAndOption.WorkTemplate.Version == "VX.Y.Z")
            {
                // Copy from local folder
                projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath = projectParameters.VersionAndOption.WorkTemplate.Repository.LocalPath;
                localFilesToExcludes = new List<string>() { "^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$" };
            }
            else
            {
                projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(projectParameters.VersionAndOption.WorkTemplate.Repository, projectParameters.VersionAndOption.WorkTemplate.Version);
            }

            if (!Directory.Exists(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath, "Red");
            }
            else
            {
                List<string> foldersToExcludes = null;
                if (!featureSettings.HasAllFeature())
                {
                    foldersToExcludes = featureSettings.GetFoldersToExcludes();
                    List<string> filesToExcludes = GetFilesToExcludes(projectParameters.VersionAndOption, featureSettings);
                    localFilesToExcludes.AddRange(filesToExcludes);
                }

                consoleWriter.AddMessageLine("Start copy template files.", "Pink");
                await Task.Run(() => FileTransform.CopyFilesRecursively(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath, projectPath, "", localFilesToExcludes, foldersToExcludes));

                IList<string> filesToRemove = new List<string>() { "^new-angular-project\\.ps1$", "BiaToolKit_FeatureSetting\\.json" };

                if (projectParameters.VersionAndOption.UseCompanyFiles)
                {
                    IList<string> filesToExclude = new List<string>() { "^biaCompanyFiles\\.json$" };
                    foreach (CFOption option in projectParameters.VersionAndOption.Options)
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
                    await Task.Run(() => FileTransform.CopyFilesRecursively(projectParameters.VersionAndOption.WorkCompanyFile.VersionFolderPath, projectPath, projectParameters.VersionAndOption.Profile, filesToExclude, foldersToExcludes));
                }

                if (filesToRemove.Count > 0)
                {
                    await Task.Run(() => FileTransform.RemoveRecursively(projectPath, filesToRemove));
                }


                this.CleanProject(projectPath, projectParameters.VersionAndOption, featureSettings);


                consoleWriter.AddMessageLine("Start rename.", "Pink");
                await Task.Run(() =>
                {
                    FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.CompanyName, projectParameters.CompanyName, FileTransform.projectFileExtensions);
                    FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.ProjectName, projectParameters.ProjectName, FileTransform.projectFileExtensions);
                    FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.CompanyName.ToLower(), projectParameters.CompanyName.ToLower(), FileTransform.projectFileExtensions);
                    FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.ProjectName.ToLower(), projectParameters.ProjectName.ToLower(), FileTransform.projectFileExtensions);
                });

                await ReplaceInFileFromConfig(projectPath, projectParameters);

                consoleWriter.AddMessageLine("Start remove BIATemplate only.", "Pink");
                await Task.Run(() => FileTransform.RemoveTemplateOnly(projectPath, "# Begin BIATemplate only", "# End BIATemplate only", new List<string>() { ".gitignore" }));

                if (projectParameters.VersionAndOption.WorkTemplate.Version.Equals("VX.Y.Z") || Version.TryParse(projectParameters.VersionAndOption.WorkTemplate.Version.Replace("V", ""), out Version projectVersion) && projectVersion >= new Version("3.10.0"))
                {
                    await Task.Run(() => FileTransform.OrderUsingFromFolder(projectPath));
                }

                await Task.Run(() =>
                {
                    bool containsFrontAngular = false;
                    if (projectParameters.AngularFronts.Count > 0)
                    {
                        foreach (var angularFront in projectParameters.AngularFronts)
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


                    string rootBiaFolder = Path.Combine(projectPath, Constants.FolderBia);
                    if (!Directory.Exists(rootBiaFolder))
                    {
                        Directory.CreateDirectory(rootBiaFolder);
                    }

                    string projectGenerationFile = Path.Combine(rootBiaFolder, settingsService.ReadSetting("ProjectGeneration"));
                    VersionAndOptionDto versionAndOptionDto = new VersionAndOptionDto();
                    VersionAndOptionMapper.ModelToDto(projectParameters.VersionAndOption, versionAndOptionDto);
                    CommonTools.SerializeToJsonFile(versionAndOptionDto, projectGenerationFile);
                });

                CleanBiaToolkitJsonFiles(projectPath);

                consoleWriter.AddMessageLine("Create project finished.", actionFinishedAtEnd ? "Green" : "Blue");
            }
        }

        public async Task OverwriteBIAFolder(string sourceFolder, string targetFolder, bool actionFinishedAtEnd)
        {
            consoleWriter.AddMessageLine("Start overwrite BIA Folder.", "Pink");
            await Task.Run(() =>
            {
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
            });
            consoleWriter.AddMessageLine("Overwrite BIA Folder finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task ReplaceInFileFromConfig(string projectPath, ProjectParameters projectParameters)
        {
            foreach (FeatureSetting featureSetting in projectParameters.VersionAndOption.FeatureSettings)
            {
                await Task.Run(() => FileTransform.ReplaceInFileAndFileName(projectPath, "BIAToolkit_FeatureSetting_" + featureSetting.DisplayName, featureSetting.IsSelected ? "true" : "false", FileTransform.projectFileExtensions));
            }
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

        private static List<string> GetFilesToExcludes(VersionAndOption versionAndOption, List<FeatureSetting> settings)
        {
            List<string> tags = settings.GetBiaFeatureTagToDeletes(BiaFeatureTag.ItemGroupTag);

            List<string> filesToExcludes = new List<string>();
            var csprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}");
            foreach (var csprojFile in csprojFiles)
            {
                XDocument document = XDocument.Load(csprojFile);
                XNamespace ns = document.Root.Name.Namespace;

                var itemGroups = document.Descendants(ns + "ItemGroup").Where(x => tags.Contains((string)x.Attribute("Label"))).ToList();
                foreach(var itemGroup in itemGroups)
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
            if (!versionAndOption.FeatureSettings.ToList().HasAllFeature())
            {
                this.CleanSln(projectPath, versionAndOption);
                this.CleanCsProj(projectPath);

                DirectoryHelper.DeleteEmptyDirectories(projectPath);

                List<string> tagToDeletes = featureSettings.GetBiaFeatureTagToDeletes();
                FileHelper.CleanFilesByTag(projectPath, tagToDeletes, "#if", "#endif", $"*{FileExtensions.DotNetClass}", true);
            }

            List<string> tags = featureSettings.GetAllBiaFeatureTag();
            FileHelper.CleanFilesByTag(projectPath, tags, "#if", "#endif", $"*{FileExtensions.DotNetClass}", false);
        }

        private void CleanBiaToolkitJsonFiles(string projectPath)
        {
            const string biatoolkitFilename = "biatoolkit.json";

            consoleWriter.AddMessageLine("Remove biatookit.json from BIA folders.", "Pink");
            CleanFiles(Path.Combine(projectPath, Constants.FolderDotNet, Constants.FolderBia), biatoolkitFilename);
            CleanFiles(Path.Combine(projectPath, Constants.FolderAngular, Constants.FolderBia), biatoolkitFilename);
        }

        private void CleanFiles(string rootDirectory, string filePattern)
        {
            if (!Directory.Exists(rootDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(rootDirectory, filePattern, SearchOption.AllDirectories))
            {
                consoleWriter.AddMessageLine($"-> Delete {file}", "orange");
                File.Delete(file);
            }
        }
    }
}
