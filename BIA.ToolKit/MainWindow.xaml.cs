namespace BIAToolKit
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Properties;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Text.Json;
    using BIA.ToolKit.Application.CompanyFiles;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConsoleWriter consoleWriter;
        bool isCreateFrameworkVersionInitialized = false;
        string biaDemoBIATemplatePath = "";
        string companyFilesPath = "";
        string tempFolderPath = Path.GetTempPath();
        CFSettings cfSettings = null;

        public MainWindow()
        {
            InitializeComponent();

            consoleWriter = new ConsoleWriter(OutputText, OutputTextViewer);

            BIADemoGitHub.IsChecked = Settings.Default.BIADemoGitHub;
            BIADemoLocalFolder.IsChecked = Settings.Default.BIADemoLocalFolder;
            BIADemoLocalFolderText.Text = Settings.Default.BIADemoLocalFolderText;

            UseCompanyFile.IsChecked = Settings.Default.UseCompanyFile;
            CompanyFilesGit.IsChecked = Settings.Default.CompanyFilesGit;
            CompanyFilesLocalFolder.IsChecked = Settings.Default.CompanyFilesLocalFolder;
            CompanyFilesGitRepo.Text = Settings.Default.CompanyFilesGitRepo;
            CompanyFilesLocalFolderText.Text = Settings.Default.CompanyFilesLocalFolderText;

            CreateProjectRootFolderText.Text = Settings.Default.CreateProjectRootFolderText;
            CreateCompanyName.Text = Settings.Default.CreateCompanyName;


            tempFolderPath = Path.GetTempPath() + "BIAToolKit";
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }


        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BIADemoGitHub = BIADemoGitHub.IsChecked == true;
            Settings.Default.BIADemoLocalFolder = BIADemoLocalFolder.IsChecked == true;
            Settings.Default.BIADemoLocalFolderText = BIADemoLocalFolderText.Text;

            Settings.Default.UseCompanyFile = UseCompanyFile.IsChecked == true;
            Settings.Default.CompanyFilesGit = CompanyFilesGit.IsChecked == true;
            Settings.Default.CompanyFilesLocalFolder = CompanyFilesLocalFolder.IsChecked == true;
            Settings.Default.CompanyFilesGitRepo = CompanyFilesGitRepo.Text;
            Settings.Default.CompanyFilesLocalFolderText = CompanyFilesLocalFolderText.Text;

            Settings.Default.CreateProjectRootFolderText = CreateProjectRootFolderText.Text;
            Settings.Default.CreateCompanyName = CreateCompanyName.Text;

            Settings.Default.Save();
        }

        private void BIADemoLocalFolder_Checked(object sender, RoutedEventArgs e)
        {
            BIADemoLocalFolderText.IsEnabled = true;
            BIADemoLocalFolderBrowse.IsEnabled = true;
            BIADemoLocalFolderSync.IsEnabled = true;
            isCreateFrameworkVersionInitialized = false;
        }

        private void BIADemoGitHub_Checked(object sender, RoutedEventArgs e)
        {
            BIADemoLocalFolderText.IsEnabled = false;
            BIADemoLocalFolderBrowse.IsEnabled = false;
            BIADemoLocalFolderSync.IsEnabled = false;
            isCreateFrameworkVersionInitialized = false;
        }

        private void BIADemoLocalFolderText_TextChanged(object sender, RoutedEventArgs e)
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

        private async void BIADemoLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            BIADemoLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            consoleWriter.AddMessageLine("Synchronize BIADemo local folder...", Brushes.Pink);
            await RunScript($"cd " + BIADemoLocalFolderText.Text + $" \r\n" + $"git pull");

            consoleWriter.AddMessageLine("Synchronize BIADemo local folder finished", Brushes.Green);

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            BIADemoLocalFolderSync.IsEnabled = true;

        }

        private async void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            consoleWriter.AddMessageLine("Synchronize Company files local folder...", Brushes.Pink);
            await RunScript($"cd " + CompanyFilesLocalFolderText.Text + $" \r\n" + $"git pull");

            consoleWriter.AddMessageLine("Synchronize Company files local folder finished", Brushes.Green);

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            CompanyFilesLocalFolderSync.IsEnabled = true;
        }

        private void BIADemoLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.BrowseFolder(BIADemoLocalFolderText);
        }

        private void CompanyFilesLocalFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.BrowseFolder(CompanyFilesLocalFolderText);
        }

        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output. 
        /// </summary>
        /// <param name="scriptContents">The script file contents.</param>
        /// <param name="scriptParameters">A dictionary of parameter names and parameter values.</param>
        public async Task<string> RunScript(string scriptContents, Dictionary<string, object> scriptParameters = null)
        {
            string output = "";
            // create a new hosted PowerShell instance using the default runspace.
            // wrap in a using statement to ensure resources are cleaned up.

            using (PowerShell ps = PowerShell.Create())
            {
                // specify the script code to run.
                ps.AddScript(scriptContents);

                // specify the parameters to pass into the script.
                if (scriptParameters!= null) ps.AddParameters(scriptParameters);

                // execute the script and await the result.
                var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(true);

                // print the resulting pipeline objects to the console.
                foreach (var item in pipelineObjects)
                {
                    Trace.WriteLine(item.BaseObject.ToString());
                    consoleWriter.AddMessageLine(item.BaseObject.ToString(), Brushes.White);
                    //output += item.BaseObject.ToString() + $" \r\n";
                }
            }
            return output;
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
                    if (BIADemoLocalFolder.IsChecked == true)
                    {
                        string biaDemoPath = BIADemoLocalFolderText.Text;
                        //string[] versionDirectories = Directory.GetDirectories(biaDemoPath + "\\Docs\\Templates", "V*.*.*", SearchOption.TopDirectoryOnly);
                        if (!Directory.Exists(biaDemoPath))
                        {
                            MessageBox.Show("Error on BIADemo local folder :\r\nThe path " + biaDemoPath + " do not exist.\r\n Correct it in config tab.");
                        }
                        else
                        {
                            biaDemoBIATemplatePath = biaDemoPath + "\\Docs\\Templates";
                            if (!Directory.Exists(biaDemoBIATemplatePath))
                            {
                                MessageBox.Show("Error on BIADemo local folder do not contain folder DOCS\\Templates :\r\n " + biaDemoBIATemplatePath + " do not exist.\r\nCorrect it or synchronize it in config tab.");
                            }
                            else
                            {
                                DirectoryInfo di = new DirectoryInfo(biaDemoBIATemplatePath);
                                // Create an array representing the files in the current directory.
                                DirectoryInfo[] versionDirectories = di.GetDirectories("V*.*.*", SearchOption.TopDirectoryOnly);
                                // Print out the names of the files in the current directory.
                                foreach (DirectoryInfo dir in versionDirectories)
                                {
                                    //Add and select the last added
                                    lastItemFrameworkVersion = CreateFrameworkVersion.Items.Add(dir.Name);
                                }
                            }
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
                if (CreateFrameworkVersion.SelectedValue.ToString() == CreateCompanyFileVersion.Items[i].ToString())
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
                MessageBox.Show("The project path is not empty : " +projectPath);
                return;
            }

            string biaDemoBIATemplatePathVersion = biaDemoBIATemplatePath + "\\" + CreateFrameworkVersion.SelectedValue;

            string dotnetZipPath = biaDemoBIATemplatePathVersion + "\\BIA.DotNetTemplate." + CreateFrameworkVersion.SelectedValue.ToString().Substring(1) + ".zip";
            string angularZipPath = biaDemoBIATemplatePathVersion + "\\BIA.AngularTemplate." + CreateFrameworkVersion.SelectedValue.ToString().Substring(1) + ".zip";

            consoleWriter.AddMessageLine("Unzip dotnet.", Brushes.Pink);
            ZipFile.ExtractToDirectory(dotnetZipPath, projectPath);
            consoleWriter.AddMessageLine("Unzip angular.", Brushes.Pink);
            ZipFile.ExtractToDirectory(angularZipPath, projectPath);

            IList<string> filesToRemove = new List<string>() { "new-angular-project.ps1" };

            if (UseCompanyFile.IsChecked == true)
            {
                consoleWriter.AddMessageLine("Start copy company files.", Brushes.Pink);

                IList<string> filesToExclude = new List<string>() { "\\.biaCompanyFiles" };
                foreach (CFOption option in cfSettings.Options)
                {
                    if (option.IsChecked)
                    {
                        if (option.FilesToRemove!=null)
                        {
                            // Remove file of this profile
                            foreach (string fileToRemove in option.FilesToRemove)
                            {
                                filesToRemove.Add(fileToRemove);
                            }
                        }
                    }
                    else
                    {
                        if (option.Files!= null)
                        {
                            // Exclude file of this profile
                            foreach (string fileToExclude in option.Files)
                            {
                                filesToExclude.Add(fileToExclude);
                            }
                        }
                    }
                }
                FileTransform.CopyFilesRecursively(companyFilesPath, projectPath, CreateCompanyFileProfile.Text, filesToExclude);
            }

            if (filesToRemove.Count > 0)
            {
                FileTransform.RemoveRecursively(projectPath, filesToRemove);
            }

            consoleWriter.AddMessageLine("Start rename.", Brushes.Pink);
            FileTransform.ReplaceInFileAndFileName(projectPath, "TheBIADevCompany", CreateCompanyName.Text, FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "BIATemplate", CreateProjectName.Text, FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "thebiadevcompany", CreateCompanyName.Text.ToLower(), FileTransform.replaceInFileExtenssions);
            FileTransform.ReplaceInFileAndFileName(projectPath, "biatemplate", CreateProjectName.Text.ToLower(), FileTransform.replaceInFileExtenssions);

            consoleWriter.AddMessageLine("Create project finished.", Brushes.Green);
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private async void CreateCompanyFileVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateCompanyFileProfile.Items.Clear();
            companyFilesPath = CompanyFilesLocalFolderText.Text + "\\" + CreateCompanyFileVersion.SelectedValue;
            string fileName = companyFilesPath + "\\.biaCompanyFiles";
            string jsonString = File.ReadAllText(fileName);


            cfSettings = JsonSerializer.Deserialize<CFSettings>(jsonString);

            int lastIndex = -1;
            foreach (string profile in cfSettings.Profiles)
            {
                lastIndex = CreateCompanyFileProfile.Items.Add(profile);
            }
            if (lastIndex != -1) CreateCompanyFileProfile.SelectedIndex = lastIndex;

            CreateGridOption.Children.Clear();
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
                checkbox.Margin = new Thickness(0, top, 0,0);
                checkbox.Checked += CreateCompanyFileOtption_Checked;
                checkbox.Unchecked += CreateCompanyFileOtption_Checked;
                top += 25;
                checkbox.VerticalAlignment = VerticalAlignment.Top;
                CreateGridOption.Children.Add(checkbox);
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
    }
}
