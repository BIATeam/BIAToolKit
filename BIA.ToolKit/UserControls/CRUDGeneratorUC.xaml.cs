namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        IConsoleWriter consoleWriter;
        CSharpParserService service;
        ZipParserService zipService;
        GenerateCrudService crudService;
        private string entityName;
        private CRUDSettings crudSettings;
        private readonly CRUDGeneratorViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, IConsoleWriter consoleWriter)
        {
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.consoleWriter = consoleWriter;
            this.crudSettings = new(consoleWriter);
        }

        /// <summary>
        /// Update CurrentProject value.
        /// </summary>
        public void SetCurrentProject(Project currentProject)
        {
            vm.CurrentProject = currentProject;
            CurrentProjectChange();
        }

        /// <summary>
        /// Init data based on current page (from settings).
        /// </summary>
        private void CurrentProjectChange()
        {
            // Clean all lists (in case of current project change)
            vm.FeatureTypeDataList.Clear();
            vm.ZipDotNetSelected.Clear();
            vm.ZipAngularSelected.Clear();
            vm.DotNetZipFilesContent.Clear();
            vm.AngularZipFilesContent.Clear();

            // List Dto files from Dto folder
            vm.DtoFiles = ListDtoFiles();

            // Read settings
            string dotnetFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, Constants.FolderDoc);
            string angularFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderAngular, Constants.FolderDoc);
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Back, this.crudSettings.ZipNameBack, dotnetFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.CRUD, this.crudSettings.ZipNameFeature, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Option, this.crudSettings.ZipNameOption, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Team, this.crudSettings.ZipNameTeam, angularFolderPath));

            // Set combobobox and checkbox enabled
            vm.IsProjectChosen = true;
        }

        #region State change
        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;
            vm.IsDtoParsed = false;
            this.entityName = crudService.GetEntityNameFromDto(vm.DtoSelected);
            vm.CRUDNameSingular = this.entityName;
        }

        /// <summary>
        /// Action linked with "Entity name (singular)" textbox.
        /// </summary>
        private void ModifyEntitySingularText_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlurial = string.Empty;
        }

        /// <summary>
        /// Action linked with "Entity name (plurial)" textbox.
        /// </summary>
        private void ModifyEntityPlurialText_TextChanged(object sender, TextChangedEventArgs e)
        {

            vm.IsCheckedAction = true;
        }

        /// <summary>
        /// Action linked with "Generate back" checkbox.
        /// </summary>
        private void GenerateBackChk_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.FeatureTypeDataList == null) return;

            FeatureTypeData crudZipData = vm.FeatureTypeDataList.Where(x => x.Type == FeatureType.Back).FirstOrDefault();
            if (crudZipData != null)
            {
                crudZipData.IsChecked = (bool)((CheckBox)sender).IsChecked;
                if (crudZipData.IsChecked)
                {
                    vm.ZipDotNetSelected.Add(crudZipData.ZipName);
                }
                else
                {
                    vm.ZipDotNetSelected.Remove(crudZipData.ZipName);
                }

                vm.IsZipParsed = false;
                vm.IsCheckedAction = true;
            }
            else
            {
                consoleWriter.AddMessageLine("No DotNet Zip.", "Rouge");
            }
        }

        /// <summary>
        /// Action linked with "Generate feature" checkbox.
        /// </summary>
        private void GenerateCrudChk_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.FeatureTypeDataList == null) return;

            FeatureTypeData crudZipData = vm.FeatureTypeDataList.Where(x => x.Type == FeatureType.CRUD).FirstOrDefault();
            if (crudZipData != null)
            {
                crudZipData.IsChecked = (bool)((CheckBox)sender).IsChecked;
                if (crudZipData.IsChecked)
                {
                    vm.ZipAngularSelected.Add(crudZipData.ZipName);
                }
                else
                {
                    vm.ZipAngularSelected.Remove(crudZipData.ZipName);
                }

                vm.IsZipParsed = false;
                vm.IsCheckedAction = true;
            }
            else
            {
                consoleWriter.AddMessageLine("No CRUD Feature Zip.", "Rouge");
            }
        }

        /// <summary>
        /// Action linked with "Generate team" checkbox.
        /// </summary>
        private void GenerateTeamChk_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.FeatureTypeDataList == null) return;

            FeatureTypeData crudZipData = vm.FeatureTypeDataList.Where(x => x.Type == FeatureType.Team).FirstOrDefault();
            if (crudZipData != null)
            {
                crudZipData.IsChecked = (bool)((CheckBox)sender).IsChecked;
                if (crudZipData.IsChecked)
                {
                    vm.ZipAngularSelected.Add(crudZipData.ZipName);
                }
                else
                {
                    vm.ZipAngularSelected.Remove(crudZipData.ZipName);
                }

                vm.IsZipParsed = false;
                vm.IsCheckedAction = true;
            }
            else
            {
                consoleWriter.AddMessageLine("No CRUD Team Zip.", "Rouge");
            }
        }

        /// <summary>
        /// Action linked with "Generate option" checkbox.
        /// </summary>
        private void GenerateOptionChk_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.FeatureTypeDataList == null) return;

            FeatureTypeData crudZipData = vm.FeatureTypeDataList.Where(x => x.Type == FeatureType.Option).FirstOrDefault();
            if (crudZipData != null)
            {
                crudZipData.IsChecked = (bool)((CheckBox)sender).IsChecked;
                if (crudZipData.IsChecked)
                {
                    vm.ZipAngularSelected.Add(crudZipData.ZipName);
                }
                else
                {
                    vm.ZipAngularSelected.Remove(crudZipData.ZipName);
                }

                vm.IsZipParsed = false;
                vm.IsCheckedAction = true;
            }
            else
            {
                consoleWriter.AddMessageLine("No CRUD Option Zip.", "Rouge");
            }
        }
        #endregion

        #region Button Action
        /// <summary>
        /// Action linked with "Parse Dto" button.
        /// </summary>
        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            ParseDtoFile();
            vm.IsDtoParsed = true;
        }

        /// <summary>
        /// Action linked with "Parse Zip" button.
        /// </summary>
        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            vm.DotNetZipFilesContent.Clear();
            vm.AngularZipFilesContent.Clear();

            // Parse DotNet Zip files
            if (vm.ZipDotNetSelected.Count > 0)
            {
                ParseDotNetZipFile();
            }

            // Parse Angular Zip files
            if (vm.ZipAngularSelected.Count > 0)
            {
                // Parse CRUD Zip file
                ParseAngularZipFile(FeatureType.CRUD);

                // Parse Option Zip file
                ParseAngularZipFile(FeatureType.Option);

                // Parse Team Zip file
                ParseAngularZipFile(FeatureType.Team);
            }

            vm.IsZipParsed = true;
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlurial, crudSettings.CRUDReferenceSingular, crudSettings.CRUDReferencePlurial);

            // Generation DotNet files
            string path = crudService.GenerateDotNetCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.DotNetZipFilesContent);

            // Generation Angular files
            ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).FirstOrDefault();
            crudService.GenerateAngularCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.AngularZipFilesContent);

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        #endregion

        #region Private method
        /// <summary>
        /// List all Dto files from current project.
        /// </summary>
        private Dictionary<string, string> ListDtoFiles()
        {
            Dictionary<string, string> dtoFiles = new();

            string dtoFolder = $"{vm.CurrentProject.CompanyName}.{vm.CurrentProject.Name}.Domain.Dto";
            string path = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, dtoFolder);

            try
            {
                // List files
                var files = Directory.GetFiles(path, "*Dto.cs", SearchOption.AllDirectories).ToList();
                // Build dictionnary: key = file Name, Value = full path
                files.ForEach(x => dtoFiles.Add(new FileInfo(x).Name, new FileInfo(x).FullName));
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            return dtoFiles;
        }

        /// <summary>
        /// Parse the Dto file.
        /// </summary>
        private void ParseDtoFile()
        {
            string fileName = vm.DtoFiles[vm.DtoSelected];
            if (string.IsNullOrWhiteSpace(fileName))
            {
                consoleWriter.AddMessageLine($"Dto file '{fileName}' not found to parse.", "Orange");
                return;
            }

            try
            {
                vm.DtoEntity = service.ParseEntity(fileName);
                if (vm.DtoEntity == null || vm.DtoEntity.Properties == null || vm.DtoEntity.Properties.Count <= 0)
                {
                    consoleWriter.AddMessageLine("No properties found on Dto file.", "Orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        /// <summary>
        /// Parse DotNet Zip file (back).
        /// </summary>
        private void ParseDotNetZipFile()
        {
            try
            {
                FeatureTypeData crudZipData = GetFeatureTypeData(FeatureType.Back, vm.ZipDotNetSelected.ToList());
                // TODO NMA : if ZipDotNetSelected contains more of 1 zip file

                if (crudZipData != null)
                {
                    string fileName = Path.Combine(crudZipData.ZipPath, crudZipData.ZipName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        consoleWriter.AddMessageLine("No DotNet Zip files found to parse.", "Orange");
                        return;
                    }

                    // Parse Feature Zip file
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderDotNet, FeatureType.Back);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
                        return;
                    }

                    if (fileList.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> file in fileList)
                        {
                            ClassDefinition classFile = service.ParseClassFile(Path.Combine(workingDirectoryPath, file.Key));
                            classFile.PathOnZip = file.Key;
                            FileType? type = zipService.GetFileType(file.Value);
                            classFile.FileType = type;
                            classFile.EntityName = zipService.GetEntityName(file.Value, type);
                            vm.DotNetZipFilesContent.Add(classFile);

                        }
                    }
                    else
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        /// <summary>
        /// Parse Angular Zip files (CRUD, option or team).
        /// </summary>
        private void ParseAngularZipFile(FeatureType type)
        {
            Dictionary<string, List<string>> planeDtoProperties = null;

            try
            {
                FeatureTypeData zipData = GetFeatureTypeData(type, vm.ZipAngularSelected.ToList());
                if (zipData != null)
                {
                    string fileName = Path.Combine(zipData.ZipPath, zipData.ZipName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        consoleWriter.AddMessageLine($"No {type} Zip files found to parse.", "Orange");
                        return;
                    }

                    // Parse Zip file
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName,
                        vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular, type);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                        return;
                    }

                    if (type == FeatureType.CRUD)
                    {
                        ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).FirstOrDefault();
                        if (cd == null)
                        {
                            consoleWriter.AddMessageLine("Can't parse angular files, 'PlaneDto' file not found.", "Orange");
                            return;
                        }

                        planeDtoProperties = zipService.GetDtoProperties(cd.PropertyList);
                        if (planeDtoProperties == null || planeDtoProperties.Count <= 0)
                        {
                            consoleWriter.AddMessageLine("Can't read plane dto properties.", "Orange");
                            return;
                        }
                    }

                    // Analyze angular files
                    if (fileList.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> file in fileList)
                        {
                            AngularFeatureData data = new(type, file.Value, file.Key, workingDirectoryPath);
                            if (type == FeatureType.CRUD)
                            {
                                data.ExtractBlocks = zipService.AnalyzeAngularFile(Path.Combine(workingDirectoryPath, file.Key), planeDtoProperties);
                            }
                            vm.AngularZipFilesContent.Add(data);
                        }
                    }
                    else
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private FeatureTypeData GetFeatureTypeData(FeatureType type, List<string> zipSelected)
        {
            if (zipSelected == null || zipSelected.Count <= 0) { return null; }

            bool found = false;
            FeatureTypeData crudZipData = vm.FeatureTypeDataList.Where(x => x.Type == type && x.IsChecked).FirstOrDefault();
            if (crudZipData != null)
            {
                zipSelected.ForEach(x =>
                {
                    if (x == crudZipData.ZipName)
                    {
                        found = true;
                        return;
                    }
                });
            }

            return found ? crudZipData : null;
        }
        #endregion
    }

    class CRUDSettings
    {
        private readonly IConsoleWriter consoleWriter;

        public string CRUDReferenceSingular { get; }
        public string CRUDReferencePlurial { get; }
        public string OptionReferenceSingular { get; }
        public string OptionReferencePlurial { get; }
        public string TeamReferenceSingular { get; }
        public string TeamReferencePlurial { get; }
        public string ZipNameBack { get; }
        public string ZipNameFeature { get; }
        public string ZipNameOption { get; }
        public string ZipNameTeam { get; }

        public CRUDSettings(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;

            CRUDReferenceSingular = ReadSetting("CrudReferenceSingular");
            CRUDReferencePlurial = ReadSetting("CrudReferencePlurial");
            OptionReferenceSingular = ReadSetting("OptionReferenceSingular");
            OptionReferencePlurial = ReadSetting("OptionReferencePlurial");
            TeamReferenceSingular = ReadSetting("TeamReferenceSingular");
            TeamReferencePlurial = ReadSetting("TeamReferencePlurial");

            ZipNameBack = ReadSetting("ZipNameBack_DotNet");
            ZipNameFeature = ReadSetting("ZipNameFeature_Angular");
            ZipNameOption = ReadSetting("ZipNameOption_Angular");
            ZipNameTeam = ReadSetting("ZipNameTeam_Angular");
        }

        private string ReadSetting(string key)
        {
            string result = null;
            try
            {
                result = ConfigurationManager.AppSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException)
            {
                consoleWriter.AddMessageLine($"Error reading app settings (key={key})", "Red");
            }
            return result;
        }
    }
}
