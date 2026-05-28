namespace BIA.ToolKit.ViewModels
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
    using System.Threading;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public partial class ModifyProjectViewModel : ObservableObject, IDisposable,
        IRecipient<SolutionClassesParsedMessage>,
        IRecipient<ProjectChangedMessage>
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;
        private readonly GitService gitService;
        private readonly ProjectCreatorService projectCreatorService;
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
            ProjectCreatorService projectCreatorService)
        {
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.projectCreatorService = projectCreatorService ?? throw new ArgumentNullException(nameof(projectCreatorService));

            this.crudSettings = new CRUDSettings(settingsService);
            ModifyProject = new ModifyProject();
            OverwriteBIAFromOriginal = true;

            WeakReferenceMessenger.Default.RegisterAll(this);

            Steps = new ObservableCollection<MigrationStep>
            {
                new(1, "Generate Only",       MigrateGenerateOnlyCommand),
                new(2, "Open Folder",         MigrateOpenFolderCommand),
                new(3, "Apply Diff",          MigrateApplyDiffCommand),
                new(4, "Merge Rejected",      MigrateMergeRejectedCommand),
                new(5, "Overwrite \\BIA-.*",  MigrateOverwriteBIAFolderCommand),
                new(6, "Fix Usings",          FixUsingsCommand),
            };
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(SolutionClassesParsedMessage message) => EventBroker_OnSolutionClassesParsed();

        /// <summary>
        /// Mirror the project selected in the shared <see cref="ProjectViewModel"/>
        /// (which is the singleton bound to <c>ProjectSelectorUC</c>).
        /// Sets the underlying model field directly to avoid re-broadcasting
        /// <see cref="ProjectChangedMessage"/> (which would loop back to the
        /// other generator VMs that already received the original message).
        /// </summary>
        public void Receive(ProjectChangedMessage message)
        {
            var project = message?.Project;
            if (ModifyProject.CurrentProject == project)
                return;

            ModifyProject.CurrentProject = project;
            ResetMigrationStepStates();
            RefreshUI();
        }

        // --- Event broker handlers ---

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
            if (Steps is null) return;          // Receive() can fire before ctor body completes
            foreach (var s in Steps) s.Reset();
        }

        // --- Migration steps (V2.14.0 stepper) ---

        /// <summary>
        /// The six steps backing the <c>MigrationStepperUC</c>. Order matters:
        /// each step's downstream peers are reset to Pending when it is
        /// (re-)executed via <see cref="ResetStepsFrom(int)"/>.
        /// </summary>
        public ObservableCollection<MigrationStep> Steps { get; }

        private MigrationStep Step(int n) => Steps[n - 1];

        /// <summary>
        /// Resets the status of every step strictly after <paramref name="number"/>
        /// to <see cref="MigrationStepStatus.Pending"/>. Called when a step is
        /// (re-)triggered so the visual chain reflects the new starting point.
        /// </summary>
        private void ResetStepsFrom(int number)
        {
            foreach (var s in Steps.Where(s => s.Number > number))
                s.Reset();
        }

        // Used by ConsoleWriter.RefreshStepColors to resolve a step's current
        // status when re-rendering output stripes.
        private MigrationStepStatus? StepStatusResolver(int number)
        {
            if (Steps is null || number < 1 || number > Steps.Count) return null;
            return Steps[number - 1].Status;
        }

        // --- Core properties ---

        public ModifyProject ModifyProject { get; set; }

        /// <summary>
        /// Re-evaluates UI state that depends on <see cref="CurrentProject"/>.
        /// Project loading/parsing and the related compatibility flags live in
        /// <see cref="ProjectViewModel"/>; here we only need to refresh the
        /// notifications consumed by ModifyProjectUC bindings and reset the
        /// active tab if the file generator is no longer initialized.
        /// </summary>
        private void RefreshUI()
        {
            OnPropertyChanged(nameof(IsProjectSelected));
            OnPropertyChanged(nameof(IsTabFeaturesEnabled));

            bool fileGenInit = fileGeneratorService.IsInit;
            bool crudCompatible = GenerateCrudService.IsProjectCompatible(CurrentProject);
            if ((!crudCompatible && !fileGenInit)
                || (SelectedTabIndex == 2 && !fileGenInit))
            {
                SelectedTabIndex = 0;
            }
        }

        // Used internally by MigratePreparePath / CreateProjectAsync.
        private string Name => ModifyProject.CurrentProject?.Name ?? string.Empty;
        private string CompanyName => ModifyProject.CurrentProject?.CompanyName ?? string.Empty;

        public Project CurrentProject => ModifyProject.CurrentProject;

        public bool OverwriteBIAFromOriginal { get; set; }

        public bool IsProjectSelected => CurrentProject != null;
        public bool IsTabFeaturesEnabled => IsProjectSelected && RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(CurrentProject);

        [ObservableProperty]
        private int selectedTabIndex;

        // --- VersionAndOption initialization ---

        public void InitVersionAndOptionComponents()
        {
            // Run via the busy waiter so we can await SetCurrentProjectPathAsync (which awaits
            // version-folder preparation before loading feature settings). The previous synchronous
            // call ordering raced with the XAML EventTrigger and left Origin features empty.
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                if (OriginVersionAndOptionVM is not null)
                {
                    OriginVersionAndOptionVM.SelectVersion(CurrentProject?.FrameworkVersion);
                    await OriginVersionAndOptionVM.SetCurrentProjectPathAsync(CurrentProject?.Folder, true, true, ct: ct);
                }

                if (TargetVersionAndOptionVM is not null)
                {
                    var originFeatures = CurrentProject is null
                        ? null
                        : OriginVersionAndOptionVM?.FeatureSettings.Select(x => x.FeatureSetting);
                    await TargetVersionAndOptionVM.SetCurrentProjectPathAsync(CurrentProject?.Folder, false, false, originFeatures, ct);
                }
            }));
        }

        // --- Migration commands ---

        [RelayCommand]
        private void Migrate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateRunAsync(ct)));
        }

        private async Task MigrateRunAsync(CancellationToken ct = default)
        {
            var generated = await MigrateGenerateOnlyRunAsync(ct);
            var applyDiff = await MigrateApplyDiffRunAsync(ct);
            if (generated == 0 && applyDiff)
            {
                await MigrateMergeRejectedRunAsync(ct);
            }
        }

        [RelayCommand]
        private void MigrateGenerateOnly()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateGenerateOnlyRunAsync(ct)));
        }

        private async Task<int> MigrateGenerateOnlyRunAsync(CancellationToken ct = default)
        {
            var step = Step(1);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(1);
            using var stepScope = consoleWriter.BeginStep(1, step.Label);
            try
            {
                if (ModifyProject.CurrentProject == null)
                {
                    consoleWriter.AddMessageLine("Select a project before click migrate.", "red");
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = "No project selected";
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    return -1;
                }
                if (!Directory.Exists(ModifyProject.CurrentProject.Folder) || IsDirectoryEmpty(ModifyProject.CurrentProject.Folder))
                {
                    consoleWriter.AddMessageLine("The project path is empty : " + ModifyProject.CurrentProject.Folder, "red");
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = "Project path is empty";
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    return -1;
                }

                MigratePreparePath(out _, out var projectOriginPath, out _, out _, out var projectTargetPath, out _);

                await GenerateProjectsAsync(true, projectOriginPath, projectTargetPath, ct);

                CanOpenFolder = true;
                CanApplyDiff = true;

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                return 0;
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }

        [RelayCommand]
        private void MigrateOpenFolder()
        {
            var step = Step(2);
            try
            {
                ResetStepsFrom(2);
                Process.Start("explorer.exe", AppSettings.TmpFolderPath);
                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
        }

        [RelayCommand]
        private void MigrateApplyDiff()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateApplyDiffRunAsync(ct)));
        }

        private async Task<bool> MigrateApplyDiffRunAsync(CancellationToken ct = default)
        {
            var step = Step(3);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(3);
            using var stepScope = consoleWriter.BeginStep(3, step.Label);
            try
            {
                bool result = false;

                MigratePreparePath(out var projectOriginalFolderName, out var projectOriginPath, out _, out var projectTargetFolderName, out _, out _);

                if (OverwriteBIAFromOriginal == true)
                {
                    await projectCreatorService.OverwriteBIAFolder(projectOriginPath, ModifyProject.CurrentProject.Folder, false, ct);
                }

                result = await ApplyDiffAsync(true, projectOriginalFolderName, projectTargetFolderName, ct);

                CanMergeRejected = true;

                step.Status = result ? MigrationStepStatus.Done : MigrationStepStatus.Warning;
                if (!result) step.LastMessage = "Apply Diff returned false — inspect output";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                return result;
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }

        [RelayCommand]
        private void MigrateMergeRejected()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await MigrateMergeRejectedRunAsync(ct)));
        }

        private async Task MigrateMergeRejectedRunAsync(CancellationToken ct = default)
        {
            var step = Step(4);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(4);
            using var stepScope = consoleWriter.BeginStep(4, step.Label);
            try
            {
                await MergeRejectedAsync(true, ct);

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
                }, ct);

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }

        [RelayCommand]
        private void MigrateOverwriteBIAFolder()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await OverwriteBIAFolderAsync(true, ct)));
        }

        [RelayCommand]
        private void FixUsings()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                var step = Step(6);
                step.Status = MigrationStepStatus.Running;
                ResetStepsFrom(6);
                using var stepScope = consoleWriter.BeginStep(6, step.Label);
                try
                {
                    await parserService.FixUsings(ct);
                    step.Status = MigrationStepStatus.Done;
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                }
                catch (OperationCanceledException)
                {
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = "Cancelled";
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    throw;
                }
                catch (Exception ex)
                {
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = ex.Message;
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    throw;
                }
            }));
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

        private async Task GenerateProjectsAsync(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath, CancellationToken ct = default)
        {
            if (Directory.Exists(projectOriginPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath), ct);
            }

            await CreateProjectAsync(false, CompanyName, Name, projectOriginPath, OriginVersionAndOptionVM, CurrentProject.BIAFronts, ct);

            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath), ct);
            }
            await CreateProjectAsync(false, CompanyName, Name, projectTargetPath, TargetVersionAndOptionVM, CurrentProject.BIAFronts, ct);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        private async Task CreateProjectAsync(bool actionFinishedAtEnd, string companyName, string projectName, string projectPath, VersionAndOptionViewModel versionAndOptionVM, List<string> fronts, CancellationToken ct = default)
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
                },
                ct: ct
            );
        }

        private async Task<bool> ApplyDiffAsync(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName, CancellationToken ct = default)
        {
            string migrateFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
            if (!await gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath, ct))
            {
                return false;
            }
            if (!await gitService.ApplyDiff(actionFinishedAtEnd, ModifyProject.CurrentProject.Folder, migrateFilePath, ct))
            {
                return false;
            }

            await HandleDeletedFilesFailedAsync(ModifyProject.CurrentProject, migrateFilePath, projectOriginalFolderName, projectTargetFolderName, ct);
            return true;
        }

        private async Task HandleDeletedFilesFailedAsync(Project currentProject, string migrateFilePath, string projectOriginalFolder, string projectTargetFolder, CancellationToken ct = default)
        {
            var migrateFileContent = (await File.ReadAllLinesAsync(migrateFilePath, ct)).ToList();
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

        private async Task MergeRejectedAsync(bool actionFinishedAtEnd, CancellationToken ct = default)
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
            }, ct);
        }

        private static string GenerateMigrationPatchFilePath(string projectOriginalFolderName, string projectTargetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
        }

        private async Task OverwriteBIAFolderAsync(bool actionFinishedAtEnd, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var step = Step(5);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(5);
            using var stepScope = consoleWriter.BeginStep(5, step.Label);
            try
            {
                MigratePreparePath(out _, out _, out _, out _, out var projectTargetPath, out _);
                await projectCreatorService.OverwriteBIAFolder(projectTargetPath, ModifyProject.CurrentProject.Folder, actionFinishedAtEnd, ct);

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }

        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
