namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class ModifyProjectViewModel : ViewModelBase
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;
        private readonly GitService gitService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly CRUDSettings crudSettings;

        private bool firstTimeSettingsUpdated = true;

        public ModifyProjectViewModel(IMessenger messenger, FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter, SettingsService settingsService, CSharpParserService parserService, GitService gitService, ProjectCreatorService projectCreatorService)
            : base(messenger)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
            this.parserService = parserService;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;
            this.crudSettings = new CRUDSettings(settingsService);

            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;
        }

        public VersionAndOptionViewModel MigrateOriginVm { get; set; }
        public VersionAndOptionViewModel MigrateTargetVm { get; set; }
        public Action OnSolutionParsedAction { get; set; }

        /// <inheritdoc/>
        public override void Initialize()
        {
            Messenger.Subscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
            Messenger.Subscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            Messenger.Unsubscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
            Messenger.Unsubscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        private void OnSettingsUpdated(SettingsUpdatedMessage message)
        {
            if (firstTimeSettingsUpdated)
            {
                RefreshProjetsList();
                firstTimeSettingsUpdated = false;
            }

            RaisePropertyChanged(nameof(RootProjectsPath));
        }

        private void OnSolutionParsed(SolutionParsedMessage msg)
        {
            ParameterModifyChange();
            OnSolutionParsedAction?.Invoke();
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand((_) => RefreshProjectInformations());
        public ICommand MigrateCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = MigrateAsync }));
        public ICommand MigrateGenerateOnlyCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = async () => await MigrateGenerateOnlyAsync() }));
        public ICommand MigrateApplyDiffCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = async () => await MigrateApplyDiffAsync() }));
        public ICommand MigrateMergeRejectedCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = MigrateMergeRejectedAsync }));
        public ICommand MigrateOverwriteBIAFolderCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = MigrateOverwriteBIAFolderAsync }));
        public ICommand FixUsingsCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = FixUsingsAsync }));
        public ICommand OpenMigrateFolderCommand => new RelayCommand(_ =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", AppSettings.TmpFolderPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening folder: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });
        public ICommand RefreshProjectsListCommand => new RelayCommand(_ => RefreshProjetsList());
        public ModifyProject ModifyProject { get; set; }

        private bool _isFileGeneratorServiceInit;
        public bool IsFileGeneratorServiceInit
        {
            get => _isFileGeneratorServiceInit;
            set
            {
                _isFileGeneratorServiceInit = value;
                RaisePropertyChanged(nameof(IsFileGeneratorServiceInit));
            }
        }

        private bool isProjectCompatibleCrudGenerator;

        public bool IsProjectCompatibleCrudGenerator
        {
            get { return isProjectCompatibleCrudGenerator; }
            set
            {
                isProjectCompatibleCrudGenerator = value;
                RaisePropertyChanged(nameof(IsProjectCompatibleCrudGenerator));
            }
        }


        private ObservableCollection<string> projects = [];
        public ObservableCollection<string> Projects
        {
            get => projects;
            set
            {
                if (projects != value)
                {
                    projects = value;
                    ModifyProject.Projects = [.. value];
                    RaisePropertyChanged(nameof(Projects));
                }
            }
        }

        public void RefreshProjetsList()
        {
            List<string> newProjects = null;

            if (!Directory.Exists(RootProjectsPath))
                return;

            DirectoryInfo di = new(RootProjectsPath);
            // Create an array representing the files in the current directory.
            DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

            newProjects = new();
            foreach (DirectoryInfo dir in versionDirectories)
            {
                //Add and select the last added
                newProjects.Add(dir.Name);
            }

            if (!newProjects.Select(x => Path.Combine(RootProjectsPath, x)).Contains(CurrentProject?.Folder))
            {
                Folder = null;
            }

            for (int i = 0; i < newProjects.Count; i++)
            {
                var existingProjectInNewProjects = Projects.FirstOrDefault(x => x == newProjects[i]);
                if (existingProjectInNewProjects is not null)
                    continue;

                Projects.Insert(i, newProjects[i]);
            }

            for (int i = 0; i < Projects.Count; i++)
            {
                var newProjectInExistingProjects = newProjects.FirstOrDefault(x => x == Projects[i]);
                if (newProjectInExistingProjects is not null)
                    continue;

                Projects.RemoveAt(i);
                i--;
            }
        }

        public string CurrentRootProjectsPath { get; set; }
        public string RootProjectsPath
        {
            get => settingsService?.Settings?.ModifyProjectRootProjectsPath;
            set
            {
                if (settingsService.Settings.ModifyProjectRootProjectsPath != value)
                {
                    CurrentRootProjectsPath = value;
                    settingsService.SetModifyProjectRootProjectPath(value);
                    RefreshProjetsList();
                }
            }
        }

        public Dictionary<string, NamesAndVersionResolver> CurrentProjectDetections { get; set; }

        public string Folder
        {
            get { return Path.GetFileName(ModifyProject.CurrentProject?.Folder); }
            set
            {
                if (value == Folder)
                    return;

                Messenger.Send(new ExecuteWithWaiterMessage
                {
                    Task = async () =>
                    {
                        IsFileGeneratorServiceInit = false;
                        IsProjectCompatibleCrudGenerator = false;

                        Project currentProject = null;
                        if (value is not null)
                        {
                            currentProject = new Project
                            {
                                Name = value,
                                Folder = Path.Combine(RootProjectsPath, value)
                            };
                            await LoadProject(currentProject);
                        }

                        await InitFileGeneratorServiceFromProject(currentProject);
                        CurrentProject = currentProject;

                    if (CurrentProject is not null)
                    {
                        await ParseProject(currentProject);
                    }

                    RaisePropertyChanged(nameof(Folder));
                }
                });
            }
        }

        private async Task InitFileGeneratorServiceFromProject(Project currentProject)
        {
            await fileGeneratorService.Init(currentProject);
            IsFileGeneratorServiceInit = fileGeneratorService.IsInit;
            IsProjectCompatibleCrudGenerator = GenerateCrudService.IsProjectCompatible(currentProject);
        }

        private async Task LoadProject(Project project)
        {
            try
            {
                consoleWriter.AddMessageLine($"Loading project {project.Name}", "pink");

                consoleWriter.AddMessageLine("List project's files...", "darkgray");
                await project.ListProjectFiles();
                project.SolutionPath = project.ProjectFiles.FirstOrDefault(path => path.EndsWith($"{project.Name}.sln", StringComparison.InvariantCultureIgnoreCase));
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
                    FrontFileBiaNgImportRegExp = "\"@bia-team/bia-ng\":",
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

        private async Task ParseProject(Project currentProject)
        {
            try
            {
                await parserService.LoadSolution(currentProject.SolutionPath);
                await parserService.ParseSolutionClasses();
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }

        private void RefreshProjectInformations()
        {
            Messenger.Send(new ExecuteWithWaiterMessage
            {
                Task = async () =>
                {
                    await LoadProject(CurrentProject);
                    await InitFileGeneratorServiceFromProject(CurrentProject);
                    await ParseProject(CurrentProject);
                    RefreshUI();
                }
            });
        }

        private void RefreshUI()
        {
            RaisePropertyChanged(nameof(FrameworkVersion));
            RaisePropertyChanged(nameof(CompanyName));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(IsProjectSelected));
            RaisePropertyChanged(nameof(BIAFronts));
            if ((!IsProjectCompatibleCrudGenerator && !IsFileGeneratorServiceInit) 
                || (SelectedTabIndex == 2 && !IsFileGeneratorServiceInit))
            {
                SelectedTabIndex = 0;
            }
        }

        public string FrameworkVersion
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.FrameworkVersion) ? "???" : ModifyProject.CurrentProject.FrameworkVersion; }
        }

        public string CompanyName
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.CompanyName) ? "???" : ModifyProject.CurrentProject.CompanyName; }
        }

        public string Name
        {
            get { return String.IsNullOrEmpty(ModifyProject.CurrentProject?.Name) ? "???" : ModifyProject.CurrentProject.Name; }
        }

        public string BIAFronts => ModifyProject.CurrentProject?.BIAFronts != null && ModifyProject.CurrentProject?.BIAFronts.Count > 0 ?
            string.Join(", ", ModifyProject.CurrentProject.BIAFronts) :
            "???";

        public Project CurrentProject
        {
            get { return ModifyProject.CurrentProject; }
            set
            {
                if (ModifyProject.CurrentProject != value)
                {
                    ModifyProject.CurrentProject = value;
                    RefreshUI();
                    Messenger.Send(new ProjectChangedMessage { Project = value });
                }
            }
        }

        public bool OverwriteBIAFromOriginal
        {
            get; set;
        }

        public bool IsProjectSelected => CurrentProject != null;

        private int selectedTabIndex = 0;

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set 
            { 
                selectedTabIndex = value;
                RaisePropertyChanged(nameof(SelectedTabIndex));
            }
        }

        private bool canMigrateOpenFolder;
        public bool CanMigrateOpenFolder
        {
            get => canMigrateOpenFolder;
            set
            {
                canMigrateOpenFolder = value;
                RaisePropertyChanged(nameof(CanMigrateOpenFolder));
            }
        }

        private bool canMigrateApplyDiff;
        public bool CanMigrateApplyDiff
        {
            get => canMigrateApplyDiff;
            set
            {
                canMigrateApplyDiff = value;
                RaisePropertyChanged(nameof(CanMigrateApplyDiff));
            }
        }

        private bool canMigrateMergeRejected;
        public bool CanMigrateMergeRejected
        {
            get => canMigrateMergeRejected;
            set
            {
                canMigrateMergeRejected = value;
                RaisePropertyChanged(nameof(CanMigrateMergeRejected));
            }
        }

        public void ParameterModifyChange()
        {
            CanMigrateOpenFolder = false;
            CanMigrateApplyDiff = false;
            CanMigrateMergeRejected = false;
        }

        public async Task MigrateAsync()
        {
            var generated = await MigrateGenerateOnlyAsync();
            var applyDiff = await MigrateApplyDiffAsync();
            if (generated == 0 && applyDiff)
            {
                await MigrateMergeRejectedAsync();
            }
        }

        public async Task<int> MigrateGenerateOnlyAsync()
        {
            if (ModifyProject.CurrentProject == null)
            {
                consoleWriter.AddMessageLine("Select a project before clicking migrate.", "red");
                return -1;
            }
            var projectFolder = ModifyProject.CurrentProject.Folder;
            if (!Directory.Exists(projectFolder) || !Directory.EnumerateFileSystemEntries(projectFolder).Any())
            {
                consoleWriter.AddMessageLine("The project path is empty : " + projectFolder, "red");
                return -1;
            }


            var paths = GetMigratePaths();

            await GenerateProjects(true, paths.OriginPath, paths.TargetPath);

            CanMigrateOpenFolder = true;
            CanMigrateApplyDiff = true;
            return 0;
        }

        public async Task<bool> MigrateApplyDiffAsync()
        {
            var paths = GetMigratePaths();

            if (OverwriteBIAFromOriginal == true)
            {
                await projectCreatorService.OverwriteBIAFolder(paths.OriginPath, ModifyProject.CurrentProject.Folder, false);
            }

            bool result = await ApplyDiff(true, paths.OriginalFolderName, paths.TargetFolderName);

            CanMigrateMergeRejected = true;
            return result;
        }

        public async Task MigrateMergeRejectedAsync()
        {
            await MergeRejected(true);

            CanMigrateMergeRejected = false;

            await Task.Run(() =>
            {
                foreach (var biaFront in ModifyProject.CurrentProject.BIAFronts)
                {
                    string path = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, ModifyProject.CurrentProject.Name, biaFront, crudSettings.PackageLockFileName);
                    if (new FileInfo(path).Exists)
                    {
                        File.Delete(path);
                    }
                }

                string rootBiaFolder = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, ModifyProject.CurrentProject.Name, Constants.FolderBia);
                if (!Directory.Exists(rootBiaFolder))
                {
                    Directory.CreateDirectory(rootBiaFolder);
                }

                var fileToSuppress = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, ModifyProject.CurrentProject.Name, FeatureSettingHelper.fileName);
                if (File.Exists(fileToSuppress))
                {
                    File.Delete(fileToSuppress);
                }

                var fileToCheck = Path.Combine(rootBiaFolder, settingsService.ReadSetting("ProjectGeneration"));
                if (!File.Exists(fileToCheck))
                {
                    var paths = GetMigratePaths();
                    var fileToCopy = Path.Combine(paths.TargetPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
                    File.Copy(fileToCopy, fileToCheck);
                }
            });
        }

        public async Task MigrateOverwriteBIAFolderAsync()
        {
            await OverwriteBIAFolder(true);
        }

        public async Task FixUsingsAsync()
        {
            await parserService.FixUsings();
        }

        private record MigratePaths(
            string OriginalFolderName,
            string OriginPath,
            string OriginalVersion,
            string TargetFolderName,
            string TargetPath,
            string TargetVersion);

        private MigratePaths GetMigratePaths()
        {
            string originalVersion = MigrateOriginVm.WorkTemplate.Version;
            string originalFolderName = Name + "_" + originalVersion + "_From";
            string originPath = AppSettings.TmpFolderPath + originalFolderName;

            string targetVersion = MigrateTargetVm.WorkTemplate.Version;
            string targetFolderName = Name + "_" + targetVersion + "_To";
            string targetPath = AppSettings.TmpFolderPath + targetFolderName;

            return new MigratePaths(originalFolderName, originPath, originalVersion, targetFolderName, targetPath, targetVersion);
        }

        private async Task GenerateProjects(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            if (Directory.Exists(projectOriginPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath));
            }
            await CreateProject(false, CompanyName, Name, projectOriginPath, MigrateOriginVm, ModifyProject.CurrentProject.BIAFronts);

            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath));
            }
            await CreateProject(false, CompanyName, Name, projectTargetPath, MigrateTargetVm, ModifyProject.CurrentProject.BIAFronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task CreateProject(bool actionFinishedAtEnd, string companyName, string projectName, string projectPath, VersionAndOptionViewModel versionAndOptionVm, List<string> fronts)
        {
            await projectCreatorService.Create(
                actionFinishedAtEnd,
                projectPath,
                new Domain.Model.ProjectParameters
                {
                    CompanyName = companyName,
                    ProjectName = projectName,
                    VersionAndOption = versionAndOptionVm.VersionAndOption,
                    AngularFronts = fronts
                }
            );
        }

        private async Task<bool> ApplyDiff(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
        {
            string migrateFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
            if (!await gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath))
            {
                return false;
            }
            if (!await gitService.ApplyDiff(actionFinishedAtEnd, ModifyProject.CurrentProject.Folder, migrateFilePath))
            {
                return false;
            }

            await HandleDeletedFilesFailed(ModifyProject.CurrentProject, migrateFilePath, projectOriginalFolderName, projectTargetFolderName);
            return true;
        }

        private async Task HandleDeletedFilesFailed(Project currentProject, string migrateFilePath, string projectOriginalFolder, string projectTargetFolder)
        {
            var migrateFileContent = (await File.ReadAllLinesAsync(migrateFilePath)).ToList();
            var deleteFileInstructionIndexes = new List<int>();
            for (int i = 0; i < migrateFileContent.Count; i++)
            {
                if (migrateFileContent[i].StartsWith("deleted file mode"))
                    deleteFileInstructionIndexes.Add(i);
            }

            if (deleteFileInstructionIndexes.Count == 0)
                return;

            consoleWriter.AddMessageLine("Verify expected deleted files", "pink");
            var filesToDelete = new List<string>();
            var pathOfFileRegex = @"\sb/(.+)$";
            foreach (var index in deleteFileInstructionIndexes)
            {
                var diffInstruction = migrateFileContent.ElementAt(index - 1);
                var match = Regex.Match(diffInstruction, pathOfFileRegex);
                if (match.Success)
                {
                    filesToDelete.Add(Path.Combine(currentProject.Folder, match.Groups[1].Value).Replace("/", "\\"));
                }
            }

            var hasNotDeletedFiles = false;
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    var originalFile = Path.Combine(AppSettings.TmpFolderPath, file.Replace(currentProject.Folder, projectOriginalFolder));
                    consoleWriter.AddMessageLine($"File not deleted : {file}", "orange", false);
                    consoleWriter.AddMessageLine($"code --diff {originalFile} {file}", "gray", false);
                    hasNotDeletedFiles = true;
                }
            }

            if (hasNotDeletedFiles)
            {
                consoleWriter.AddMessageLine("Some files have not been deleted. Check the previous details to launch diff command for each of them. Delete them manually if applicable.", "orange");
            }
        }

        private async Task MergeRejected(bool actionFinishedAtEnd)
        {
            var paths = GetMigratePaths();

            await gitService.MergeRejected(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = ModifyProject.CurrentProject.Folder,
                ProjectOriginPath = paths.OriginPath,
                ProjectOriginVersion = paths.OriginalVersion,
                ProjectTargetPath = paths.TargetPath,
                ProjectTargetVersion = paths.TargetVersion,
                MigrationPatchFilePath = GenerateMigrationPatchFilePath(paths.OriginalFolderName, paths.TargetFolderName)
            });
        }

        private async Task OverwriteBIAFolder(bool actionFinishedAtEnd)
        {
            var paths = GetMigratePaths();
            await projectCreatorService.OverwriteBIAFolder(paths.TargetPath, ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);
        }

        private static string GenerateMigrationPatchFilePath(string projectOriginalFolderName, string projectTargetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
        }
    }
}
