namespace BIAToolKit
{
    using BIA.ToolKit.Helper;
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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConsoleWriter consoleWriter;
        bool isCreateFrameworkVersionInitialized = false;
        string biaDemoBIATemplatePath = "";
        string tempFolderPath = Path.GetTempPath();

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
                CreateFrameworkVersion.Items.Clear();
                var tab = sender as TabItem;
                if (tab != null)
                {
                    if (BIADemoLocalFolder.IsChecked == true)
                    {
                        string biaDemoPath = BIADemoLocalFolderText.Text;
                        //string[] versionDirectories = Directory.GetDirectories(biaDemoPath + "\\Docs\\BIATemplate", "V*.*.*", SearchOption.TopDirectoryOnly);
                        if (!Directory.Exists(biaDemoPath))
                        {
                            MessageBox.Show("Error on BIADemo local folder :\r\nThe path " + biaDemoPath + " do not exist.\r\n Correct it in config tab.");
                        }
                        else
                        {
                            biaDemoBIATemplatePath = biaDemoPath + "\\Docs\\BIATemplate";
                            if (!Directory.Exists(biaDemoBIATemplatePath))
                            {
                                MessageBox.Show("Error on BIADemo local folder do not contain BIATemplate :\r\n " + biaDemoBIATemplatePath + " do not exist.\r\nCorrect it or synchronize it in config tab.");
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
                                    CreateFrameworkVersion.SelectedIndex = CreateFrameworkVersion.Items.Add(dir.Name);
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
                                CreateCompanyFileVersion.SelectedIndex = CreateCompanyFileVersion.Items.Add(dir.Name);
                            }
                        }
                    }
                }
                else
                {
                    CreateCompanyFileVersion.Visibility = Visibility.Hidden;
                    CreateCompanyFileVersionLabel.Visibility = Visibility.Hidden;
                }

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

            if (UseCompanyFile.IsChecked == true)
            {
                consoleWriter.AddMessageLine("Start copy company files.", Brushes.Pink);
                string companyFilesPath = CompanyFilesLocalFolderText.Text + "\\" + CreateCompanyFileVersion.SelectedValue;
                FileTransform.CopyFilesRecursively(companyFilesPath, projectPath);
            }

            consoleWriter.AddMessageLine("Start rename.", Brushes.Pink);
            FileTransform.ReplaceInFileAndFileName(projectPath, "TheBIADevCompany", CreateCompanyName.Text);
            FileTransform.ReplaceInFileAndFileName(projectPath, "BIATemplate", CreateProjectName.Text);
            FileTransform.ReplaceInFileAndFileName(projectPath, "thebiadevcompany", CreateCompanyName.Text.ToLower());
            FileTransform.ReplaceInFileAndFileName(projectPath, "biatemplate", CreateProjectName.Text.ToLower());

            consoleWriter.AddMessageLine("Create project finished.", Brushes.Green);
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

    }
}
