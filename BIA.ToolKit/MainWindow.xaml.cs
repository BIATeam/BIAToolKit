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
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Threading;
    using BIA.ToolKit.Application.Parser;
    using BIA.ToolKit.UserControls;
    using BIA.ToolKit.Application.ViewModel;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel _viewModel;

        GitService gitService;
        ProjectCreatorService projectCreatorService;
        GenerateFilesService generateFilesService;
        CSharpParserService cSharpParserService;
        ConsoleWriter consoleWriter;
        bool isCreateTabInitialized = false;
        bool isModifyTabInitialized = false;

        Configuration configuration;

        public MainWindow(Configuration configuration, GitService gitService, CSharpParserService cSharpParserService, GenerateFilesService genFilesService, ProjectCreatorService projectCreatorService, IConsoleWriter consoleWriter)
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
            this.cSharpParserService = cSharpParserService;

            InitializeComponent();
            _viewModel = (MainViewModel)base.DataContext;
            CreateVersionAndOption.Inject(configuration,gitService,consoleWriter);
            ModifyProject.Inject(configuration, gitService, consoleWriter, cSharpParserService, projectCreatorService);

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

            _viewModel.RootProjectsPath = Settings.Default.CreateProjectRootFolderText;
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

            Settings.Default.CreateProjectRootFolderText = _viewModel.RootProjectsPath;
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
            _viewModel.RootProjectsPath = FileDialog.BrowseFolder(_viewModel.RootProjectsPath);
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
                        ModifyProject.RefreshConfiguration();
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
            if (string.IsNullOrEmpty(_viewModel.RootProjectsPath))
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

            string projectPath = _viewModel.RootProjectsPath + "\\" + CreateProjectName.Text;
            if (Directory.Exists(projectPath) && !FileDialog.IsDirectoryEmpty(projectPath))
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
            //TODO recabler:
            /*if (ModifyProjectRootFolderText != null && CreateProjectRootFolderText != null && ModifyProjectRootFolderText.Text != CreateProjectRootFolderText.Text)
            {
                ModifyProjectRootFolderText.Text = CreateProjectRootFolderText.Text;
            }*/
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
