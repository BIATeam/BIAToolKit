namespace BIAToolKit
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
    using BIAToolKit.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using System.Collections.Generic;
    using System;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GitService gitService;
        ProjectCreatorService projectCreatorService;
        ConsoleWriter consoleWriter;
        bool isCreateFrameworkVersionInitialized = false;
        string companyFilesPath = "";
        string tempFolderPath = "";
        string appFolderPath = "";
        CFSettings cfSettings = null;

        public MainWindow(GitService gitService, ProjectCreatorService projectCreatorService, IConsoleWriter consoleWriter)
        {
            this.gitService = gitService;
            this.projectCreatorService = projectCreatorService;

            InitializeComponent();

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


            tempFolderPath = Path.GetTempPath() + "BIAToolKit";
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }

            appFolderPath = System.Windows.Forms.Application.LocalUserAppDataPath;
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
            BIATemplateLocalFolderSync.IsEnabled = true;
            isCreateFrameworkVersionInitialized = false;
        }

        private void BIATemplateGitHub_Checked(object sender, RoutedEventArgs e)
        {
            BIATemplateLocalFolderText.IsEnabled = false;
            BIATemplateLocalFolderBrowse.IsEnabled = false;
            BIATemplateLocalFolderSync.IsEnabled = false;
            isCreateFrameworkVersionInitialized = false;
        }

        private void BIATemplateLocalFolderText_TextChanged(object sender, RoutedEventArgs e)
        {
            isCreateFrameworkVersionInitialized = false;
        }

        private void CompanyFilesLocalFolder_Checked(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderText.IsEnabled = true;
            CompanyFilesLocalFolderBrowse.IsEnabled = true;
            CompanyFilesLocalFolderSync.IsEnabled = true;
        }

        private void CompanyFilesGit_Checked(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderText.IsEnabled = false;
            CompanyFilesLocalFolderBrowse.IsEnabled = false;
            CompanyFilesLocalFolderSync.IsEnabled = false;
        }

        private async void BIATemplateLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            BIATemplateLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            this.gitService.Synchronize("BIATemplate", BIATemplateLocalFolderText.Text); 

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            BIATemplateLocalFolderSync.IsEnabled = true;

            isCreateFrameworkVersionInitialized = false;
        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            this.gitService.Synchronize("Company files", CompanyFilesLocalFolderText.Text);

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            CompanyFilesLocalFolderSync.IsEnabled = true;

            isCreateFrameworkVersionInitialized = false;
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

        private void OnTabCreateSelected(object sender, RoutedEventArgs e)
        {


            if (!isCreateFrameworkVersionInitialized)
            {
                int lastItemCreateCompanyFileVersion = -1;
                int lastItemFrameworkVersion = -1;

                CreateFrameworkVersion.Items.Clear();
                var tab = sender as TabItem;
                if (tab != null)
                {
                    if (BIATemplateLocalFolder.IsChecked == true)
                    {

                        string biaTemplatePath = BIATemplateLocalFolderText.Text;
                        if (Directory.Exists(biaTemplatePath))
                        {
                            List<string> versions = gitService.GetRelease(biaTemplatePath).OrderBy(q => q).ToList();

                            foreach (string version in versions)
                            {
                                //Add and select the last added
                                lastItemFrameworkVersion = CreateFrameworkVersion.Items.Add(version);
                            }

                            CreateFrameworkVersion.Items.Add("VX.Y.Z");
                        }
                        else
                        {
                            consoleWriter.AddMessageLine("The path " + biaTemplatePath + " do not exist.", Brushes.Red);
                        }
                    }
                }
                if (UseCompanyFile.IsChecked == true)
                {
                    CreateCompanyFileVersion.Visibility = Visibility.Visible;
                    CreateCompanyFileVersionLabel.Visibility = Visibility.Visible;

                    CreateCompanyFileVersion.Items.Clear();
                    if (CompanyFilesLocalFolder.IsChecked == true)
                    {
                        string companyFilesPath = CompanyFilesLocalFolderText.Text;
                        if (!Directory.Exists(companyFilesPath))
                        {
                            MessageBox.Show("Error on company files path local folder :\r\nThe path " + companyFilesPath + " do not exist.\r\n Correct it in config tab.");
                        }
                        else
                        {
                            DirectoryInfo di = new DirectoryInfo(companyFilesPath);
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
                }
                else
                {
                    CreateCompanyFileGroup.Visibility = Visibility.Hidden;
                    CreateCompanyFileVersion.Visibility = Visibility.Hidden;
                    CreateCompanyFileVersionLabel.Visibility = Visibility.Hidden;
                    CreateCompanyFileProfile.Visibility = Visibility.Hidden;
                    CreateCompanyFileProfileLabel.Visibility = Visibility.Hidden;
                }

                if (lastItemFrameworkVersion != -1) CreateFrameworkVersion.SelectedIndex = lastItemFrameworkVersion;
                if (lastItemCreateCompanyFileVersion != -1) CreateCompanyFileVersion.SelectedIndex = lastItemCreateCompanyFileVersion;

                isCreateFrameworkVersionInitialized = true;
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
            this.projectCreatorService.Create(CreateCompanyName.Text, CreateProjectName.Text, projectPath, BIATemplateLocalFolderText.Text, CreateFrameworkVersion.SelectedValue.ToString(),
            UseCompanyFile.IsChecked == true, cfSettings, companyFilesPath, CreateCompanyFileProfile.Text, appFolderPath);
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
           isCreateFrameworkVersionInitialized = false;
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
    }
}
