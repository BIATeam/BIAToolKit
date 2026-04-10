namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Mapper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Common.Helpers;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;
    using static BIA.ToolKit.Common.Constants;

    public partial class ProjectCreatorService(IConsoleWriter consoleWriter,
        RepositoryService repositoryService,
        SettingsService settingsService,
        ProjectService projectService)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;
        private readonly RepositoryService repositoryService = repositoryService;
        private readonly SettingsService settingsService = settingsService;
        private readonly ProjectService projectService = projectService;
        private static readonly int MaxRetryCount = 3;

        public async Task<bool> Create(
            bool actionFinishedAtEnd,
            string projectPath,
            ProjectParameters projectParameters,
            int tryCount = 0,
            CancellationToken ct = default
            )
        {
            bool success;
            try
            {
                success = await CreateCore(actionFinishedAtEnd, projectPath, projectParameters, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine("An unexpected error occurred during project creation: " + ex.Message, "Red");
                success = false;
            }

            if (!success)
            {
                consoleWriter.AddMessageLine("Project creation failed.", "Red");

                if (Directory.Exists(projectPath))
                {
                    consoleWriter.AddMessageLine("Cleaning up created project due to errors...", "Gray");
                    await Task.Run(() => Directory.Delete(projectPath, true), ct);
                }

                if (tryCount < MaxRetryCount)
                {
                    consoleWriter.AddMessageLine("Waiting before retrying...", "Gray");
                    await Task.Delay(5000, ct);
                    consoleWriter.AddMessageLine($"Retrying project creation (Attempt {++tryCount}/{MaxRetryCount})...", "Yellow");
                    return await Create(actionFinishedAtEnd: actionFinishedAtEnd, projectPath: projectPath, projectParameters: projectParameters, tryCount: tryCount, ct: ct);
                }

                return false;
            }

            return true;
        }

        private async Task<bool> CreateCore(
            bool actionFinishedAtEnd,
            string projectPath,
            ProjectParameters projectParameters,
            System.Threading.CancellationToken cancellationToken
            )
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Ensure to have namespaces correctly formated
            projectParameters.ProjectName = $"{char.ToUpper(projectParameters.ProjectName[0])}{projectParameters.ProjectName[1..]}";

            List<FeatureSetting> featureSettings = projectParameters.VersionAndOption?.FeatureSettings.ToList() ?? [];

            List<string> foldersToExcludes = [];
            List<string> localFilesToExcludes = [];

            if (projectParameters.VersionAndOption.WorkTemplate.Version == "VX.Y.Z")
            {
                // Copy from local folder
                projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath = projectParameters.VersionAndOption.WorkTemplate.Repository.LocalPath;
                foldersToExcludes.AddRange("^\\.git$", "^\\.vs$", "\\.csproj\\.user$", "^bin$", "^obj$", "^node_modules$", "^dist$");
            }
            else
            {
                projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath = await repositoryService.PrepareVersionFolder(projectParameters.VersionAndOption.WorkTemplate.Repository, projectParameters.VersionAndOption.WorkTemplate.Version, cancellationToken);
            }

            if (!Directory.Exists(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath))
            {
                consoleWriter.AddMessageLine("The template source folder do not exist: " + projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath, "Red");
                return false;
            }

            if (!featureSettings.HasAllFeature())
            {
                foldersToExcludes.AddRange(featureSettings.GetFoldersToExcludes());
                localFilesToExcludes.AddRange(featureSettings.GetFilesToExcludes());
                localFilesToExcludes.AddRange(GetFilesToExcludes(projectParameters, featureSettings));
            }

            consoleWriter.AddMessageLine("Start copy template files.", "Pink");
            await Task.Run(() => FileTransform.CopyFilesRecursively(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath, projectPath, "", localFilesToExcludes, foldersToExcludes), cancellationToken);

            IList<string> filesToRemove = ["^new-angular-project\\.ps1$", "BiaToolKit_FeatureSetting\\.json"];

            if (projectParameters.VersionAndOption.UseCompanyFiles)
            {
                IList<string> filesToExclude = ["^biaCompanyFiles\\.json$"];
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
                await Task.Run(() => FileTransform.CopyFilesRecursively(projectParameters.VersionAndOption.WorkCompanyFile.VersionFolderPath, projectPath, projectParameters.VersionAndOption.Profile, filesToExclude, foldersToExcludes), cancellationToken);
            }

            if (filesToRemove.Count > 0)
            {
                await Task.Run(() => FileTransform.RemoveRecursively(projectPath, filesToRemove), cancellationToken);
            }

            await Task.Run(() => CleanProject(projectPath, projectParameters.VersionAndOption, featureSettings), cancellationToken);

            await RenameInProject(projectPath, projectParameters, cancellationToken);

            consoleWriter.AddMessageLine("Start remove BIATemplate only.", "Pink");
            await Task.Run(() => FileTransform.RemoveTemplateOnly(projectPath, "# Begin BIATemplate only", "# End BIATemplate only", [".gitignore"]), cancellationToken);

            if (projectParameters.VersionAndOption.WorkTemplate.Version.Equals("VX.Y.Z") || Version.TryParse(projectParameters.VersionAndOption.WorkTemplate.Version.Replace("V", ""), out Version projectVersion) && projectVersion >= new Version("3.10.0"))
            {
                consoleWriter.AddMessageLine("Start order usings.", "Pink");
                await Task.Run(() => FileTransform.OrderUsingFromFolder(projectPath), cancellationToken);
            }

            bool containsFrontAngular = false;
            if (projectParameters.AngularFronts.Count > 0)
            {
                consoleWriter.AddMessageLine("Start copy Angular files.", "Pink");
                foreach (string angularFront in projectParameters.AngularFronts)
                {
                    if (!angularFront.Equals("angular", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await Task.Run(() =>
                        {
                            Directory.CreateDirectory(projectPath + "\\" + angularFront);
                            FileTransform.CopyFilesRecursively(projectPath + "\\Angular", projectPath + "\\" + angularFront);
                        }, cancellationToken);
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
                    consoleWriter.AddMessageLine("Start remove Angular folder.", "Pink");
                    await Task.Run(() =>
                    {
                        Directory.Delete(projectPath + "\\Angular", true);
                    }, cancellationToken);
                }
            }

            if (featureSettings.Any(f => f.Id == (int)BiaFeatureSettingsEnum.CreateDefaultTeam && f.IsSelected))
            {
                // TODO: mock purpose, implements UI
                projectParameters.VersionAndOption.HasDefaultTeam = true;
                projectParameters.VersionAndOption.DefaultTeamName = "Site";
                projectParameters.VersionAndOption.DefaultTeamNamePlural = "Sites";
                projectParameters.VersionAndOption.DefaultTeamDomainName = "Site";
                // --------

                await CreateDefaultTeam(projectPath, projectParameters, cancellationToken);
            }

            string rootBiaFolder = Path.Combine(projectPath, Constants.FolderBia);
            await Task.Run(() =>
            {
                if (!Directory.Exists(rootBiaFolder))
                {
                    Directory.CreateDirectory(rootBiaFolder);
                }
            }, cancellationToken);

            consoleWriter.AddMessageLine("Start generation file creation.", "Pink");
            string projectGenerationFile = Path.Combine(rootBiaFolder, settingsService.ReadSetting("ProjectGeneration"));
            var versionAndOptionDto = new VersionAndOptionDto();
            VersionAndOptionMapper.ModelToDto(projectParameters.VersionAndOption, versionAndOptionDto);
            CommonTools.SerializeToJsonFile(versionAndOptionDto, projectGenerationFile);

            CleanBiaToolkitJsonFiles(projectPath);

            consoleWriter.AddMessageLine("Create project finished.", actionFinishedAtEnd ? "Green" : "Blue");
            return true;
        }

        private async Task CreateDefaultTeam(string projectPath, ProjectParameters projectParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            consoleWriter.AddMessageLine("Start create default team.", "Pink");
            var project = new Domain.ModifyProject.Project
            {
                Folder = projectPath,
            };

            await projectService.LoadProject(project, cancellationToken);

            var fileGeneratorService = new FileGeneratorService(consoleWriter);
            await fileGeneratorService.Init(project, cancellationToken: cancellationToken);

            bool hasFront = project.BIAFronts.Count > 0;
            string angularFront = Path.Combine(projectPath, project.BIAFronts.FirstOrDefault() ?? string.Empty);
            string prettierTemporaryToolProjectPath = null;
            if (hasFront)
            {
                prettierTemporaryToolProjectPath = await fileGeneratorService.CreateTemporaryPrettierToolProject(angularFront, cancellationToken);
                fileGeneratorService.SetPrettierAngularProjectPath(prettierTemporaryToolProjectPath);
            }

            try
            {
                string baseKeyType = "int";
                await fileGeneratorService.GenerateTeamAsync(new FileGenerator.Contexts.FileGeneratorTeamContext
                {
                    CompanyName = projectParameters.CompanyName,
                    ProjectName = projectParameters.ProjectName,
                    EntityName = projectParameters.VersionAndOption.DefaultTeamName,
                    EntityNamePlural = projectParameters.VersionAndOption.DefaultTeamNamePlural,
                    DomainName = projectParameters.VersionAndOption.DefaultTeamDomainName,
                    BaseKeyType = baseKeyType,
                    IsTeam = true,
                    GenerateBack = true,
                    GenerateFront = hasFront,
                    AngularFront = angularFront
                }, cancellationToken);

                await fileGeneratorService.GenerateDtoAsync(new FileGenerator.Contexts.FileGeneratorDtoContext
                {
                    CompanyName = projectParameters.CompanyName,
                    ProjectName = projectParameters.ProjectName,
                    EntityName = projectParameters.VersionAndOption.DefaultTeamName,
                    EntityNamePlural = projectParameters.VersionAndOption.DefaultTeamNamePlural,
                    DomainName = projectParameters.VersionAndOption.DefaultTeamDomainName,
                    BaseKeyType = baseKeyType,
                    IsTeam = true,
                    GenerateBack = true,
                    GenerateFront = hasFront,
                    AngularFront = angularFront
                }, cancellationToken);

                await fileGeneratorService.GenerateCRUDAsync(new FileGenerator.Contexts.FileGeneratorCrudContext
                {
                    CompanyName = projectParameters.CompanyName,
                    ProjectName = projectParameters.ProjectName,
                    EntityName = projectParameters.VersionAndOption.DefaultTeamName,
                    EntityNamePlural = projectParameters.VersionAndOption.DefaultTeamNamePlural,
                    DomainName = projectParameters.VersionAndOption.DefaultTeamDomainName,
                    BaseKeyType = baseKeyType,
                    IsTeam = true,
                    TeamTypeId = 1,
                    TeamRoleId = 1,
                    DisplayItemName = "Title",
                    GenerateBack = true,
                    GenerateFront = hasFront,
                    AngularFront = angularFront
                }, cancellationToken);
            }
            finally
            {
                if (hasFront && !string.IsNullOrWhiteSpace(prettierTemporaryToolProjectPath))
                {
                    await fileGeneratorService.CleanupTemporaryPrettierToolProject(prettierTemporaryToolProjectPath, CancellationToken.None);
                }
            }
        }

        private async Task RenameInProject(string projectPath, ProjectParameters projectParameters, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            consoleWriter.AddMessageLine("Start rename.", "Pink");
            await FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.CompanyName, projectParameters.CompanyName, FileTransform.projectFileExtensions, consoleWriter, cancellationToken);
            await FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.ProjectName, projectParameters.ProjectName, FileTransform.projectFileExtensions, consoleWriter, cancellationToken);
            await FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.CompanyName.ToLower(), projectParameters.CompanyName.ToLower(), FileTransform.projectFileExtensions, consoleWriter, cancellationToken);
            await FileTransform.ReplaceInFileAndFileName(projectPath, projectParameters.VersionAndOption.WorkTemplate.Repository.ProjectName.ToLower(), projectParameters.ProjectName.ToLower(), FileTransform.projectFileExtensions, consoleWriter, cancellationToken);
            await ReplaceInFileFromConfig(projectPath, projectParameters);
        }

        public void ClearPermissions(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(filePath);
                    jsonString = StripJsonComments(jsonString);

                    JsonDocument jsonDoc;
                    try
                    {
                        jsonDoc = JsonDocument.Parse(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException("Invalid JSON format in the file.", ex);
                    }

                    JsonElement root = jsonDoc.RootElement.Clone();
                    if (!root.TryGetProperty("BiaNet", out JsonElement biaNet))
                    {
                        throw new InvalidOperationException("The 'BiaNet' property was not found in the JSON.");
                    }

                    using var stream = new MemoryStream();
                    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                    writer.WriteStartObject();
                    writer.WritePropertyName("BiaNet");
                    writer.WriteStartObject();

                    foreach (JsonProperty property in biaNet.EnumerateObject())
                    {
                        if (property.Name != "Permissions")
                        {
                            property.WriteTo(writer);
                        }
                        else
                        {
                            writer.WritePropertyName("Permissions");
                            writer.WriteStartArray();
                            writer.WriteEndArray();
                        }
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.Flush();

                    File.WriteAllText(filePath, Encoding.UTF8.GetString(stream.ToArray()));
                }
                catch
                {
                    consoleWriter.AddMessageLine($"Error while removing permissions from {filePath}", "red");
                }
            }
        }

        private static string StripJsonComments(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            var sb = new StringBuilder(json.Length);

            bool inString = false;
            bool inSingleLineComment = false;
            bool inMultiLineComment = false;
            bool isEscaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char current = json[i];
                char next = i + 1 < json.Length ? json[i + 1] : '\0';

                if (inSingleLineComment)
                {
                    if (current == '\r' || current == '\n')
                    {
                        inSingleLineComment = false;
                        sb.Append(current);
                    }
                    continue;
                }

                if (inMultiLineComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inMultiLineComment = false;
                        i++;
                    }
                    continue;
                }

                if (inString)
                {
                    sb.Append(current);

                    if (isEscaped)
                    {
                        isEscaped = false;
                    }
                    else if (current == '\\')
                    {
                        isEscaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    sb.Append(current);
                    continue;
                }

                if (current == '/' && next == '/')
                {
                    inSingleLineComment = true;
                    i++;
                    continue;
                }

                if (current == '/' && next == '*')
                {
                    inMultiLineComment = true;
                    i++;
                    continue;
                }

                sb.Append(current);
            }

            return sb.ToString();
        }

        public async Task OverwriteBIAFolder(string sourceFolder, string targetFolder, bool actionFinishedAtEnd, CancellationToken ct = default)
        {
            consoleWriter.AddMessageLine("Start overwrite BIA Folder.", "Pink");
            await Task.Run(() =>
            {
                Regex reg = MyRegex();
                string[] biaDirectories = Directory.GetDirectories(sourceFolder, "bia-*", SearchOption.AllDirectories);
                foreach (string biaDirectory in biaDirectories)
                {
                    string relativePath = biaDirectory[sourceFolder.Length..];
                    Match matchBia = reg.Match(relativePath);

                    // treat only root folder
                    if (matchBia.Length == 0)
                    {
                        string targetDirectory = targetFolder + relativePath;

                        Directory.Delete(targetDirectory, true);
                        Directory.CreateDirectory(targetDirectory);
                        FileTransform.CopyFilesRecursively(biaDirectory, targetDirectory);
                    }
                }
            }, ct);
            consoleWriter.AddMessageLine("Overwrite BIA Folder finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task ReplaceInFileFromConfig(string projectPath, ProjectParameters projectParameters)
        {
            foreach (FeatureSetting featureSetting in projectParameters.VersionAndOption.FeatureSettings)
            {
                await FileTransform.ReplaceInFileAndFileName(projectPath, "BIAToolkit_FeatureSetting_" + featureSetting.Name ?? featureSetting.DisplayName, featureSetting.IsSelected ? "true" : "false", FileTransform.projectFileExtensions, consoleWriter);
            }
        }

        private static void CleanSln(string projectPath, VersionAndOption versionAndOption)
        {
            if (!string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(versionAndOption?.WorkTemplate?.VersionFolderPath))
            {
                List<string> slnFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetSolution}");
                if (slnFiles.Count == 0) return;

                List<string> templateCsprojFiles = FileHelper.GetFilesFromPathWithExtension(versionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}", projectPath);

                List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetProject}");

                var csprojFilesToRemoves = templateCsprojFiles?.Except(csprojFiles).ToList();

                if (csprojFilesToRemoves.Count == 0) return;

                DotnetHelper.RemoveProjectsFromSolution(slnFiles[0], csprojFilesToRemoves);
            }
        }

        private static void CleanCsProj(string projectPath, List<FeatureSetting> featureSettings)
        {
            List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(projectPath, $"*{FileExtensions.DotNetProject}");
            List<string> tagsToDelete = featureSettings.GetBiaFeatureTagToDeletes();

            foreach (string csprojFile in csprojFiles)
            {
                var xDocument = XDocument.Load(csprojFile);
                XNamespace ns = xDocument.Root.Name.Namespace;
                bool shouldSaveDocument = false;

                // ItemGroup
                var itemGroups = xDocument
                    .Descendants("ItemGroup")
                    .Where(x => x.Attribute("Label")?.Value?.StartsWith(BiaFeatureTag.ItemGroupTag) == true)
                    .ToList();
                if (itemGroups.Count > 0)
                {
                    itemGroups.ForEach(ig => ig.Remove());
                    shouldSaveDocument = true;
                }

                // DefineConstants
                var defineConstants = xDocument
                    .Descendants("DefineConstants")
                    .ToList();
                foreach (XElement defineConstant in defineConstants)
                {
                    if (!string.IsNullOrWhiteSpace(defineConstant.Value))
                    {
                        defineConstant.Value = csprojFile.EndsWith($"{BiaProjectName.Test}{FileExtensions.DotNetProject}") ? "TRACE" : string.Empty;
                        shouldSaveDocument = true;
                    }
                }

                // ProjectReference
                var projectReferences = xDocument
                    .Descendants("ProjectReference")
                    .Where(x => x.Attribute("Condition") is not null)
                    .ToList();
                foreach (XElement projectReference in projectReferences)
                {
                    XAttribute conditionAttribute = projectReference.Attribute("Condition");
                    if (tagsToDelete.Any(conditionAttribute.Value.Contains))
                    {
                        projectReference.Remove();
                    }
                    else
                    {
                        projectReference.SetAttributeValue(conditionAttribute.Name, null);
                    }
                    shouldSaveDocument = true;
                }

                if (shouldSaveDocument)
                {
                    xDocument.Save(csprojFile);
                }
            }
        }

        private static List<string> GetFilesToExcludes(ProjectParameters projectParameters, List<FeatureSetting> settings)
        {
            List<string> tags = settings.GetBiaFeatureTagToDeletes(BiaFeatureTag.ItemGroupTag);

            List<string> filesToExcludes = [];
            List<string> csprojFiles = FileHelper.GetFilesFromPathWithExtension(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath, $"*{FileExtensions.DotNetProject}");
            foreach (string csprojFile in csprojFiles)
            {
                string rootDirectory = Path.GetDirectoryName(csprojFile);

                var document = XDocument.Load(csprojFile);
                XNamespace ns = document.Root.Name.Namespace;

                var itemGroups = document.Descendants(ns + "ItemGroup").Where(x => tags.Contains((string)x.Attribute("Label"))).ToList();
                foreach (XElement itemGroup in itemGroups)
                {
                    List<string> compileRemoveItems = [.. itemGroup.Elements(ns + "Compile")
                                                      .Where(x => x.Attribute("Remove") != null)
                                                      .Select(x => x.Attribute("Remove").Value)];

                    foreach (string item in compileRemoveItems)
                    {
                        string newPattern;
                        if (item.Contains("**\\*"))
                        {
                            newPattern = @"\\.*\\.*" + Regex.Escape(item.Replace("**\\*", "").Replace(".cs", "").Replace("*", "")) + ".*\\.cs$";
                        }
                        else
                        {
                            newPattern = @"\\.*\\" + Regex.Escape(item.Replace("**\\", "").Replace(".cs", "")) + @"\.cs$";
                        }

                        string fullPattern =
                            @"^.*\\"
                            + rootDirectory
                                .Replace(Path.GetDirectoryName(projectParameters.VersionAndOption.WorkTemplate.VersionFolderPath) + @"\", string.Empty)
                                .Replace(@"\", @"\\")
                                .Replace(".", "\\.")
                            + newPattern;

                        if (!filesToExcludes.Contains(fullPattern))
                        {
                            filesToExcludes.Add(fullPattern);
                        }
                    }
                }
            }

            return filesToExcludes;
        }

        private static void CleanProject(string projectPath, VersionAndOption versionAndOption, List<FeatureSetting> featureSettings)
        {
            if (!versionAndOption.FeatureSettings.ToList().HasAllFeature())
            {
                CleanSln(projectPath, versionAndOption);
                CleanCsProj(projectPath, featureSettings);

                DirectoryHelper.DeleteEmptyDirectories(projectPath);

                List<string> tagToDeletes = featureSettings.GetBiaFeatureTagToDeletes();
                FileHelper.CleanFilesByTag(projectPath, tagToDeletes, "#if", "#else", "#endif", $"*{FileExtensions.DotNetClass}", true);
                FileHelper.CleanFilesByTag(projectPath, tagToDeletes, "// if", null, "// endif", $"*{FileExtensions.Json}", true);
            }

            List<string> tags = featureSettings.GetAllBiaFeatureTag();
            FileHelper.CleanFilesByTag(projectPath, tags, "#if", "#else", "#endif", $"*{FileExtensions.DotNetClass}", false);
            FileHelper.CleanFilesByTag(projectPath, tags, "// if", null, "// endif", $"*{FileExtensions.Json}", false);
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

            foreach (string file in Directory.GetFiles(rootDirectory, filePattern, SearchOption.AllDirectories))
            {
                consoleWriter.AddMessageLine($"-> Delete {file}", "orange");
                File.Delete(file);
            }
        }

        [GeneratedRegex(@"\\bia-.*\\", RegexOptions.IgnoreCase, "fr-FR")]
        private static partial Regex MyRegex();
    }
}
