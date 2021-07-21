namespace BIAToolKit
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Properties;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
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
        public MainWindow()
        {
            InitializeComponent();

            consoleWriter = new ConsoleWriter(OutputText, OutputTextViewer);

            BIADemoGitHub.IsChecked = Settings.Default.BIADemoGitHub;
            BIADemoLocalFolder.IsChecked = Settings.Default.BIADemoLocalFolder;
            BIADemoLocalFolderText.Text = Settings.Default.BIADemoLocalFolderText;

            CompanyFilesGit.IsChecked = Settings.Default.CompanyFilesGit;
            CompanyFilesLocalFolder.IsChecked = Settings.Default.CompanyFilesLocalFolder;
            CompanyFilesGitRepo.Text = Settings.Default.CompanyFilesGitRepo;
            CompanyFilesLocalFolderText.Text = Settings.Default.CompanyFilesLocalFolderText;

            CreateProjectRootFolderText.Text = Settings.Default.CreateProjectRootFolderText;
            CreateCompanyName.Text = Settings.Default.CreateCompanyName;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BIADemoGitHub = BIADemoGitHub.IsChecked == true;
            Settings.Default.BIADemoLocalFolder = BIADemoLocalFolder.IsChecked == true;
            Settings.Default.BIADemoLocalFolderText = BIADemoLocalFolderText.Text;

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

        private void BIADemoLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            BIADemoLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            consoleWriter.AddMessageLine("Synchronize BIADemo local folder...", Brushes.Green);
            _ = RunScript($"cd " + BIADemoLocalFolderText.Text + $" \r\n" + $"git pull");

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            BIADemoLocalFolderSync.IsEnabled = true;

        }

        private void CompanyFilesLocalFolderSync_Click(object sender, RoutedEventArgs e)
        {
            CompanyFilesLocalFolderSync.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            consoleWriter.AddMessageLine("Synchronize Company files local folder...", Brushes.Green);
            _ = RunScript($"cd " + CompanyFilesLocalFolderText.Text + $" \r\n" + $"git pull");

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
                var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);

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
                            string biaDemoBIATemplatePath = biaDemoPath + "\\Docs\\BIATemplate";
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
                isCreateFrameworkVersionInitialized = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
