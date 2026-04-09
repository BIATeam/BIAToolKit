namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class ModifyProjectViewModel : ObservableObject, IDisposable,
        IRecipient<SettingsUpdatedMessage>,
        IRecipient<SolutionClassesParsedMessage>
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;
        private readonly GitService gitService;
        private readonly ProjectCreatorService projectCreatorService;
        private readonly IDialogService dialogService;
        private readonly CRUDSettings crudSettings;
        private bool disposed;

        /// <summary>
        /// Child VMs for the two VersionAndOption controls (Origin / Target).
        /// Created by the parent and passed down.
        /// </summary>
        public VersionAndOptionViewModel OriginVersionAndOptionVM { get; set; }
        public VersionAndOptionViewModel TargetVersionAndOptionVM { get; set; }

        public ModifyProjectViewModel(
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            SettingsService settingsService,
            CSharpParserService parserService,
            GitService gitService,
            ProjectCreatorService projectCreatorService,
            IDialogService dialogService)
        {
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.projectCreatorService = projectCreatorService ?? throw new ArgumentNullException(nameof(projectCreatorService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            this.crudSettings = new CRUDSettings(settingsService);
            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(SettingsUpdatedMessage message) => EventBroker_OnSettingsUpdated(message.Settings);
        public void Receive(SolutionClassesParsedMessage message) => EventBroker_OnSolutionClassesParsed();

        // --- Event broker handlers ---

        private bool firstTimeSettingsUpdated = true;
        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            OnPropertyChanged(nameof(RootProjectsPath));
            if (firstTimeSettingsUpdated)
            {
                RefreshProjetsList();
                firstTimeSettingsUpdated = false;
            }
        }

        private void EventBroker_OnSolutionClassesParsed()
        {
            ResetMigrationStepStates();
            InitVersionAndOptionComponents();
        }

        // --- Migration step state ---

        [ObservableProperty]
        private bool canOpenFolder;

        [ObservableProperty]
        private bool canApplyDiff;

        [ObservableProperty]
        private bool canMergeRejected;

        private void ResetMigrationStepStates()
        {
            CanOpenFolder = false;
            CanApplyDiff = false;
            CanMergeRejected = false;
        }

        // --- Core properties ---

        public ModifyProject ModifyProject { get; set; }

        [ObservableProperty]
        private bool isFileGeneratorServiceInit;

        [ObservableProperty]
        private bool isProjectCompatibleCrudGenerator;


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

        [RelayCommand]
        public void RefreshProjetsList()
        {
            List<string> newProjects = null;

            if (!Directory.Exists(RootProjectsPath))
                return;

            DirectoryInfo di = new(RootProjectsPath);
            DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

            newProjects = new();
            foreach (DirectoryInfo dir in versionDirectories)
            {
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

        [RelayCommand]
        private void BrowseRootProjectsFolder()
        {
            RootProjectsPath = dialogService.BrowseFolder(RootProjectsPath, "Choose modify project root path");
        }

        public Dictionary<string, NamesAndVersionResolver> CurrentProjectDetections { get; set; }

        public string Folder
        {
            get { return Path.GetFileName(ModifyProject.CurrentProject?.Folder); }
            set
            {
                if (value == Folder)
                    return;

                WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
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

        private async Task ParseProject(Project currentProject, CancellationToken ct = default)
        {
            try
            {
                await parserService.LoadSolution(currentProject.SolutionPath, ct);
                await parserService.ParseSolutionClasses(ct);
            }
            catch (OperationCanceledException)
            {
                consoleWriter.AddMessageLine("Operation cancelled.", "Yellow");
                throw;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }

        [RelayCommand]
        private void RefreshProjectInformations()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await LoadProject(CurrentProject);
                await InitFileGeneratorServiceFromProject(CurrentProject);
                await ParseProject(CurrentProject, ct);
                RefreshUI();
            }));
        }

        private void RefreshUI()
        {
            OnPropertyChanged(nameof(FrameworkVersion));
            OnPropertyChanged(nameof(CompanyName));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsProjectSelected));
            OnPropertyChanged(nameof(IsTabFeaturesEnabled));
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
                    ResetMigrationStepStates();
                    RefreshUI();
                    WeakReferenceMessenger.Default.Send(new ProjectChangedMessage(value));
                }
            }
        }

        public bool OverwriteBIAFromOriginal { get; set; }

        public bool IsProjectSelected => CurrentProject != null;
        public bool IsTabFeaturesEnabled => IsProjectSelected && RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(CurrentProject);

        [ObservableProperty]
        private int selectedTabIndex;

        // --- VersionAndOption initialization ---

        public void InitVersionAndOptionComponents()
        {
            OriginVersionAndOptionVM?.SelectVersion(CurrentProject?.FrameworkVersion);
            OriginVersionAndOptionVM?.SetCurrentProjectPath(CurrentProject?.Folder, true, true);
            TargetVersionAndOptionVM?.SetCurrentProjectPath(CurrentProject?.Folder, false, false,
                CurrentProject is null ?
                null :
                OriginVersionAndOptionVM?.FeatureSettings.Select(x => x.FeatureSetting));
        }

        // --- Migration commands ---

        [RelayCommand]
        private void Migrate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateRunAsync()));
        }

        private async Task MigrateRunAsync()
        {
            var generated = await MigrateGenerateOnlyRunAsync();
            var applyDiff = await MigrateApplyDiffRunAsync();
            if (generated == 0 && applyDiff)
            {
                await MigrateMergeRejectedRunAsync();
            }
        }

        [RelayCommand]
        private void MigrateGenerateOnly()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateGenerateOnlyRunAsync()));
        }

        private async Task<int> MigrateGenerateOnlyRunAsync()
        {
            if (ModifyProject.CurrentProject == null)
            {
                consoleWriter.AddMessageLine("Select a project before click migrate.", "red");
                return -1;
            }
            if (!Directory.Exists(ModifyProject.CurrentProject.Folder) || IsDirectoryEmpty(ModifyProject.CurrentProject.Folder))
            {
                consoleWriter.AddMessageLine("The project path is empty : " + ModifyProject.CurrentProject.Folder, "red");
                return -1;
            }

            MigratePreparePath(out _, out var projectOriginPath, out _, out _, out var projectTargetPath, out _);

            await GenerateProjectsAsync(true, projectOriginPath, projectTargetPath);

            CanOpenFolder = true;
            CanApplyDiff = true;
            return 0;
        }

        [RelayCommand]
        private void MigrateOpenFolder()
        {
            Process.Start("explorer.exe", AppSettings.TmpFolderPath);
        }

        [RelayCommand]
        private void MigrateApplyDiff()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateApplyDiffRunAsync()));
        }

        private async Task<bool> MigrateApplyDiffRunAsync()
        {
            bool result = false;

            MigratePreparePath(out var projectOriginalFolderName, out var projectOriginPath, out _, out var projectTargetFolderName, out _, out _);

            if (OverwriteBIAFromOriginal == true)
            {
                await projectCreatorService.OverwriteBIAFolder(projectOriginPath, ModifyProject.CurrentProject.Folder, false);
            }

            result = await ApplyDiffAsync(true, projectOriginalFolderName, projectTargetFolderName);

            CanMergeRejected = true;
            return result;
        }

        [RelayCommand]
        private void MigrateMergeRejected()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateMergeRejectedRunAsync()));
        }

        private async Task MigrateMergeRejectedRunAsync()
        {
            await MergeRejectedAsync(true);

            CanMergeRejected = false;

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
                    MigratePreparePath(out _, out _, out _, out _, out var projectTargetPath, out _);
                    var fileToCopy = Path.Combine(projectTargetPath, Constants.FolderBia, settingsService.ReadSetting("ProjectGeneration"));
                    File.Copy(fileToCopy, fileToCheck);
                }
            });
        }

        [RelayCommand]
        private void MigrateOverwriteBIAFolder()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await OverwriteBIAFolderAsync(true)));
        }

        [RelayCommand]
        private void FixUsings()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await parserService.FixUsings()));
        }

        // --- Migration helper methods ---

        private void MigratePreparePath(out string projectOriginalFolderName, out string projectOriginPath, out string projectOriginalVersion, out string projectTargetFolderName, out string projectTargetPath, out string projectTargetVersion)
        {
            projectOriginalVersion = OriginVersionAndOptionVM.WorkTemplate.Version;
            projectOriginalFolderName = Name + "_" + projectOriginalVersion + "_From";
            projectOriginPath = AppSettings.TmpFolderPath + projectOriginalFolderName;

            projectTargetVersion = TargetVersionAndOptionVM.WorkTemplate.Version;
            projectTargetFolderName = Name + "_" + projectTargetVersion + "_To";
            projectTargetPath = AppSettings.TmpFolderPath + projectTargetFolderName;
        }

        private async Task GenerateProjectsAsync(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            if (Directory.Exists(projectOriginPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath));
            }

            await CreateProjectAsync(false, CompanyName, Name, projectOriginPath, OriginVersionAndOptionVM, CurrentProject.BIAFronts);

            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath));
            }
            await CreateProjectAsync(false, CompanyName, Name, projectTargetPath, TargetVersionAndOptionVM, CurrentProject.BIAFronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task CreateProjectAsync(bool actionFinishedAtEnd, string companyName, string projectName, string projectPath, VersionAndOptionViewModel versionAndOptionVM, List<string> fronts)
        {
            await projectCreatorService.Create(
                actionFinishedAtEnd,
                projectPath,
                new ProjectParameters
                {
                    CompanyName = companyName,
                    ProjectName = projectName,
                    VersionAndOption = versionAndOptionVM.VersionAndOption,
                    AngularFronts = fronts
                }
            );
        }

        private async Task<bool> ApplyDiffAsync(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
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
            MigratePreparePath(out var projectOriginalFolderName, out var projectOriginPath, out var projectOriginalVersion, out var projectTargetFolderName, out var projectTargetPath, out var projectTargetVersion);

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
            MigratePreparePath(out _, out _, out _, out _, out var projectTargetPath, out _);
            await projectCreatorService.OverwriteBIAFolder(projectTargetPath, ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);
        }

        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
