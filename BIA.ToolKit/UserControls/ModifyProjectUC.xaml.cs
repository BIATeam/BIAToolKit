namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for ModifyProjectUC.xaml
    /// </summary>
    public partial class ModifyProjectUC : UserControl
    {
        public ModifyProjectViewModel _viewModel;
        IConsoleWriter consoleWriter;
        GitService gitService;
        Configuration configuration;
        CSharpParserService cSharpParserService;
        ProjectCreatorService projectCreatorService;

        public ModifyProjectUC()
        {
            InitializeComponent();
            _viewModel = (ModifyProjectViewModel)base.DataContext;
            _viewModel.RootProjectsPath = Settings.Default.CreateProjectRootFolderText;
        }

        public void Inject(Configuration configuration, GitService gitService, IConsoleWriter consoleWriter, CSharpParserService cSharpParserService, ProjectCreatorService projectCreatorService)
        {
            this.configuration = configuration;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.cSharpParserService = cSharpParserService;
            this.projectCreatorService = projectCreatorService;
            MigrateOriginVersionAndOption.Inject(configuration, gitService, consoleWriter);
            MigrateTargetVersionAndOption.Inject(configuration, gitService, consoleWriter);
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
        }



        private async void Migrate_Click(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists(_viewModel.ModifyProject.CurrentProject.Folder) || FileDialog.IsDirectoryEmpty(_viewModel.ModifyProject.CurrentProject.Folder))
            {
                MessageBox.Show("The project path is empty : " + _viewModel.ModifyProject.CurrentProject.Folder);
                return;
            }

            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            GenerateProjects(false, projectOriginPath, projectTargetPath);

            await ApplyDiff(false, projectOriginalFolderName, projectTargetFolderName);

            await MergeRejected(false);

            consoleWriter.AddMessageLine("Migration finished.", "Green");

            MigrateOpenFolder.IsEnabled = true;
            MigrateMergeRejected.IsEnabled = true;

            Enable(true);
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
            if (MigrateOverwriteBIAFolder != null) MigrateOverwriteBIAFolder.IsEnabled = false;
        }

        private void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists(_viewModel.ModifyProject.CurrentProject.Folder) || FileDialog.IsDirectoryEmpty(_viewModel.ModifyProject.CurrentProject.Folder))
            {
                MessageBox.Show("The project path is empty : " + _viewModel.ModifyProject.CurrentProject.Folder);
                return;
            }

            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            GenerateProjects(true, projectOriginPath, projectTargetPath);

            MigrateOpenFolder.IsEnabled = true;
            MigrateApplyDiff.IsEnabled = true;
            MigrateMergeRejected.IsEnabled = true;
            MigrateOverwriteBIAFolder.IsEnabled = true;
            Enable(true);
        }

        private async void MigrateApplyDiff_Click(object sender, RoutedEventArgs e)
        {
            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);


            await ApplyDiff(true, projectOriginalFolderName, projectTargetFolderName);

            Enable(true);
        }

        private async void MigrateMergeRejected_Click(object sender, RoutedEventArgs e)
        {
            Enable(false);

            await MergeRejected(true);

            MigrateMergeRejected.IsEnabled = false;
            Enable(true);
        }

        private async void MigrateOverwriteBIAFolder_Click(object sender, RoutedEventArgs e)
        {
            Enable(false);

            await OverwriteBIAFolder(true);

            MigrateOverwriteBIAFolder.IsEnabled = false;

            Enable(true);
        }

        private void MigratePreparePath(out string projectOriginalFolderName, out string projectOriginPath, out string projectOriginalVersion, out string projectTargetFolderName, out string projectTargetPath, out string projectTargetVersion)
        {
            projectOriginalVersion = MigrateOriginVersionAndOption.FrameworkVersion.SelectedValue.ToString();
            projectOriginalFolderName = _viewModel.Name + "_" + projectOriginalVersion + "_From";
            projectOriginPath = configuration.TmpFolderPath + projectOriginalFolderName;

            projectTargetVersion = MigrateTargetVersionAndOption.FrameworkVersion.SelectedValue.ToString();
            projectTargetFolderName = _viewModel.Name + "_" + projectTargetVersion + "_To";
            projectTargetPath = configuration.TmpFolderPath + projectTargetFolderName;
        }

        private void GenerateProjects(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            // Create project at original version.
            if (Directory.Exists(projectOriginPath))
            {
                FileTransform.ForceDeleteDirectory(projectOriginPath);
            }

            string[] fronts = _viewModel.BIAFronts.Split(", ");

            CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectOriginPath, MigrateOriginVersionAndOption, fronts);

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                FileTransform.ForceDeleteDirectory(projectTargetPath);
            }
            CreateProject(false, _viewModel.CompanyName, _viewModel.Name, projectTargetPath, MigrateTargetVersionAndOption, fronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
        }

        //TODO mutualiser avec celle de MainWindows
        private void CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, string[] fronts)
        {
            this.projectCreatorService.Create(actionFinishedAtEnd, CompanyName, ProjectName, projectPath, configuration.BIATemplatePath, versionAndOption.FrameworkVersion.SelectedValue.ToString(),
            configuration.UseCompanyFileIsChecked, versionAndOption.CfSettings, versionAndOption.CompanyFilesPath, versionAndOption.CompanyFileProfile.Text, configuration.AppFolderPath, fronts);
        }

        private void MigrateOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", configuration.TmpFolderPath);
        }

        private async System.Threading.Tasks.Task ApplyDiff(bool actionFinishedAtEnd, string projectOriginalFolderName, string projectTargetFolderName)
        {
            // Make the differential
            string migrateFilePath = configuration.TmpFolderPath + $"Migration_{projectOriginalFolderName}-{projectTargetFolderName}.patch";
            await gitService.DiffFolder(false, configuration.TmpFolderPath, projectOriginalFolderName, projectTargetFolderName, migrateFilePath);

            //Apply the differential
            await gitService.ApplyDiff(actionFinishedAtEnd, _viewModel.ModifyProject.CurrentProject.Folder, migrateFilePath);
        }

        private async System.Threading.Tasks.Task MergeRejected(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await gitService.MergeRejeted(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = _viewModel.ModifyProject.CurrentProject.Folder,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = projectOriginalVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = projectTargetVersion
            }); ;
        }

        private async System.Threading.Tasks.Task OverwriteBIAFolder(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            projectCreatorService.OverwriteBIAFolder(projectTargetPath, _viewModel.ModifyProject.CurrentProject.Folder, actionFinishedAtEnd);

        }

        private void ModifyProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath);
        }
    }
}
