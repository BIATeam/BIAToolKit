namespace BIA.ToolKit.UserControls
{
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common.Extensions;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;
    using BIA.ToolKit.Services;

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
        UIEventBroker uiEventBroker;

        public ModifyProjectUC()
        {
            InitializeComponent();
            _viewModel = (ModifyProjectViewModel)base.DataContext;
            _viewModel.RootProjectsPath = Settings.Default.CreateProjectRootFolderText;
        }

        public void Inject(BIATKSettings settings, RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService,
            ProjectCreatorService projectCreatorService, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService, FeatureSettingService featureSettingService,
            BiaFrameworkFileGeneratorService fileGeneratorService, UIEventBroker uiEventBroker)
        {
            this.settings = settings;
            this.repositoryService = repositoryService;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            MigrateOriginVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService);
            MigrateTargetVersionAndOption.Inject(settings, repositoryService, gitService, consoleWriter, featureSettingService);
            CRUDGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker);
            OptionGenerator.Inject(cSharpParserService, zipService, crudService, settingsService, consoleWriter, uiEventBroker);
            DtoGenerator.Inject(cSharpParserService, settingsService, consoleWriter, fileGeneratorService, uiEventBroker);
            this.crudSettings = new(settingsService);
            this.uiEventBroker = uiEventBroker;
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
            uiEventBroker.NotifyProjectChanged(_viewModel.CurrentProject);

            ParameterModifyChange();
            MigrateOriginVersionAndOption.SelectVersion(_viewModel.CurrentProject?.FrameworkVersion);
            MigrateOriginVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder);
            MigrateTargetVersionAndOption.SetCurrentProjectPath(_viewModel.CurrentProject?.Folder);
        }

        private void Migrate_Click(object sender, RoutedEventArgs e)
        {
            _ = Migrate_Run();
        }
        private async Task Migrate_Run()
        {
            var generated = await MigrateGenerateOnly_Run();
            if (generated == 0 && MigrateApplyDiff_Run() == true)
            {
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

        private bool MigrateApplyDiff_Run()
        {
            bool result = false;
            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            if (_viewModel.OverwriteBIAFromOriginal == true)
            {
                projectCreatorService.OverwriteBIAFolder(projectOriginPath, _viewModel.ModifyProject.CurrentProject.Folder, false);
            }

            result = ApplyDiff(true, projectOriginalFolderName, projectTargetFolderName);

            MigrateMergeRejected.IsEnabled = true;
            Enable(true);
            return result;
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
            foreach (var biaFront in _viewModel.ModifyProject.CurrentProject.BIAFronts)
            {
                string path = Path.Combine(_viewModel.ModifyProject.RootProjectsPath, _viewModel.ModifyProject.CurrentProject.Name, biaFront, crudSettings.PackageLockFileName);
                if (new FileInfo(path).Exists)
                {
                    File.Delete(path);
                }
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

            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectOriginPath, MigrateOriginVersionAndOption, _viewModel.CurrentProject.BIAFronts);

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                FileTransform.ForceDeleteDirectory(projectTargetPath);
            }
            await CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectTargetPath, MigrateTargetVersionAndOption, _viewModel.CurrentProject.BIAFronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        //TODO mutualiser avec celle de MainWindows
        private async Task CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, List<string> fronts)
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
            string migrateFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName);
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
                ProjectTargetVersion = projectTargetVersion,
                MigrationPatchFilePath = GenerateMigrationPatchFilePath(projectOriginalFolderName, projectTargetFolderName)
            }); ;
        }

        private static string GenerateMigrationPatchFilePath(string projectOriginalFolderName, string projectTargetFolderName)
        {
            return AppSettings.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
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

        private void TabActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            const string TabItem_Migration = "TabMigration";
            const string TabItem_OptionGenerator = "TabOptionGenerator";
            const string TabItem_CrudGenerator = "TabCrudGenerator";
            const string TabItem_DtoGenerator = "TabDtoGenerator";

            if (e.AddedItems.Count == 1 && e.AddedItems[0] is TabItem tabItem)
            {
                switch(tabItem.Name)
                {
                    case TabItem_Migration:
                        uiEventBroker.SetCurrentTabItemModifyProject(UIEventBroker.TabItemModifyProjectEnum.Migration);
                        break;
                    case TabItem_OptionGenerator:
                        uiEventBroker.SetCurrentTabItemModifyProject(UIEventBroker.TabItemModifyProjectEnum.OptionGenerator);
                        uiEventBroker.NotifyProjectChanged(_viewModel.CurrentProject);
                        break;
                    case TabItem_CrudGenerator:
                        uiEventBroker.SetCurrentTabItemModifyProject(UIEventBroker.TabItemModifyProjectEnum.CrudGenerator);
                        uiEventBroker.NotifyProjectChanged(_viewModel.CurrentProject);
                        break;
                    case TabItem_DtoGenerator:
                        uiEventBroker.SetCurrentTabItemModifyProject(UIEventBroker.TabItemModifyProjectEnum.DtoGenerator);
                        uiEventBroker.NotifyProjectChanged(_viewModel.CurrentProject);
                        break;
                }
            }
        }

        private async void ResolveUsings_Click(object sender, RoutedEventArgs e)
        {
            await ResolveUsings_Run();
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
