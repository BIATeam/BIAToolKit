﻿namespace BIA.ToolKit.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;
    using BIA.ToolKit.Domain.Work;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        BIATKSettings settings;
        GitService gitService;
        RepositoryService repositoryService;
        IConsoleWriter consoleWriter;
        FeatureSettingService featureSettingService;
        private string currentProjectPath;

        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            vm = (VersionAndOptionViewModel)base.DataContext;
        }

        public void Inject(BIATKSettings settings, RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, FeatureSettingService featureSettingService)
        {
            this.settings = settings;
            this.gitService = gitService;
            this.consoleWriter = consoleWriter;
            this.repositoryService = repositoryService;
            this.featureSettingService = featureSettingService;
        }

        public void SelectVersion(string version)
        {
            vm.WorkTemplate = vm.WorkTemplates.FirstOrDefault(workTemplate => workTemplate.Version == $"V{version}");
        }

        public void SetCurrentProjectPath(string path)
        {
            this.currentProjectPath = path;
            this.LoadfeatureSetting();
        }

        private void LoadfeatureSetting()
        {
            List<FeatureSetting> featureSettings = this.featureSettingService.Get(vm.WorkTemplate?.VersionFolderPath);
            List<FeatureSetting> projectFeatureSettings = this.featureSettingService.Get(this.currentProjectPath);

            if (featureSettings?.Any() == true && projectFeatureSettings?.Any() == true)
            {
                foreach (FeatureSetting featureSetting in featureSettings)
                {
                    FeatureSetting projectFeatureSetting = projectFeatureSettings.Find(x => x.Id == featureSetting.Id);

                    if (projectFeatureSetting != null)
                    {
                        featureSetting.IsSelected = projectFeatureSetting.IsSelected;
                    }
                }
            }

            featureSettings = featureSettings ?? new List<FeatureSetting>();
            vm.FeatureSettings = new ObservableCollection<FeatureSetting>(featureSettings);
        }

        public async Task FillVersionFolderPathAsync()
        {
            if (vm?.WorkTemplate?.RepositorySettings != null)
            {
                if (vm.WorkTemplate.Version == "VX.Y.Z")
                {
                    vm.WorkTemplate.VersionFolderPath = vm.WorkTemplate.RepositorySettings.RootFolderPath;
                }
                else
                {
                    vm.WorkTemplate.VersionFolderPath = await this.repositoryService.PrepareVersionFolder(vm.WorkTemplate.RepositorySettings, vm.WorkTemplate.Version);
                }
            }
        }

        public void refreshConfig()
        {
            var listCompanyFiles = new List<WorkRepository>();
            var listWorkTemplates = new List<WorkRepository>();


            if (settings.CustomRepoTemplates?.Count > 0)
            {
                foreach (var repositorySettings in settings.CustomRepoTemplates)
                {
                    AddTemplatesVersion(listWorkTemplates, repositorySettings);
                }
            }

            if (Directory.Exists(settings.BIATemplateRepository.RootFolderPath))
            {
                AddTemplatesVersion(listWorkTemplates, settings.BIATemplateRepository);
                listWorkTemplates.Add(new WorkRepository(settings.BIATemplateRepository, "VX.Y.Z"));
            }

            vm.WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
            if (listWorkTemplates.Count >= 2)
            {
                vm.WorkTemplate = listWorkTemplates[listWorkTemplates.Count - 2];
            }

            vm.UseCompanyFiles = settings.UseCompanyFiles;
            if (settings.UseCompanyFiles)
            {
                UseCompanyFiles.Visibility = Visibility.Visible;
                //CompanyFileGroup.Visibility = Visibility.Visible;

                //Warning.Visibility = Visibility.Hidden;
                //WarningLabel.Visibility = Visibility.Hidden;

                AddTemplatesVersion(listCompanyFiles, settings.CompanyFiles);
                vm.WorkCompanyFiles = new ObservableCollection<WorkRepository>(listCompanyFiles);
                if (listCompanyFiles.Count >= 1)
                {
                    vm.WorkCompanyFile = listCompanyFiles.FirstOrDefault(companyFile => companyFile.Version == vm.WorkTemplate.Version);
                }
            }
            else
            {
                UseCompanyFiles.Visibility = Visibility.Hidden;
                //CompanyFileGroup.Visibility = Visibility.Hidden;
                //Warning.Visibility = Visibility.Visible;
                //WarningLabel.Visibility = Visibility.Visible;
            }
        }

        private void UseCompanyFile_Checked(object sender, RoutedEventArgs e)
        {
            /*if (vm.UseCompanyFiles)
            {
                CompanyFileGroup.Visibility = Visibility.Visible;
            }
            else
            {
                CompanyFileGroup.Visibility = Visibility.Hidden;
            }*/
        }

        private void AddTemplatesVersion(List<WorkRepository> WorkTemplates, RepositorySettings repositorySettings)
        {
            if (repositorySettings.Versioning == VersioningType.Folder)
            {
                if (Directory.Exists(repositorySettings.RootFolderPath))
                {
                    DirectoryInfo di = new DirectoryInfo(repositorySettings.RootFolderPath);
                    //  an array representing the files in the current directory.
                    DirectoryInfo[] versionDirectories = di.GetDirectories("V*.*.*", SearchOption.TopDirectoryOnly);
                    // Print out the names of the files in the current directory.
                    foreach (DirectoryInfo dir in versionDirectories)
                    {
                        //Add and select the last added
                        WorkTemplates.Add(new WorkRepository(repositorySettings, dir.Name));
                    }
                }
            }
            else
            {
                List<string> versions = gitService.GetTags(repositorySettings.RootFolderPath).OrderBy(q => q).ToList();

                foreach (string version in versions)
                {
                    WorkTemplates.Add(new WorkRepository(repositorySettings, version));
                }
            }

            WorkTemplates.Sort(new WorkRepository.VersionComparer());
        }

        private async void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.UseCompanyFiles = vm.WorkTemplate?.RepositorySettings?.Name == "BIATemplate";

            foreach (var WorkCompanyFile in vm.WorkCompanyFiles)
            {
                if (vm.WorkTemplate?.Version == WorkCompanyFile.Version)
                {
                    vm.WorkCompanyFile = WorkCompanyFile;
                }
            }

            await this.FillVersionFolderPathAsync();
            this.LoadfeatureSetting();
        }

        private void CompanyFileVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listProfiles = new List<string>();

            GridOption.Children.Clear();

            if (vm.WorkCompanyFile != null)
            {
                vm.WorkCompanyFile.VersionFolderPath = repositoryService.PrepareVersionFolder(vm.WorkCompanyFile.RepositorySettings, vm.WorkCompanyFile.Version).Result;
                string fileName = vm.WorkCompanyFile.VersionFolderPath + "\\biaCompanyFiles.json";

                try
                {
                    string jsonString = File.ReadAllText(fileName);

                    CFSettings cfSetting = JsonSerializer.Deserialize<CFSettings>(jsonString);

                    foreach (string profile in cfSetting.Profiles)
                    {
                        listProfiles.Add(profile);
                        vm.Profile = profile;
                    }
                    vm.Profiles = new ObservableCollection<string>(listProfiles);

                    int top = 0;
                    vm.Options = new List<CFOption>();
                    foreach (CFOption option in cfSetting.Options)
                    {
                        option.IsChecked = (!(option?.Default == 0));
                        vm.Options.Add(option);

                        CheckBox checkbox = new CheckBox();
                        checkbox.Content = option.Name;
                        checkbox.IsChecked = option.IsChecked;
                        checkbox.Foreground = Brushes.White;
                        checkbox.Height = 16;
                        checkbox.Name = "CFOption_" + option.Key;
                        checkbox.Margin = new Thickness(0, top, 0, 0);
                        checkbox.Checked += CompanyFileOption_Checked;
                        checkbox.Unchecked += CompanyFileOption_Checked;
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

        private void CompanyFileOption_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                CheckBox chx = (CheckBox)sender;
                foreach (CFOption option in vm.Options)
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
