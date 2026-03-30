namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        ModifyProjectViewModel _viewModel;
        IConsoleWriter consoleWriter;
        GitService gitService;
        CSharpParserService cSharpParserService;
        ProjectCreatorService projectCreatorService;
        CRUDSettings crudSettings;
        UIEventBroker uiEventBroker;
        SettingsService settingsService;
        RegenerateFeaturesDiscoveryService regenerateFeaturesDiscoveryService;
        FeatureMigrationGeneratorService featureMigrationGeneratorService;

        public ModifyProjectViewModel ViewModel => _viewModel;

        public ModifyProjectUC()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker, RegenerateFeaturesDiscoveryService regenerateFeaturesDiscoveryService,
            FeatureMigrationGeneratorService featureMigrationGeneratorService, ProjectViewModel projectViewModel)
        {
            _viewModel = new ModifyProjectViewModel(uiEventBroker);
            this.DataContext = _viewModel;

            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            MigrateOriginVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, uiEventBroker);
            MigrateTargetVersionAndOption.Inject(repositoryService, gitService, consoleWriter, settingsService, uiEventBroker);
            this.regenerateFeaturesDiscoveryService = regenerateFeaturesDiscoveryService;
            this.featureMigrationGeneratorService = featureMigrationGeneratorService;
            this.crudSettings = new(settingsService);
            this.uiEventBroker = uiEventBroker;
            this.settingsService = settingsService;

            ProjectSelector.Inject(projectViewModel);
            ProjectSelector.RootPathTextChanged += (_, _) => ParameterModifyChange();

            RegenerateFeatures.Inject(consoleWriter, uiEventBroker, settingsService,
                regenerateFeaturesDiscoveryService, featureMigrationGeneratorService,
                gitService, cSharpParserService);

            uiEventBroker.OnSolutionClassesParsed += UiEventBroker_OnSolutionClassesParsed;
        }

        private void UiEventBroker_OnSolutionClassesParsed()
        {
            ParameterModifyChange();
            InitVersionAndOptionComponents();
        }

        private bool firstTimeSettingsUpdated = true;
        private void UiEventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            // Settings update is handled by ProjectViewModel directly.
        }

        private void InitVersionAndOptionComponents()
        {
            MigrateOriginVersionAndOption.SelectVersion(_viewModel.CurrentProject?.FrameworkVersion);
            MigrateOriginVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, true, true);
            MigrateTargetVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, false, false,
                _viewModel.CurrentProject is null ?
                null :
                MigrateOriginVersionAndOption.vm.FeatureSettings.Select(x => x.FeatureSetting));
        }

        private void Migrate_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(Migrate_Run);
        }
        private async Task Migrate_Run()
        {
            if (!await MigrateGenerateOnly_Run())
                return;

            if (!await MigrateApplyDiff_Run())
                return;

            await MigrateMergeRejected_Run();
        }

        private void ParameterModifyChange()
        {
            if (MigrateOpenFolder != null) MigrateOpenFolder.IsEnabled = false;
            if (MigrateApplyDiff != null) MigrateApplyDiff.IsEnabled = false;
            if (MigrateMergeRejected != null) MigrateMergeRejected.IsEnabled = false;
        }

        private void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(MigrateGenerateOnly_Run);
        }

        private async Task<bool> MigrateGenerateOnly_Run()
        {
            if (_viewModel.ModifyProject.CurrentProject == null)
            {
                MessageBox.Show("Select a project before click migrate.");
                return false;
            }
            if (!Directory.Exists(_viewModel.ModifyProject.CurrentProject.Folder) || FileDialog.IsDirectoryEmpty(_viewModel.ModifyProject.CurrentProject.Folder))
            {
                MessageBox.Show("The project path is empty : " + _viewModel.ModifyProject.CurrentProject.Folder);
                return false;
            }

            MigratePreparePath(out _, out string projectOriginPath, out string projectOriginalVersion, out _, out string projectTargetPath, out string projectTargetVersion);

            if (!await GenerateProjects(true, projectOriginPath, projectTargetPath))
            {
                consoleWriter.AddMessageLine("Generate projects failed.", "Red");
                return false;
            }

            if (_viewModel.IncludeFeatureMigration && _viewModel.IsProjectCompatibleRegenerateFeatures)
            {
                var entities = regenerateFeaturesDiscoveryService.DiscoverRegenerableEntities(_viewModel.CurrentProject);
                var validEntities = entities.Where(e => e.CanSelectEntity).ToList();

                if (validEntities.Count > 0)
                {
                    await featureMigrationGeneratorService.GenerateFeaturesAsync(
                        _viewModel.CurrentProject, projectOriginPath, projectOriginalVersion, validEntities, cSharpParserService);
                    await featureMigrationGeneratorService.GenerateFeaturesAsync(
                      _viewModel.CurrentProject, projectTargetPath, projectTargetVersion, validEntities, cSharpParserService);
                }
            }

            if (_viewModel.IncludeFeatureMigration && _viewModel.IsProjectCompatibleRegenerateFeatures)
            {
                var entities = regenerateFeaturesDiscoveryService.DiscoverRegenerableEntities(_viewModel.CurrentProject);
                var validEntities = entities.Where(e => e.CanSelectEntity).ToList();

                if (validEntities.Count > 0)
                {
                    await featureMigrationGeneratorService.GenerateFeaturesAsync(
                        _viewModel.CurrentProject, projectOriginPath, projectOriginalVersion, validEntities, cSharpParserService);
                    await featureMigrationGeneratorService.GenerateFeaturesAsync(
                      _viewModel.CurrentProject, projectTargetPath, projectTargetVersion, validEntities, cSharpParserService);
                }
            }

            MigrateOpenFolder.IsEnabled = true;
            MigrateApplyDiff.IsEnabled = true;
            return true;
        }

        private void MigrateApplyDiff_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(MigrateApplyDiff_Run);
        }

        private async Task<bool> MigrateApplyDiff_Run()
        {
            MigratePreparePath(out string projectOriginalFolderName, out string projectOriginPath, out _, out string projectTargetFolderName, out _, out _);

            if (_viewModel.OverwriteBIAFromOriginal == true)
            {
                await projectCreatorService.OverwriteBIAFolder(projectOriginPath, _viewModel.ModifyProject.CurrentProject.Folder, false);
            }

            bool result = await ApplyDiff(true, projectOriginalFolderName, projectTargetFolderName);
            MigrateMergeRejected.IsEnabled = result;
            return result;
        }

        private void MigrateMergeRejected_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(MigrateMergeRejected_Run);
        }

        private async Task MigrateMergeRejected_Run()
        {
            await MergeRejected(true);

            MigrateMergeRejected.IsEnabled = false;

            await Task.Run(() =>
            {
                // delete PACKAGE_LOCK_FILE
                foreach (var biaFront in _viewModel.ModifyProject.CurrentProject.BIAFronts)
                {
                    string path = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, biaFront, crudSettings.PackageLockFileName);
                    if (new FileInfo(path).Exists)
                    {
                        File.Delete(path);
                    }
                }

                string rootBiaFolder = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, Constants.FolderBia);
                if (!Directory.Exists(rootBiaFolder))
                {
                    Directory.CreateDirectory(rootBiaFolder);
                }

                var fileToSuppress = Path.Combine(settingsService.Settings.ModifyProjectRootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, FeatureSettingHelper.fileName);
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

        private void MigrateOverwriteBIAFolder_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(async () => await OverwriteBIAFolder(true));
        }

        private void MigratePreparePath(out string projectOriginalFolderName, out string projectOriginPath, out string projectOriginalVersion, out string projectTargetFolderName, out string projectTargetPath, out string projectTargetVersion)
        {
            projectOriginalVersion = MigrateOriginVersionAndOption.vm.WorkTemplate.Version;
            projectOriginalFolderName = _viewModel.Name + "_" + projectOriginalVersion + "_From";
            projectOriginPath = AppSettings.TmpFolderPath + projectOriginalFolderName;

            projectTargetVersion = MigrateTargetVersionAndOption.vm.WorkTemplate.Version;
            projectTargetFolderName = _viewModel.Name + "_" + projectTargetVersion + "_To";
            projectTargetPath = AppSettings.TmpFolderPath + projectTargetFolderName;
        }

        private async Task<bool> GenerateProjects(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            // Create project at original version.
            if (Directory.Exists(projectOriginPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath));
            }

            consoleWriter.AddMessageLine($"Generate project FROM with version {MigrateOriginVersionAndOption.vm.WorkTemplate.Version}", "Blue");
            if (!await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectOriginPath, MigrateOriginVersionAndOption, _viewModel.CurrentProject.BIAFronts))
            {
                return false;
            }

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath));
            }


            consoleWriter.AddMessageLine($"Generate project TO with version {MigrateTargetVersionAndOption.vm.WorkTemplate.Version}", "Blue");
            if (!await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectTargetPath, MigrateTargetVersionAndOption, _viewModel.CurrentProject.BIAFronts))
            {
                return false;
            }

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
            return true;
        }

        //TODO mutualiser avec celle de MainWindows
        private async Task<bool> CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, List<string> fronts)
        {
            return await this.projectCreatorService.Create(
                actionFinishedAtEnd,
                projectPath,
                new Domain.Model.ProjectParameters
                {
                    CompanyName = CompanyName,
                    ProjectName = ProjectName,
                    VersionAndOption = versionAndOption.vm.VersionAndOption,
                    AngularFronts = fronts
                }
            );
            string filePath = Path.Combine(projectPath, Constants.FolderNetCore, $"{CompanyName}.{ProjectName}.Presentation.Api", "bianetpermissions.json");
            this.projectCreatorService.ClearPermissions(filePath);
        }

        private void MigrateOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppSettings.TmpFolderPath);
        }

        private async Task<bool> ApplyDiff(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
        {
            // Make the differential
            string migrateFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
            if (!await gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath))
            {
                return false;
            }
            //Apply the differential
            if (!await gitService.ApplyDiff(actionFinishedAtEnd, _viewModel.ModifyProject.CurrentProject.Folder, migrateFilePath))
            {
                return false;
            }

            await HandleDeletedFilesFailed(_viewModel.ModifyProject.CurrentProject, migrateFilePath, projectOriginalFolderName, projectTargetFolderName);
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
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await gitService.MergeRejected(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = _viewModel.ModifyProject.CurrentProject.Folder,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = projectOriginalVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = projectTargetVersion,
                MigrationPatchFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName)
            }); ;
        }

        private static string GenerateMigrationPatchFilePath(string projectOriginalFolderName, string projectTargetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
        }

        private async Task OverwriteBIAFolder(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await projectCreatorService.OverwriteBIAFolder(projectTargetPath, _viewModel.ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);
        }

        private void FixUsings_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.RequestExecuteActionWithWaiter(FixUsings_Run);
        }

        private async Task FixUsings_Run()
        {
            await cSharpParserService.FixUsings();
        }
    }
}
