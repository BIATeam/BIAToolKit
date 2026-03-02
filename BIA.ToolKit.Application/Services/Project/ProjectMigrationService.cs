namespace BIA.ToolKit.Application.Services.ProjectMigration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;

    /// <summary>
    /// Service implementation for project migration operations.
    /// Encapsulates business logic extracted from ModifyProjectViewModel.
    /// </summary>
    public class ProjectMigrationService : IProjectMigrationService
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;
        private readonly IGitService gitService;
        private readonly IProjectCreatorService projectCreatorService;
        private readonly CRUDSettings crudSettings;

        public Project CurrentProject { get; set; }

        public ProjectMigrationService(
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            SettingsService settingsService,
            CSharpParserService parserService,
            IGitService gitService,
            IProjectCreatorService projectCreatorService)
        {
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.projectCreatorService = projectCreatorService ?? throw new ArgumentNullException(nameof(projectCreatorService));
            this.crudSettings = new CRUDSettings(settingsService);
        }

        /// <inheritdoc/>
        public async Task LoadProjectAsync(Project project)
        {
            try
            {
                consoleWriter.AddMessageLine($"Loading project {project.Name}", "pink");

                consoleWriter.AddMessageLine("List project's files...", "darkgray");
                await project.ListProjectFiles();
                project.SolutionPath = project.ProjectFiles.FirstOrDefault(path => 
                    path.EndsWith($"{project.Name}.sln", StringComparison.InvariantCultureIgnoreCase));
                consoleWriter.AddMessageLine("Project's files listed", "lightgreen");

                consoleWriter.AddMessageLine("Resolving names and version...", "darkgray");
                
                NamesAndVersionResolver nvResolverOldVersions = new()
                {
                    ConstantFileRegExpPath = @"\\.*\\(.*)\.(.*)\.Common\\Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath = null,
                    FrontFileUsingBiaNg = null,
                    FrontFileBiaNgImportRegExp = null,
                    FrontFileNameSearchPattern = null
                };

                NamesAndVersionResolver nvResolver = new()
                {
                    ConstantFileRegExpPath = @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath =
                    [
                        @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$",
                        @"\\(.*)\\packages\\bia-ng\\core\\bia-core.module\.ts$"
                    ],
                    FrontFileUsingBiaNg = @"\\(?!.*(?:\\node_modules\\|\\dist\\|\\\.angular\\))(.*)\\package\.json$",
                    FrontFileBiaNgImportRegExp = "\"bia-ng\":",
                    FrontFileNameSearchPattern = "bia-core.module.ts"
                };

                var resolverTask = Task.Run(() => nvResolver.ResolveNamesAndVersion(project));
                var resolverOldVersionsTask = Task.Run(() => nvResolverOldVersions.ResolveNamesAndVersion(project));
                await Task.WhenAll(resolverTask, resolverOldVersionsTask);

                consoleWriter.AddMessageLine("Names and version resolved", "lightgreen");

                if (project.BIAFronts.Count == 0)
                {
                    consoleWriter.AddMessageLine("Unable to find any BIA front folder for this project", "orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project : {ex.Message}", "red");
            }
        }

        /// <inheritdoc/>
        public async Task ParseProjectAsync(Project project)
        {
            try
            {
                await parserService.LoadSolution(project.SolutionPath);
                await parserService.ParseSolutionClasses();
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateAsync(MigrationRequest request)
        {
            var result = new MigrationResult();
            
            result.GenerateResult = await GenerateOnlyAsync(request);
            result.ApplyDiffResult = await ApplyDiffAsync(request);
            
            if (result.GenerateResult == 0 && result.ApplyDiffResult)
            {
                await MergeRejectedAsync(request);
            }

            result.Success = result.GenerateResult == 0 && result.ApplyDiffResult;
            return result;
        }

        /// <inheritdoc/>
        public async Task<int> GenerateOnlyAsync(MigrationRequest request)
        {
            if (request.Project == null)
            {
                return -1;
            }

            if (!Directory.Exists(request.Project.Folder) || IsDirectoryEmpty(request.Project.Folder))
            {
                return -1;
            }

            PreparePaths(request, out string projectOriginPath, out string projectTargetPath, out _, out _);

            await GenerateProjectsAsync(true, projectOriginPath, projectTargetPath);

            return 0;
        }

        /// <inheritdoc/>
        public async Task<bool> ApplyDiffAsync(MigrationRequest request)
        {
            PreparePaths(request, out string projectOriginPath, out string projectTargetPath, 
                out string projectOriginalFolderName, out string projectTargetFolderName);

            if (request.OverwriteBIAFromOriginal)
            {
                await projectCreatorService.OverwriteBIAFolder(projectOriginPath, request.Project.Folder, false);
            }

            string migrateFilePath = GetMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
            
            if (!await gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath))
            {
                return false;
            }

            if (!await gitService.ApplyDiff(true, request.Project.Folder, migrateFilePath))
            {
                return false;
            }

            await HandleDeletedFilesFailedAsync(request.Project, migrateFilePath, projectOriginalFolderName, projectTargetFolderName);
            return true;
        }

        /// <inheritdoc/>
        public async Task MergeRejectedAsync(MigrationRequest request)
        {
            PreparePaths(request, out string projectOriginPath, out string projectTargetPath,
                out string projectOriginalFolderName, out string projectTargetFolderName);

            await gitService.MergeRejected(true, new GitService.MergeParameter()
            {
                ProjectPath = request.Project.Folder,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = request.OriginVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = request.TargetVersion,
                MigrationPatchFilePath = GetMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName)
            });

            await Task.Run(() =>
            {
                // delete PACKAGE_LOCK_FILE
                foreach (var biaFront in request.Project.BIAFronts)
                {
                    string path = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, 
                        request.Project.Name, biaFront, crudSettings.PackageLockFileName);
                    if (new FileInfo(path).Exists)
                    {
                        File.Delete(path);
                    }
                }

                string rootBiaFolder = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, 
                    request.Project.Name, Constants.FolderBia);
                if (!Directory.Exists(rootBiaFolder))
                {
                    Directory.CreateDirectory(rootBiaFolder);
                }

                var fileToSuppress = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, 
                    request.Project.Name, FeatureSettingHelper.fileName);
                if (File.Exists(fileToSuppress))
                {
                    File.Delete(fileToSuppress);
                }

                var fileToCheck = Path.Combine(rootBiaFolder, settingsService.ReadSetting("ProjectGeneration"));
                if (!File.Exists(fileToCheck))
                {
                    var fileToCopy = Path.Combine(projectTargetPath, Constants.FolderBia, 
                        settingsService.ReadSetting("ProjectGeneration"));
                    File.Copy(fileToCopy, fileToCheck);
                }
            });
        }

        /// <inheritdoc/>
        public async Task OverwriteBIAFolderAsync(MigrationRequest request)
        {
            PreparePaths(request, out _, out string projectTargetPath, out _, out _);
            await projectCreatorService.OverwriteBIAFolder(projectTargetPath, request.Project.Folder, true);
        }

        /// <inheritdoc/>
        public async Task FixUsingsAsync()
        {
            await parserService.FixUsings();
        }

        /// <inheritdoc/>
        public string GetMigrationPatchFilePath(string originalFolderName, string targetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{originalFolderName}-{targetFolderName}.patch";
        }

        private void PreparePaths(MigrationRequest request, out string projectOriginPath, out string projectTargetPath,
            out string projectOriginalFolderName, out string projectTargetFolderName)
        {
            projectOriginalFolderName = request.Project.Name + "_" + request.OriginVersion + "_From";
            projectOriginPath = AppSettings.TmpFolderPath + projectOriginalFolderName;

            projectTargetFolderName = request.Project.Name + "_" + request.TargetVersion + "_To";
            projectTargetPath = AppSettings.TmpFolderPath + projectTargetFolderName;
        }

        private async Task GenerateProjectsAsync(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            // Create project at original version.
            if (Directory.Exists(projectOriginPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath));
            }

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath));
            }

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task HandleDeletedFilesFailedAsync(Project currentProject, 
            string migrateFilePath, string projectOriginalFolder, string projectTargetFolder)
        {
            var migrateFileContent = (await File.ReadAllLinesAsync(migrateFilePath)).ToList();
            var deleteFileInstructionIndexes = migrateFileContent
                .Select((line, index) => new { line, index })
                .Where(x => x.line.StartsWith("deleted file mode"))
                .Select(x => x.index)
                .ToList();

            if (deleteFileInstructionIndexes.Count == 0)
                return;

            consoleWriter.AddMessageLine("Verify expected deleted files", "pink");
            var filesToDelete = deleteFileInstructionIndexes
                .Select(index =>
                {
                    var diffInstruction = migrateFileContent.ElementAt(index - 1);
                    var match = Regex.Match(diffInstruction, @"\sb/(.+)$");
                    return match.Success 
                        ? Path.Combine(currentProject.Folder, match.Groups[1].Value).Replace("/", "\\") 
                        : null;
                })
                .Where(x => x != null)
                .ToList();

            var hasNotDeletedFiles = false;
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    var originalFile = Path.Combine(AppSettings.TmpFolderPath, 
                        file.Replace(currentProject.Folder, projectOriginalFolder));
                    consoleWriter.AddMessageLine($"File not deleted : {file}", "orange", false);
                    consoleWriter.AddMessageLine($"code --diff {originalFile} {file}", "gray", false);
                    hasNotDeletedFiles = true;
                }
            }

            if (hasNotDeletedFiles)
            {
                consoleWriter.AddMessageLine(
                    "Some files have not been deleted. Check the previous details to launch diff command for each of them. Delete them manually if applicable.", 
                    "orange");
            }
        }

        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
