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
    using System.Text.Json;
    using BIA.ToolKit.Application.CompanyFiles;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using System.Collections.Generic;
    using System;
    using BIA.ToolKit.Application.Helper;
    using System.Text.RegularExpressions;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GitService gitService;
        ProjectCreatorService projectCreatorService;
        ConsoleWriter consoleWriter;
        bool isCreateTabInitialized = false;
        bool isModifyTabInitialized = false;

        Configuration configuration;

        string companyFilesPath = "";
        CFSettings cfSettings = null;

        string modifyProjectPath = "";

        public MainWindow(Configuration configuration, GitService gitService, ProjectCreatorService projectCreatorService, IConsoleWriter consoleWriter)
        {
            this.configuration = configuration;
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;

            InitializeComponent();
            MigrateVersionAndOption.Inject(configuration,gitService,consoleWriter);

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



        }

        public bool RefreshConfiguration()
        {
            if (RefreshBIATemplateConfiguration())
            {
                return RefreshCompanyFilesConfiguration();
            }
            return false;
        }

        public bool RefreshBIATemplateConfiguration()
        {
            Configurationchange(); 
            if (!configuration.RefreshBIATemplate(MainTab, BIATemplateLocalFolder.IsChecked == true, BIATemplateLocalFolderText.Text))
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

        public bool RefreshCompanyFilesConfiguration()
        {
            Configurationchange(); 
            if (!configuration.RefreshCompanyFiles(MainTab, UseCompanyFile.IsChecked == true, CompanyFilesLocalFolder.IsChecked == true, CompanyFilesLocalFolderText.Text))
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
            if (RefreshBIATemplateConfiguration())
            {
                BIATemplateLocalFolderSync.IsEnabled = false;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                if (!configuration.BIATemplateLocalFolderIsChecked && !Directory.Exists(configuration.BIATemplatePath))
                {
                    this.gitService.Clone("BIATemplate", "https://github.com/BIATeam/BIATemplate.git", configuration.BIATemplatePath);
                }
                else
                {
                    this.gitService.Synchronize("BIATemplate", configuration.BIATemplatePath);
                }

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                BIATemplateLocalFolderSync.IsEnabled = true;
            }
        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshCompanyFilesConfiguration())
            {
                CompanyFilesLocalFolderSync.IsEnabled = false;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                if (configuration.CompanyFilesLocalFolderIsChecked)
                {
                    this.gitService.Synchronize("Company files", configuration.RootCompanyFilesPath);
                }
                else
                {
                    if (Directory.Exists(configuration.RootCompanyFilesPath))
                    {
                        FileTransform.ForceDeleteDirectory(configuration.RootCompanyFilesPath);
                    }
                    this.gitService.Clone("BIACompanyFiles", CompanyFilesGitRepo.Text, configuration.RootCompanyFilesPath);
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
                        MigrateVersionAndOption.refreshConfig();
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

                        int lastItemCreateCompanyFileVersion = -1;
                        int lastItemCreateFrameworkVersion = -1;

                        CreateFrameworkVersion.Items.Clear();
                        CreateCompanyFileVersion.Items.Clear();


                        if (Directory.Exists(configuration.BIATemplatePath))
                        {
                            List<string> versions = gitService.GetRelease(configuration.BIATemplatePath).OrderBy(q => q).ToList();

                            foreach (string version in versions)
                            {
                                //Add and select the last added
                                lastItemCreateFrameworkVersion = CreateFrameworkVersion.Items.Add(version);
                            }

                            CreateFrameworkVersion.Items.Add("VX.Y.Z");
                        }

                        if (configuration.UseCompanyFileIsChecked)
                        {
                            CreateCompanyFileGroup.Visibility = Visibility.Visible;


                            if (Directory.Exists(configuration.RootCompanyFilesPath))
                            {
                                DirectoryInfo di = new DirectoryInfo(configuration.RootCompanyFilesPath);
                                // Create an array representing the files in the current directory.
                                DirectoryInfo[] versionDirectories = di.GetDirectories("V*.*.*", SearchOption.TopDirectoryOnly);
                                // Print out the names of the files in the current directory.
                                foreach (DirectoryInfo dir in versionDirectories)
                                {
                                    //Add and select the last added
                                    lastItemCreateCompanyFileVersion = CreateCompanyFileVersion.Items.Add(dir.Name);
                                }
                            }
                        }
                        else
                        {
                            CreateCompanyFileGroup.Visibility = Visibility.Hidden;
                        }

                        if (lastItemCreateFrameworkVersion != -1) CreateFrameworkVersion.SelectedIndex = lastItemCreateFrameworkVersion;
                        if (lastItemCreateCompanyFileVersion != -1) CreateCompanyFileVersion.SelectedIndex = lastItemCreateCompanyFileVersion;

                        isCreateTabInitialized = true;
                    }
                }
            }
        }

        private void CreateFrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i= 0; i<CreateCompanyFileVersion.Items.Count; i++)
            {
                if (CreateFrameworkVersion.SelectedValue?.ToString() == CreateCompanyFileVersion.Items[i].ToString())
                {
                    CreateCompanyFileVersion.SelectedIndex = i;
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
            if (CreateFrameworkVersion.SelectedValue == null)
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
            this.projectCreatorService.Create(CreateCompanyName.Text, CreateProjectName.Text, projectPath, configuration.BIATemplatePath, CreateFrameworkVersion.SelectedValue.ToString(),
            configuration.UseCompanyFileIsChecked, cfSettings, companyFilesPath, CreateCompanyFileProfile.Text, configuration.AppFolderPath);
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

        private async void CreateCompanyFileVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateCompanyFileProfile.Items.Clear();
            CreateGridOption.Children.Clear();
            if (!string.IsNullOrEmpty(CreateCompanyFileVersion.SelectedValue?.ToString()))
            {
                companyFilesPath = CompanyFilesLocalFolderText.Text + "\\" + CreateCompanyFileVersion.SelectedValue;
                string fileName = companyFilesPath + "\\biaCompanyFiles.json";
                string jsonString = File.ReadAllText(fileName);

                try {

                    cfSettings = JsonSerializer.Deserialize<CFSettings>(jsonString);

                    int lastIndex = -1;
                    foreach (string profile in cfSettings.Profiles)
                    {
                        lastIndex = CreateCompanyFileProfile.Items.Add(profile);
                    }
                    if (lastIndex != -1) CreateCompanyFileProfile.SelectedIndex = lastIndex;


                    int top = 0;

                    foreach (CFOption option in cfSettings.Options)
                    {
                        option.IsChecked = true;

                        //  <CheckBox Content="Otpion2" Foreground="White"  Height="16" VerticalAlignment="Top" Name="CreateCFOption_Otpion2" Margin="0,25,0,0" />
                        CheckBox checkbox = new CheckBox();
                        checkbox.Content = option.Name;
                        checkbox.IsChecked = true;
                        checkbox.Foreground = Brushes.White;
                        checkbox.Height = 16;
                        checkbox.Name = "CreateCFOption_" + option.Key;
                        checkbox.Margin = new Thickness(0, top, 0, 0);
                        checkbox.Checked += CreateCompanyFileOtption_Checked;
                        checkbox.Unchecked += CreateCompanyFileOtption_Checked;
                        top += 25;
                        checkbox.VerticalAlignment = VerticalAlignment.Top;
                        CreateGridOption.Children.Add(checkbox);
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine(ex.Message, Brushes.Red);
                }
            }

        }

        private void UseCompanyFile_Checked(object sender, RoutedEventArgs e)
        {
           Configurationchange(); 
        }

        private void CreateCompanyFileOtption_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                CheckBox chx = (CheckBox)sender;
                foreach (CFOption option in cfSettings.Options)
                {
                    if ("CreateCFOption_" + option.Key == chx.Name)
                    {
                        option.IsChecked = chx.IsChecked == true;
                    }
                }
            }
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
                Color col = (Color)ColorConverter.ConvertFromString("Red");
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
            if (ModifyProject != null)
            {
                ModifyProjectCompany.Content = "???";
                ModifyProjectName.Content = "???";
                ModifyProjectVersion.Content = "???";
                ModifyProject.Items.Clear();
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
                    ModifyProject.Items.Add(dir.Name);
                }
            }
        }

        private void ModifyProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModifyProjectCompany.Content = "???";
            ModifyProjectName.Content = "???";
            ModifyProjectVersion.Content = "???";

            modifyProjectPath = ModifyProjectRootFolderText.Text + "\\" + ModifyProject.SelectedValue;
            Regex reg = new Regex(modifyProjectPath.Replace("\\","\\\\") + @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\Constants\.cs$");
            string file = Directory.GetFiles(modifyProjectPath, "Constants.cs", SearchOption.AllDirectories).Where(path => reg.IsMatch(path)).FirstOrDefault();
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
        }
    }
}
