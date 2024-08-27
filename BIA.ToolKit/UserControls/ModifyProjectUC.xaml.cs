namespace BIA.ToolKit.UserControls
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common.Extensions;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        BIATKSettings settings;
        ModifyProjectViewModel _viewModel;
        IConsoleWriter consoleWriter;
        RepositoryService repositoryService;
        GitService gitService;
        CSharpParserService cSharpParserService;
        ProjectCreatorService projectCreatorService;
        CRUDSettings crudSettings;
        FeatureSettingService featureSettingService;

        public ModifyProjectUC()
        {
            InitializeComponent();
            _viewModel = (ModifyProjectViewModel)base.DataContext;
            _viewModel.RootProjectsPath = Settings.Default.CreateProjectRootFolderText;
        }

        public void Inject(BIATKSettings settings, RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService, FeatureSettingService featureSettingService)
        {
            this.settings = settings;
            this.repositoryService = repositoryService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            this.featureSettingService = featureSettingService;
            MigrateOriginVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService);
            MigrateTargetVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService);
            CRUDGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter);
            this.crudSettings = new(settingsService);
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

        private void ModifyProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ParameterModifyChange();
            if (_viewModel.ModifyProject.CurrentProject != null)
            {
                _viewModel.ModifyProject.CurrentProject.Folder = _viewModel.ModifyProject.RootProjectsPath;
                CRUDGenerator.SetCurrentProject(_viewModel.ModifyProject.CurrentProject);
            }

            // this.LoadFeatureSetting();
        }

        private void Migrate_Click(object sender, RoutedEventArgs e)
        {
            _ = Migrate_Run();
        }
        private async Task Migrate_Run()
        {
            var generated = await MigrateGenerateOnly_Run();
            if (generated == 0)
            {
                MigrateApplyDiff_Run();
                MigrateMergeRejected_Run();
            }
        }

        private void Enable(bool isEnabled)
        {
            //Migrate.IsEnabled = false;
            if (isEnabled)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
            else
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            }
            //TODO recabler
            //MainTab.IsEnabled = isEnabled;

            System.Windows.Forms.Application.DoEvents();
        }

        private void ParameterModifyChange()
        {
            if (MigrateOpenFolder != null) MigrateOpenFolder.IsEnabled = false;
            if (MigrateApplyDiff != null) MigrateApplyDiff.IsEnabled = false;
            if (MigrateMergeRejected != null) MigrateMergeRejected.IsEnabled = false;
        }

        private async void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {
            await MigrateGenerateOnly_Run();
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

            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await GenerateProjects(true, projectOriginPath, projectTargetPath);

            MigrateOpenFolder.IsEnabled = true;
            MigrateApplyDiff.IsEnabled = true;
            Enable(true);
            return 0;
        }

        private void MigrateApplyDiff_Click(object sender, RoutedEventArgs e)
        {
            MigrateApplyDiff_Run();
        }

        private void MigrateApplyDiff_Run()
        {
            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);


            ApplyDiff(true, projectOriginalFolderName, projectTargetFolderName);

            MigrateMergeRejected.IsEnabled = true;
            Enable(true);
        }

        private void MigrateMergeRejected_Click(object sender, RoutedEventArgs e)
        {
            MigrateMergeRejected_Run();
        }

        private void MigrateMergeRejected_Run()
        {
            Enable(false);

            MergeRejected(true);

            MigrateMergeRejected.IsEnabled = false;

            // delete PACKAGE_LOCK_FILE
            string path = Path.Combine(_viewModel.ModifyProject.RootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, _viewModel.ModifyProject.CurrentProject.BIAFronts, crudSettings.PackageLockFileName);
            if (new FileInfo(path).Exists)
            {
                File.Delete(path);
            }

            Enable(true);
        }

        private void MigrateOverwriteBIAFolder_Click(object sender, RoutedEventArgs e)
        {
            Enable(false);

            OverwriteBIAFolder(true);

            Enable(true);
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
                FileTransform.ForceDeleteDirectory(projectOriginPath);
            }

            string[] fronts = new string[0];
            if (_viewModel.BIAFronts != "???" && !string.IsNullOrEmpty(_viewModel.BIAFronts))
            {
                fronts = _viewModel.BIAFronts.Split(", ");
            }

            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectOriginPath, MigrateOriginVersionAndOption, fronts);

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                FileTransform.ForceDeleteDirectory(projectTargetPath);
            }

            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectTargetPath, MigrateTargetVersionAndOption, fronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        //TODO mutualiser avec celle de MainWindows
        private async Task CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, string[] fronts)
        {
            await this.projectCreatorService.Create(actionFinishedAtEnd, CompanyName, ProjectName, projectPath, versionAndOption.vm.VersionAndOption, fronts);
        }

        private void MigrateOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppSettings.TmpFolderPath);
        }

        private bool ApplyDiff(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
        {
            // Make the differential
            string migrateFilePath = AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
            if (!gitService.DiffFolder(false, AppSettings.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath))
            {
                return false;
            }
            //Apply the differential
            return gitService.ApplyDiff(actionFinishedAtEnd, _viewModel.ModifyProject.CurrentProject.Folder, migrateFilePath);
        }

        private void MergeRejected(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            gitService.MergeRejeted(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = _viewModel.ModifyProject.CurrentProject.Folder,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = projectOriginalVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = projectTargetVersion
            }); ;
        }

        private void OverwriteBIAFolder(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            projectCreatorService.OverwriteBIAFolder(projectTargetPath, _viewModel.ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);

        }

        private void ModifyProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath);
        }

        private void RefreshProjectFolderList_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshProjetsList();
        }

        //private void LoadFeatureSetting()
        //{
        //    FeatureSetting featureSettingTarget = this.featureSettingService.Get(Path.Combine(_viewModel.ModifyProject.CurrentProject.Folder, _viewModel.ModifyProject.CurrentProject.Name));
        //    FeatureSetting featureSettingOrigin = featureSettingTarget.DeepCopy();
        //    ucFeatureTarget.ViewModel = new FeatureSettingVM(featureSettingTarget);
        //    ucFeatureOrigin.ViewModel = new FeatureSettingVM(featureSettingOrigin);
        //}
    }
}
