namespace BIA.ToolKit
{
    using BIA.ToolKit.Application.CompanyFiles;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Helper;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
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
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        Configuration configuration;
        GitService gitService;
        IConsoleWriter consoleWriter;

        public string CompanyFilesPath { get; private set; }
        public CFSettings CfSettings { get; private set; }

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void Inject(Configuration configuration, GitService gitService, IConsoleWriter consoleWriter)
        {
            this.configuration = configuration;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
        }

        public void refreshConfig()
        {
            int lastItemCompanyFileVersion = -1;
            int lastItemFrameworkVersion = -1;

            FrameworkVersion.Items.Clear();
            CompanyFileVersion.Items.Clear();


            if (Directory.Exists(configuration.BIATemplatePath))
            {
                List<string> versions = gitService.GetRelease(configuration.BIATemplatePath).OrderBy(q => q).ToList();

                foreach (string version in versions)
                {
                    //Add and select the last added
                    lastItemFrameworkVersion = FrameworkVersion.Items.Add(version);
                }

                FrameworkVersion.Items.Add("VX.Y.Z");
            }

            if (configuration.UseCompanyFileIsChecked)
            {
                CompanyFileGroup.Visibility = Visibility.Visible;


                if (Directory.Exists(configuration.RootCompanyFilesPath))
                {
                    DirectoryInfo di = new DirectoryInfo(configuration.RootCompanyFilesPath);
                    //  an array representing the files in the current directory.
                    DirectoryInfo[] versionDirectories = di.GetDirectories("V*.*.*", SearchOption.TopDirectoryOnly);
                    // Print out the names of the files in the current directory.
                    foreach (DirectoryInfo dir in versionDirectories)
                    {
                        //Add and select the last added
                        lastItemCompanyFileVersion = CompanyFileVersion.Items.Add(dir.Name);
                    }
                }
            }
            else
            {
                CompanyFileGroup.Visibility = Visibility.Hidden;
            }

            if (lastItemFrameworkVersion != -1) FrameworkVersion.SelectedIndex = lastItemFrameworkVersion;
            if (lastItemCompanyFileVersion != -1) CompanyFileVersion.SelectedIndex = lastItemCompanyFileVersion;
        }

        private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < CompanyFileVersion.Items.Count; i++)
            {
                if (FrameworkVersion.SelectedValue?.ToString() == CompanyFileVersion.Items[i].ToString())
                {
                    CompanyFileVersion.SelectedIndex = i;
                }
            }
        }

        private void CompanyFileVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CompanyFileProfile.Items.Clear();
            GridOption.Children.Clear();
            if (!string.IsNullOrEmpty(CompanyFileVersion.SelectedValue?.ToString()))
            {
                CompanyFilesPath = configuration.CompanyFilesLocalFolderText + "\\" + CompanyFileVersion.SelectedValue;
                string fileName = CompanyFilesPath + "\\biaCompanyFiles.json";

                try
                {
                    string jsonString = File.ReadAllText(fileName);

                    CfSettings = JsonSerializer.Deserialize<CFSettings>(jsonString);

                    int lastIndex = -1;
                    foreach (string profile in CfSettings.Profiles)
                    {
                        lastIndex = CompanyFileProfile.Items.Add(profile);
                    }
                    if (lastIndex != -1) CompanyFileProfile.SelectedIndex = lastIndex;


                    int top = 0;

                    foreach (CFOption option in CfSettings.Options)
                    {
                        option.IsChecked = (!(option?.Default == 0));

                        //  <CheckBox Content="Otpion2" Foreground="White"  Height="16" VerticalAlignment="Top" Name="CFOption_Otpion2" Margin="0,25,0,0" />
                        CheckBox checkbox = new CheckBox();
                        checkbox.Content = option.Name;
                        checkbox.IsChecked = option.IsChecked;
                        checkbox.Foreground = Brushes.White;
                        checkbox.Height = 16;
                        checkbox.Name = "CFOption_" + option.Key;
                        checkbox.Margin = new Thickness(0, top, 0, 0);
                        checkbox.Checked += CompanyFileOtption_Checked;
                        checkbox.Unchecked += CompanyFileOtption_Checked;
                        top += 25;
                        checkbox.VerticalAlignment = VerticalAlignment.Top;
                        GridOption.Children.Add(checkbox);
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine(ex.Message, "Red");
                }
            }

        }

        private void CompanyFileOtption_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                CheckBox chx = (CheckBox)sender;
                foreach (CFOption option in CfSettings.Options)
                {
                    if ("CFOption_" + option.Key == chx.Name)
                    {
                        option.IsChecked = chx.IsChecked == true;
                    }
                }
            }
        }
    }
}
