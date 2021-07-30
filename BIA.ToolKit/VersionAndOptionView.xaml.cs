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
    public partial class VersionAndOptionView : UserControl
    {
        Configuration configuration;
        GitService gitService;
        IConsoleWriter consoleWriter;

        public string CompanyFilesPath { get; private set; }
        public CFSettings cfSettings { get; private set; }

        public VersionAndOptionView(GitService gitService, IConsoleWriter consoleWriter)
        {
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            InitializeComponent();
        }

        public void refreshConfig(Configuration configuration)
        {
            this.configuration = configuration;
        }

        private void CreateFrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < CreateCompanyFileVersion.Items.Count; i++)
            {
                if (CreateFrameworkVersion.SelectedValue?.ToString() == CreateCompanyFileVersion.Items[i].ToString())
                {
                    CreateCompanyFileVersion.SelectedIndex = i;
                }
            }
        }

        private async void CreateCompanyFileVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateCompanyFileProfile.Items.Clear();
            CreateGridOption.Children.Clear();
            if (!string.IsNullOrEmpty(CreateCompanyFileVersion.SelectedValue?.ToString()))
            {
                CompanyFilesPath = configuration.CompanyFilesLocalFolderText + "\\" + CreateCompanyFileVersion.SelectedValue;
                string fileName = CompanyFilesPath + "\\biaCompanyFiles.json";
                string jsonString = File.ReadAllText(fileName);

                try
                {

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
                    consoleWriter.AddMessageLine(ex.Message, "Red");
                }
            }

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
