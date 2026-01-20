namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Application.Messages;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class ModifyProjectViewModel : ObservableObject
    {
        private readonly IMessenger messenger;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;
        private readonly GitService gitService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly CRUDSettings crudSettings;
        private readonly IFileDialogService fileDialogService;
        private bool firstSettingsUpdate = true;

        public event Action SolutionClassesParsed = delegate { };

        public ModifyProjectViewModel(
            IMessenger messenger,
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            SettingsService settingsService,
            CSharpParserService parserService,
            GitService gitService,
            ProjectCreatorService projectCreatorService,
            IFileDialogService fileDialogService)
        {
            this.messenger = messenger;
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
            this.parserService = parserService;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;
            this.fileDialogService = fileDialogService;
            this.crudSettings = new CRUDSettings(settingsService);

            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;

            // Initialize commands - wrap with ExecuteActionWithWaiterMessage for UI feedback
            MigrateCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(MigrateAsync)));
            MigrateGenerateOnlyCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(async () => await MigrateGenerateOnlyAsync())));
            MigrateApplyDiffCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(async () => await MigrateApplyDiffAsync())));
            MigrateMergeRejectedCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(MigrateMergeRejectedAsync)));
            MigrateOverwriteBIAFolderCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(MigrateOverwriteBIAFolderAsync)));
            MigrateOpenFolderCommand = new RelayCommand(MigrateOpenFolder);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            RefreshProjectFolderListCommand = new RelayCommand(RefreshProjetsList);
            FixUsingsCommand = new RelayCommand(() => messenger.Send(new ExecuteActionWithWaiterMessage(FixUsingsAsync)));

            messenger.Register<SettingsUpdatedMessage>(this, (_, m) => OnSettingsUpdated(m.Settings));
            messenger.Register<SolutionClassesParsedMessage>(this, (_, __) => OnSolutionClassesParsed());
        }

        private void OnSettingsUpdated(IBIATKSettings settings)
        {
            OnPropertyChanged(nameof(RootProjectsPath));
            if (firstSettingsUpdate)
            {
                RefreshProjetsList();
                firstSettingsUpdate = false;
            }
        }

        private void OnSolutionClassesParsed()
        {
            SolutionClassesParsed?.Invoke();
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand(RefreshProjectInformations);
        public IRelayCommand MigrateCommand { get; }
        public IRelayCommand MigrateGenerateOnlyCommand { get; }
        public IRelayCommand MigrateApplyDiffCommand { get; }
        public IRelayCommand MigrateMergeRejectedCommand { get; }
        public IRelayCommand MigrateOverwriteBIAFolderCommand { get; }
        public IRelayCommand MigrateOpenFolderCommand { get; }
        public IRelayCommand BrowseFolderCommand { get; }
        public IRelayCommand RefreshProjectFolderListCommand { get; }
        public IRelayCommand FixUsingsCommand { get; }
        public ModifyProject ModifyProject { get; set; }

        private bool _isFileGeneratorServiceInit;
        public bool IsFileGeneratorServiceInit
        {
            get => _isFileGeneratorServiceInit;
            set
            {
                _isFileGeneratorServiceInit = value;
                OnPropertyChanged(nameof(IsFileGeneratorServiceInit));
            }
        }

        private bool isProjectCompatibleCrudGenerator;

        public bool IsProjectCompatibleCrudGenerator
        {
            get { return isProjectCompatibleCrudGenerator; }
            set
            {
                isProjectCompatibleCrudGenerator = value;
                OnPropertyChanged(nameof(IsProjectCompatibleCrudGenerator));
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
                    OnPropertyChanged(nameof(Projects));
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

                messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
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

                    OnPropertyChanged(nameof(Folder));
                }));
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
            messenger.Send(new ExecuteActionWithWaiterMessage(async () =>
            {
                await LoadProject(CurrentProject);
                await InitFileGeneratorServiceFromProject(CurrentProject);
                await ParseProject(CurrentProject);
                RefreshUI();
            }));
        }

        private void RefreshUI()
        {
            OnPropertyChanged(nameof(FrameworkVersion));
            OnPropertyChanged(nameof(CompanyName));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsProjectSelected));
            OnPropertyChanged(nameof(BIAFronts));
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
                    messenger.Send(new ProjectChangedMessage(value));
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
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        // Command implementations
        private async Task MigrateAsync()
        {
            var generated = await MigrateGenerateOnlyAsync();
            var applyDiff = await MigrateApplyDiffAsync();
            if (generated == 0 && applyDiff)
            {
                await MigrateMergeRejectedAsync();
            }
        }

        private async Task<int> MigrateGenerateOnlyAsync()
        {
            if (ModifyProject.CurrentProject == null)
            {
                System.Windows.MessageBox.Show("Select a project before click migrate.");
                return -1;
            }
            if (!Directory.Exists(ModifyProject.CurrentProject.Folder) || fileDialogService.IsDirectoryEmpty(ModifyProject.CurrentProject.Folder))
            {
                System.Windows.MessageBox.Show("The project path is empty : " + ModifyProject.CurrentProject.Folder);
                return -1;
            }

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await GenerateProjectsAsync(true, projectOriginPath, projectTargetPath);

            // Enable buttons via properties
            OnPropertyChanged(nameof(CanOpenFolder));
            OnPropertyChanged(nameof(CanApplyDiff));
            return 0;
        }

        private async Task<bool> MigrateApplyDiffAsync()
        {
            bool result = false;

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            if (OverwriteBIAFromOriginal == true)
            {
                await projectCreatorService.OverwriteBIAFolder(projectOriginPath, ModifyProject.CurrentProject.Folder, false);
            }

            result = await ApplyDiffAsync(true, projectOriginalFolderName, projectTargetFolderName);

            OnPropertyChanged(nameof(CanMergeRejected));
            return result;
        }

        private async Task MigrateMergeRejectedAsync()
        {
            await MergeRejectedAsync(true);

            OnPropertyChanged(nameof(CanMergeRejected));

            await Task.Run(() =>
            {
                // delete PACKAGE_LOCK_FILE
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
                    string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
                    MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);
                    var fileToCopy = Path.Combine(projectTargetPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));

                    File.Copy(fileToCopy, fileToCheck);
                }
            });
        }

        private async Task MigrateOverwriteBIAFolderAsync()
        {
            await OverwriteBIAFolderAsync(true);
        }

        private void MigrateOpenFolder()
        {
            Process.Start("explorer.exe", AppSettings.TmpFolderPath);
        }

        private void BrowseFolder()
        {
            var selectedPath = fileDialogService.BrowseFolder(
                RootProjectsPath,
                "Choose modify project root path");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                RootProjectsPath = selectedPath;
            }
        }

        private async Task FixUsingsAsync()
        {
            await parserService.FixUsings();
        }

        // Helper methods
        private void MigratePreparePath(out string projectOriginalFolderName, out string projectOriginPath, out string projectOriginalVersion, out string projectTargetFolderName, out string projectTargetPath, out string projectTargetVersion)
        {
            projectOriginalVersion = GetOriginVersion?.Invoke() ?? "TBD";
            projectOriginalFolderName = Name + "_" + projectOriginalVersion + "_From";
            projectOriginPath = AppSettings.TmpFolderPath + projectOriginalFolderName;

            projectTargetVersion = GetTargetVersion?.Invoke() ?? "TBD";
            projectTargetFolderName = Name + "_" + projectTargetVersion + "_To";
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

        private async Task<bool> ApplyDiffAsync(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
        {
            // Make the differential
            string migrateFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
            if (!await gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath))
            {
                return false;
            }
            //Apply the differential
            if (!await gitService.ApplyDiff(actionFinishedAtEnd, ModifyProject.CurrentProject.Folder, migrateFilePath))
            {
                return false;
            }

            await HandleDeletedFilesFailedAsync(ModifyProject.CurrentProject, migrateFilePath, projectOriginalFolderName, projectTargetFolderName);
            return true;
        }

        private async Task HandleDeletedFilesFailedAsync(Project currentProject, string migrateFilePath, string projectOriginalFolder, string projectTargetFolder)
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

        private async Task MergeRejectedAsync(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await gitService.MergeRejected(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = ModifyProject.CurrentProject.Folder,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = projectOriginalVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = projectTargetVersion,
                MigrationPatchFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName)
            });
        }

        private static string GenerateMigrationPatchFilePath(string projectOriginalFolderName, string projectTargetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
        }

        private async Task OverwriteBIAFolderAsync(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await projectCreatorService.OverwriteBIAFolder(projectTargetPath, ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);
        }

        // Properties for button enablement
        public bool CanOpenFolder => ModifyProject.CurrentProject != null;
        public bool CanApplyDiff => ModifyProject.CurrentProject != null;
        public bool CanMergeRejected => ModifyProject.CurrentProject != null;

        // Properties for version/option access (set by code-behind)
        public Func<string> GetOriginVersion { get; set; }
        public Func<string> GetTargetVersion { get; set; }

        /// <summary>
        /// Initialize the VersionAndOption controls with current project information
        /// Called from ModifyProjectUC when solution classes are parsed
        /// </summary>
        public void InitializeVersionAndOption(
            VersionAndOptionViewModel originVersionControl, 
            VersionAndOptionViewModel targetVersionControl)
        {
            if (CurrentProject is null)
                return;

            // Initialize origin (old version)
            originVersionControl.SelectVersion(CurrentProject.FrameworkVersion);
            originVersionControl.SetCurrentProjectPath(CurrentProject.Folder, true, true);

            // Initialize target (new version) with origin feature settings
            var originFeatureSettings = originVersionControl.FeatureSettings?.Select(x => x.FeatureSetting);
            targetVersionControl.SetCurrentProjectPath(
                CurrentProject.Folder, 
                false, 
                false, 
                originFeatureSettings);

            // Wire version accessors for migration operations
            // These are used by MigratePreparePath to determine folder names
            GetOriginVersion = () => originVersionControl.WorkTemplate?.Version ?? "TBD";
            GetTargetVersion = () => targetVersionControl.WorkTemplate?.Version ?? "TBD";
        }

    }}
