namespace BIA.ToolKit.UserControls
{
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
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
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

        public ModifyProjectUC()
        {
            InitializeComponent();
            _viewModel = (ModifyProjectViewModel)base.DataContext;
            _viewModel.RootProjectsPath = Settings.Default.CreateProjectRootFolderText;
        }

        public void Inject(BIATKSettings settings, RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService, FeatureSettingService featureSettingService,
            FileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker)
        {
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            MigrateOriginVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService, settingsService, uiEventBroker);
            MigrateTargetVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService, settingsService, uiEventBroker);
            CRUDGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService);
            OptionGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker, fileGeneratorService);
            DtoGenerator.Inject(cSharpParserService, settingsService, consoleWriter, fileGeneratorService, uiEventBroker);
            this.crudSettings = new(settingsService);
            this.uiEventBroker = uiEventBroker;
            _viewModel.Inject(uiEventBroker, fileGeneratorService, consoleWriter);
            this.settingsService = settingsService;

            uiEventBroker.OnProjectChanged += UiEventBroker_OnProjectChanged;
        }

        private void UiEventBroker_OnProjectChanged(Domain.ModifyProject.Project project)
        {
            ParameterModifyChange();
            MigrateOriginVersionAndOption.SelectVersion(_viewModel.CurrentProject?.FrameworkVersion);
            MigrateOriginVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, true);
            MigrateTargetVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder, false);
        }

        public void RefreshConfiguration()
        {
            MigrateOriginVersionAndOption.refreshConfig();
            MigrateTargetVersionAndOption.refreshConfig();
        }

        private void ModifyProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParameterModifyChange();
        }

        private void Migrate_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(Migrate_Run);
        }
        private async Task Migrate_Run()
        {
            var generated = await MigrateGenerateOnly_Run();
            var applyDiff = await MigrateApplyDiff_Run();
            if (generated == 0 && applyDiff)
            {
                await MigrateMergeRejected_Run();
            }
        }

        private void ParameterModifyChange()
        {
            if (MigrateOpenFolder != null) MigrateOpenFolder.IsEnabled = false;
            if (MigrateApplyDiff != null) MigrateApplyDiff.IsEnabled = false;
            if (MigrateMergeRejected != null) MigrateMergeRejected.IsEnabled = false;
        }

        private void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(MigrateGenerateOnly_Run);
        }

        private async Task<int> MigrateGenerateOnly_Run()
        {
            if (_viewModel.ModifyProject.CurrentProject == null)
            {
                MessageBox.Show("Select a project before click migrate.");
                return -1;
            }
            if (!Directory.Exists(_viewModel.ModifyProject.CurrentProject.Folder) || FileDialog.IsDirectoryEmpty(_viewModel.ModifyProject.CurrentProject.Folder))
            {
                MessageBox.Show("The project path is empty : " + _viewModel.ModifyProject.CurrentProject.Folder);
                return -1;
            }

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await GenerateProjects(true, projectOriginPath, projectTargetPath);

            MigrateOpenFolder.IsEnabled = true;
            MigrateApplyDiff.IsEnabled = true;
            return 0;
        }

        private void MigrateApplyDiff_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(MigrateApplyDiff_Run);
        }

        private async Task<bool> MigrateApplyDiff_Run()
        {
            bool result = false;

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            if (_viewModel.OverwriteBIAFromOriginal == true)
            {
                await projectCreatorService.OverwriteBIAFolder(projectOriginPath, _viewModel.ModifyProject.CurrentProject.Folder, false);
            }

            result = await ApplyDiff(true, projectOriginalFolderName, projectTargetFolderName);

            MigrateMergeRejected.IsEnabled = true;
            return result;
        }

        private void MigrateMergeRejected_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(MigrateMergeRejected_Run);
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
                    string path = Path.Combine(_viewModel.ModifyProject.RootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, biaFront, crudSettings.PackageLockFileName);
                    if (new FileInfo(path).Exists)
                    {
                        File.Delete(path);
                    }
                }

                string rootBiaFolder = Path.Combine(_viewModel.ModifyProject.RootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, Constants.FolderBia);
                if (!Directory.Exists(rootBiaFolder))
                {
                    Directory.CreateDirectory(rootBiaFolder);
                }

                var fileToSuppress = Path.Combine(_viewModel.ModifyProject.RootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, FeatureSettingService.fileName);
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
            uiEventBroker.ExecuteActionWithWaiter(async () => await OverwriteBIAFolder(true));
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

        private async Task GenerateProjects(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            // Create project at original version.
            if (Directory.Exists(projectOriginPath))
            {
               await Task.Run(() => FileTransform.ForceDeleteDirectory(projectOriginPath));
            }

            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectOriginPath, MigrateOriginVersionAndOption, _viewModel.CurrentProject.BIAFronts);

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                await Task.Run(() => FileTransform.ForceDeleteDirectory(projectTargetPath));
            }
            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectTargetPath, MigrateTargetVersionAndOption, _viewModel.CurrentProject.BIAFronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        //TODO mutualiser avec celle de MainWindows
        private async Task CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, List<string> fronts)
        {
            await this.projectCreatorService.Create(
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
                if(match.Success)
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

            if(hasNotDeletedFiles)
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

        private void ModifyProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath);
        }

        private void RefreshProjectFolderList_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshProjetsList();
        }

        private void ResolveUsings_Click(object sender, RoutedEventArgs e)
        {
            uiEventBroker.ExecuteActionWithWaiter(ResolveUsings_Run);
        }

        private async Task ResolveUsings_Run()
        {
            var result = MessageBox.Show("Make sure all Nuget packages have been restored in your solution before running the automatic resolve of usings statement.", "Resolve usings", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.OK)
            {
                await cSharpParserService.ResolveUsings(_viewModel.CurrentProject.SolutionPath);
            }
        }
    }
}
