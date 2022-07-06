namespace BIA.ToolKit
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using System.Collections.Generic;
    using System;
    using System.Text.RegularExpressions;
    using System.Diagnostics;



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GitService gitService;
        ProjectCreatorService projectCreatorService;
        GenerateFilesService generateFilesService;
        ConsoleWriter consoleWriter;
        bool isCreateTabInitialized = false;
        bool isModifyTabInitialized = false;

        Configuration configuration;

        string modifyProjectPath = "";

        public MainWindow(Configuration configuration, GitService gitService,GenerateFilesService genFilesService, ProjectCreatorService projectCreatorService, IConsoleWriter consoleWriter)
        {
            //if (Settings.Default.UpdateSettings)
            //{
            //    Settings.Default.Upgrade();
            //    Settings.Default.UpdateSettings = false;
            //    Settings.Default.Save();
            //}

            this.configuration = configuration;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;
            this.generateFilesService = genFilesService;

            InitializeComponent();
            MigrateOriginVersionAndOption.Inject(configuration, gitService, consoleWriter);
            MigrateTargetVersionAndOption.Inject(configuration, gitService, consoleWriter);
            CreateVersionAndOption.Inject(configuration,gitService,consoleWriter);

            this.consoleWriter = (ConsoleWriter) consoleWriter;
            this.consoleWriter.InitOutput(OutputText, OutputTextViewer);

            BIATemplateGitHub.IsChecked = Settings.Default.BIATemplateGitHub;
            BIATemplateLocalFolder.IsChecked = Settings.Default.BIATemplateLocalFolder;
            BIATemplateLocalFolderText.Text = Settings.Default.BIATemplateLocalFolderText;

            UseCompanyFile.IsChecked = Settings.Default.UseCompanyFile;
            CompanyFilesGit.IsChecked = Settings.Default.CompanyFilesGit;
            CompanyFilesLocalFolder.IsChecked = Settings.Default.CompanyFilesLocalFolder;
            CompanyFilesGitRepo.Text = Settings.Default.CompanyFilesGitRepo;
            CompanyFilesLocalFolderText.Text = Settings.Default.CompanyFilesLocalFolderText;

            CreateProjectRootFolderText.Text = Settings.Default.CreateProjectRootFolderText;
            ModifyProjectRootFolderText.Text = Settings.Default.CreateProjectRootFolderText;
            CreateCompanyName.Text = Settings.Default.CreateCompanyName;

            txtFileGenerator_Folder.Text = Path.GetTempPath() + "BIAToolKit\\";


        }

        public bool RefreshConfiguration()
        {
            if (RefreshBIATemplateConfiguration(false))
            {
                return RefreshCompanyFilesConfiguration(false);
            }
            return false;
        }

        public bool RefreshBIATemplateConfiguration(bool inSync)
        {
            Configurationchange(); 
            if (!configuration.RefreshBIATemplate(MainTab, BIATemplateLocalFolder.IsChecked == true, BIATemplateLocalFolderText.Text, inSync))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

        private void Configurationchange()
        {
            isCreateTabInitialized = false;
            isModifyTabInitialized = false;
        }

        public bool RefreshCompanyFilesConfiguration(bool inSync)
        {
            Configurationchange(); 
            if (!configuration.RefreshCompanyFiles(MainTab, UseCompanyFile.IsChecked == true, CompanyFilesLocalFolder.IsChecked == true, CompanyFilesLocalFolderText.Text, inSync))
            {
                Dispatcher.BeginInvoke((Action)(() => MainTab.SelectedIndex = 0));
                return false;
            }
            return true;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BIATemplateGitHub = BIATemplateGitHub.IsChecked == true;
            Settings.Default.BIATemplateLocalFolder = BIATemplateLocalFolder.IsChecked == true;
            Settings.Default.BIATemplateLocalFolderText = BIATemplateLocalFolderText.Text;

            Settings.Default.UseCompanyFile = UseCompanyFile.IsChecked == true;
            Settings.Default.CompanyFilesGit = CompanyFilesGit.IsChecked == true;
            Settings.Default.CompanyFilesLocalFolder = CompanyFilesLocalFolder.IsChecked == true;
            Settings.Default.CompanyFilesGitRepo = CompanyFilesGitRepo.Text;
            Settings.Default.CompanyFilesLocalFolderText = CompanyFilesLocalFolderText.Text;

            Settings.Default.CreateProjectRootFolderText = CreateProjectRootFolderText.Text;
            Settings.Default.CreateCompanyName = CreateCompanyName.Text;

            Settings.Default.Save();
        }

        private void BIATemplateLocalFolder_Checked(object sender, RoutedEventArgs e)
        {
            BIATemplateLocalFolderText.IsEnabled = true;
            BIATemplateLocalFolderBrowse.IsEnabled = true;
            Configurationchange();
        }

        private void BIATemplateGitHub_Checked(object sender, RoutedEventArgs e)
        {
            BIATemplateLocalFolderText.IsEnabled = false;
            BIATemplateLocalFolderBrowse.IsEnabled = false;
            Configurationchange();
        }

        private void BIATemplateLocalFolderText_TextChanged(object sender, RoutedEventArgs e)
        {
            Configurationchange();
        }

        private void CompanyFilesLocalFolder_Checked(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderText.IsEnabled = true;
            CompanyFilesLocalFolderBrowse.IsEnabled = true;
            Configurationchange();
        }

        private void CompanyFilesGit_Checked(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderText.IsEnabled = false;
            CompanyFilesLocalFolderBrowse.IsEnabled = false;
            Configurationchange(); 
        }

        private async void BIATemplateLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshBIATemplateConfiguration(true))
            {
                BIATemplateLocalFolderSync.IsEnabled = false;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                if (!configuration.BIATemplateLocalFolderIsChecked && !Directory.Exists(configuration.BIATemplatePath))
                {
                    await this.gitService.Clone("BIATemplate", "https://github.com/BIATeam/BIATemplate.git", configuration.BIATemplatePath);
                }
                else
                {
                    await this.gitService.Synchronize("BIATemplate", configuration.BIATemplatePath);
                }
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                BIATemplateLocalFolderSync.IsEnabled = true;
            }
        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshCompanyFilesConfiguration(true))
            {
                CompanyFilesLocalFolderSync.IsEnabled = false;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                if (configuration.CompanyFilesLocalFolderIsChecked)
                {
                    await this.gitService.Synchronize("BIACompanyFiles", configuration.RootCompanyFilesPath);
                }
                else
                {
                    if (Directory.Exists(configuration.RootCompanyFilesPath))
                    {
                        FileTransform.ForceDeleteDirectory(configuration.RootCompanyFilesPath);
                    }
                    await this.gitService.Clone("BIACompanyFiles", CompanyFilesGitRepo.Text, configuration.RootCompanyFilesPath);
                }

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                CompanyFilesLocalFolderSync.IsEnabled = true;
            }
        }

        private void BIATemplateLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.BrowseFolder(BIATemplateLocalFolderText);
        }

        private void CompanyFilesLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.BrowseFolder(CompanyFilesLocalFolderText);
        }


        private void CreateProjectRootFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.BrowseFolder(CreateProjectRootFolderText);
        }


        private void OnTabModifySelected(object sender, RoutedEventArgs e)
        {
            var tab = sender as TabItem;
            if (tab != null)
            {
                if (!isModifyTabInitialized)
                {
                    if (RefreshConfiguration())
                    {
                        MigrateOriginVersionAndOption.refreshConfig();
                        MigrateTargetVersionAndOption.refreshConfig();
                        isModifyTabInitialized = true;
                    }
                }
            }
        }

        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {
            var tab = sender as TabItem;
            if (tab != null)
            {
                if (!isCreateTabInitialized)
                {
                    if (RefreshConfiguration())
                    {
                        CreateVersionAndOption.refreshConfig();
                        isCreateTabInitialized = true;
                    }
                }
            }
        }


        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CreateProjectRootFolderText.Text))
            {
                MessageBox.Show("Please select root path.");
                return;
            }
            if (string.IsNullOrEmpty(CreateCompanyName.Text))
            {
                MessageBox.Show("Please select company name.");
                return;
            }
            if (string.IsNullOrEmpty(CreateProjectName.Text))
            {
                MessageBox.Show("Please select project name.");
                return;
            }
            if (CreateVersionAndOption.FrameworkVersion.SelectedValue == null)
            {
                MessageBox.Show("Please select framework version.");
                return;
            }

            string projectPath = CreateProjectRootFolderText.Text + "\\" + CreateProjectName.Text;
            if (Directory.Exists(projectPath) && !IsDirectoryEmpty(projectPath))
            {
                MessageBox.Show("The project path is not empty : " + projectPath);
                return;
            }
            CreateProject(true, CreateCompanyName.Text, CreateProjectName.Text, projectPath, CreateVersionAndOption, new string[] { "Angular" } );
        }

        private void CreateProject(bool actionFinishedAtEnd, string CompanyName, string ProjectName, string projectPath, VersionAndOptionUserControl versionAndOption, string[] fronts)
        {
            this.projectCreatorService.Create(actionFinishedAtEnd, CompanyName, ProjectName, projectPath, configuration.BIATemplatePath, versionAndOption.FrameworkVersion.SelectedValue.ToString(),
            configuration.UseCompanyFileIsChecked, versionAndOption.CfSettings, versionAndOption.CompanyFilesPath, versionAndOption.CompanyFileProfile.Text, configuration.AppFolderPath, fronts);
        }

        private bool IsDirectoryEmpty(string path)
        {
            string[] files = System.IO.Directory.GetFiles(path);
            if (files.Length != 0) return false;

            List<string> dirs = System.IO.Directory.GetDirectories(path).ToList();

            if(dirs.Where(d => !d.EndsWith("\\.git")).Count() != 0) return false;

            return true;

            //return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private void UseCompanyFile_Checked(object sender, RoutedEventArgs e)
        {
           Configurationchange(); 
        }

        public void AddMessageLine(string message,  string color= null)
        {
            Brush brush = null;
            if (string.IsNullOrEmpty(color))
            {
                brush = Brushes.White;
            }
            else
            {
                Color col = (Color)ColorConverter.ConvertFromString(color);
                brush = new SolidColorBrush(col);
            }
            consoleWriter.AddMessageLine(message, brush);
        }

        private void CreateProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ModifyProjectRootFolderText != null && CreateProjectRootFolderText != null && ModifyProjectRootFolderText.Text != CreateProjectRootFolderText.Text)
            {
                ModifyProjectRootFolderText.Text = CreateProjectRootFolderText.Text;
            }
        }

        private void ModifyProjectRootFolderText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParameterModifyChange();

            if (ModifyProjectFolder != null)
            {
                ModifyProjectCompany.Content = "???";
                ModifyProjectName.Content = "???";
                ModifyProjectVersion.Content = "???";
                ModifyProjectFolder.Items.Clear();
            }
            if (ModifyProjectRootFolderText != null && CreateProjectRootFolderText != null && ModifyProjectRootFolderText.Text != CreateProjectRootFolderText.Text)
            {
                CreateProjectRootFolderText.Text = ModifyProjectRootFolderText.Text;
            }
            if (Directory.Exists(ModifyProjectRootFolderText.Text))
            {
                DirectoryInfo di = new DirectoryInfo(ModifyProjectRootFolderText.Text);
                // Create an array representing the files in the current directory.
                DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
                // Print out the names of the files in the current directory.
                foreach (DirectoryInfo dir in versionDirectories)
                {
                    //Add and select the last added
                    ModifyProjectFolder.Items.Add(dir.Name);
                }
            }
        }

        private void ModifyProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ParameterModifyChange();

            ModifyProjectCompany.Content = "???";
            ModifyProjectName.Content = "???";
            ModifyProjectVersion.Content = "???";

            modifyProjectPath = ModifyProjectRootFolderText.Text + "\\" + ModifyProjectFolder.SelectedValue;
            Regex reg = new Regex(modifyProjectPath.Replace("\\","\\\\") + @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$", RegexOptions.IgnoreCase);
            string file = Directory.GetFiles(modifyProjectPath, "Constants.cs", SearchOption.AllDirectories)?.Where(path => reg.IsMatch(path))?.FirstOrDefault();
            if (file != null)
            {
                var match = reg.Match(file);
                ModifyProjectCompany.Content = match.Groups[1].Value;
                ModifyProjectName.Content = match.Groups[2].Value;
                Regex regVersion = new Regex(@" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$");

                foreach (var line in File.ReadAllLines(file))
                {
                    var matchVersion = regVersion.Match(line);
                    if (matchVersion.Success )
                    {
                        ModifyProjectVersion.Content = matchVersion.Groups[1].Value;
                        break;
                    }
                }
            }

            Regex reg2 = new Regex(modifyProjectPath.Replace("\\", "\\\\") + @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$", RegexOptions.IgnoreCase);
            List<string> filesFront = Directory.GetFiles(modifyProjectPath, "bia-core.module.ts", SearchOption.AllDirectories)?.Where(path => reg2.IsMatch(path)).ToList();
            ModifyBIAFronts.Content = "";
            if (filesFront != null)
            {
                foreach(var fileFront in filesFront)
                {
                    var match = reg2.Match(fileFront);
                    if (ModifyBIAFronts.Content.ToString() != "")
                    {
                        ModifyBIAFronts.Content += ", ";
                    }
                    ModifyBIAFronts.Content += match.Groups[1].Value;
                }
            }
        }

        private async void Migrate_Click(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists(modifyProjectPath) || IsDirectoryEmpty(modifyProjectPath))
            {
                MessageBox.Show("The project path is empty : " + modifyProjectPath);
                return;
            }

            Enable(false);

            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            GenerateProjects(false, projectOriginPath, projectTargetPath);

            await ApplyDiff(false,  projectOriginalFolderName, projectTargetFolderName);

            await MergeRejected(false);

            consoleWriter.AddMessageLine("Migration finished.", "Green" );

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

            MainTab.IsEnabled = isEnabled;
            System.Windows.Forms.Application.DoEvents();
        }

        private void ParameterModifyChange()
        {
            if (MigrateOpenFolder!= null) MigrateOpenFolder.IsEnabled = false;
            if (MigrateApplyDiff != null) MigrateApplyDiff.IsEnabled = false;
            if (MigrateMergeRejected != null) MigrateMergeRejected.IsEnabled = false;
            if (MigrateOverwriteBIAFolder != null) MigrateOverwriteBIAFolder.IsEnabled = false;
        }

        private void MigrateGenerateOnly_Click(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists(modifyProjectPath) || IsDirectoryEmpty(modifyProjectPath))
            {
                MessageBox.Show("The project path is empty : " + modifyProjectPath);
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
            projectOriginalFolderName = ModifyProjectName.Content + "_" + projectOriginalVersion + "_From";
            projectOriginPath = configuration.TmpFolderPath + projectOriginalFolderName;

            projectTargetVersion = MigrateTargetVersionAndOption.FrameworkVersion.SelectedValue.ToString();
            projectTargetFolderName = ModifyProjectName.Content + "_" + projectTargetVersion + "_To";
            projectTargetPath = configuration.TmpFolderPath + projectTargetFolderName;
        }

        private void GenerateProjects(bool actionFinishedAtEnd, string projectOriginPath, string projectTargetPath)
        {
            // Create project at original version.
            if (Directory.Exists(projectOriginPath))
            {
                FileTransform.ForceDeleteDirectory(projectOriginPath);
            }

            string []fronts = ModifyBIAFronts.Content.ToString().Split(", ");

            CreateProject(false, ModifyProjectCompany.Content.ToString(), ModifyProjectName.Content.ToString(), projectOriginPath, MigrateOriginVersionAndOption, fronts);

            // Create project at target version.
            if (Directory.Exists(projectTargetPath))
            {
                FileTransform.ForceDeleteDirectory(projectTargetPath);
            }
            CreateProject(false, ModifyProjectCompany.Content.ToString(), ModifyProjectName.Content.ToString(), projectTargetPath, MigrateTargetVersionAndOption, fronts);

            consoleWriter.AddMessageLine("Generate projects finished.", actionFinishedAtEnd ? "Green" : "Blue");
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
            await gitService.ApplyDiff(actionFinishedAtEnd, modifyProjectPath, migrateFilePath);
        }

        private async System.Threading.Tasks.Task MergeRejected(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            await gitService.MergeRejeted(actionFinishedAtEnd, new GitService.MergeParameter()
            {
                ProjectPath = modifyProjectPath,
                ProjectOriginPath = projectOriginPath,
                ProjectOriginVersion = projectOriginalVersion,
                ProjectTargetPath = projectTargetPath,
                ProjectTargetVersion = projectTargetVersion
            });;
        }

        private async System.Threading.Tasks.Task OverwriteBIAFolder(bool actionFinishedAtEnd)
        {
            string projectOriginalFolderName, projectOriginPath, projectOriginalVersion, projectTargetFolderName, projectTargetPath, projectTargetVersion;
            MigratePreparePath(out projectOriginalFolderName, out projectOriginPath, out projectOriginalVersion, out projectTargetFolderName, out projectTargetPath, out projectTargetVersion);

            projectCreatorService.OverwriteBIAFolder(projectTargetPath, modifyProjectPath, actionFinishedAtEnd);

        }

        private void btnFileGenerator_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFileGenerator_Folder.Text = dlg.SelectedPath;
            }
        }

        private void btnFileGenerator_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox 
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Open document 
                string filename = dlg.FileName;
                txtFileGenerator_File.Text = filename;
                btnFileGenerator_Generate.IsEnabled = true;
            }
        }

        private void btnFileGenerator_Generate_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(txtFileGenerator_Folder.Text))
            {
                if (!string.IsNullOrEmpty(txtFileGenerator_File.Text))
                {
                    string resultFolder = string.Empty;
                    generateFilesService.GenerateFiles(txtFileGenerator_File.Text, txtFileGenerator_Folder.Text, ref resultFolder);
                    ProcessStartInfo startInfo = new ProcessStartInfo(resultFolder)
                    {
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);

                }
                else
                {
                    MessageBox.Show("Select the class file","Generate files",MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            else
            {
                MessageBox.Show("Select the folder to save the files", "Generate files", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
