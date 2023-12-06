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
            vm.CRUDZipDataList.Clear();
            vm.ZipDotNetSelected.Clear();
            vm.ZipAngularSelected.Clear();
            vm.DotNetZipFilesContent.Clear();
            vm.AngularZipContentFiles.Clear();

            // List Dto files from Dto folder
            vm.DtoFiles = ListDtoFiles();

            // Read settings
            string dotnetFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, Constants.FolderDoc);
            string angularFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderAngular, Constants.FolderDoc);
            vm.CRUDZipDataList.Add(new CRUDTypeData(CRUDType.Back, this.crudSettings.Back_DotNet, dotnetFolderPath));
            vm.CRUDZipDataList.Add(new CRUDTypeData(CRUDType.Feature, this.crudSettings.Feature_Angular, angularFolderPath));
            vm.CRUDZipDataList.Add(new CRUDTypeData(CRUDType.Option, this.crudSettings.Option_Angular, angularFolderPath));
            vm.CRUDZipDataList.Add(new CRUDTypeData(CRUDType.Team, this.crudSettings.Team_Angular, angularFolderPath));

            // Set combobobox and checkbox enabled
            vm.IsEnable = true;
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
        private void GenerateCrudBack_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.CRUDZipDataList == null) return;

            CRUDTypeData crudZipData = vm.CRUDZipDataList.Where(x => x.Type == CRUDType.Back).FirstOrDefault();
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
        private void GenerateCrudFeature_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.CRUDZipDataList == null) return;

            CRUDTypeData crudZipData = vm.CRUDZipDataList.Where(x => x.Type == CRUDType.Feature).FirstOrDefault();
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
        private void GenerateCrudTeam_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.CRUDZipDataList == null) return;

            CRUDTypeData crudZipData = vm.CRUDZipDataList.Where(x => x.Type == CRUDType.Team).FirstOrDefault();
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
        private void GenerateCrudOption_Change(object sender, RoutedEventArgs e)
        {
            if (vm == null || vm.CRUDZipDataList == null) return;

            CRUDTypeData crudZipData = vm.CRUDZipDataList.Where(x => x.Type == CRUDType.Option).FirstOrDefault();
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
            // Parse DotNet Zip files
            if (vm.ZipDotNetSelected.Count > 0)
            {
                ParseDotNetZipFile();
            }

            // Parse Angular Zip files
            if (vm.ZipAngularSelected.Count > 0)
            {
                ParseAngularZipFile();
            }

            vm.IsZipParsed = true;
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlurial, crudSettings.DtoReferenceSingular, crudSettings.DtoReferencePlurial);

            // Generation DotNet files
            string path = crudService.GenerateDotNetCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.DotNetZipFilesContent);

            // Generation Angular files
            ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).First();
            crudService.GenerateAngularCrudFiles(this.entityName, vm.CurrentProject, vm.DtoEntity, vm.AngularZipContentFiles/*, cd*/);

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
                consoleWriter.AddMessageLine("Dto file not found to parse.", "Orange");
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
                CRUDTypeData crudZipData = GetCRUDTypeData(CRUDType.Back, vm.ZipDotNetSelected.ToList());
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
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderDotNet, CRUDType.Back);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
                        return;
                    }

                    vm.DotNetZipFilesContent.Clear();
                    if (fileList.Count > 0)
                    {
                        foreach (FileInfo fi in new DirectoryInfo(workingDirectoryPath).GetFiles())
                        {
                            ClassDefinition classFile = service.ParseClassFile(fi.FullName);
                            classFile.PathOnZip = fileList[fi.Name];
                            FileType? type = zipService.GetFileType(fi.Name);
                            classFile.FileType = type;
                            classFile.EntityName = zipService.GetEntityName(fi.Name, type);
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
        /// Parse Angular Zip files (feature, option team).
        /// </summary>
        private void ParseAngularZipFile()
        {
            try
            {
                // Parse Feature Zip file
                CRUDTypeData crudZipDataFtr = GetCRUDTypeData(CRUDType.Feature, vm.ZipAngularSelected.ToList());
                if (crudZipDataFtr != null)
                {
                    string fileName = Path.Combine(crudZipDataFtr.ZipPath, crudZipDataFtr.ZipName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        consoleWriter.AddMessageLine("No Angular Zip files found to parse.", "Orange");
                        return;
                    }

                    // Parse Feature Zip file
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName,
                        vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular, CRUDType.Feature);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                        return;
                    }

                    ClassDefinition cd = vm.DotNetZipFilesContent.Where(x => x.FileType == FileType.Dto).FirstOrDefault();
                    if (cd == null)
                    {
                        consoleWriter.AddMessageLine("Can't parse angular files, 'PlaneDto' file not found.", "Orange");
                        return;
                    }

                    Dictionary<string, List<string>> planeDtoProperties = zipService.GetDtoProperties(cd.PropertyList);
                    if (planeDtoProperties == null || planeDtoProperties.Count <= 0)
                    {
                        consoleWriter.AddMessageLine("Can't read plane dto properties.", "Orange");
                        return;
                    }

                    // Analyze angular files
                    vm.AngularZipContentFiles.Clear();
                    if (fileList.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> file in fileList)
                        {
                            CRUDAngularData data = new(file.Key, file.Value, workingDirectoryPath)
                            {
                                ExtractBlocks = zipService.AnalyzeAngularFile(Path.Combine(workingDirectoryPath, file.Key), planeDtoProperties)
                            };
                            vm.AngularZipContentFiles.Add(data);
                        }
                    }
                    else
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                    }
                }

                // TODO NMA : 
                // Parse Option Zip file
                CRUDTypeData crudZipDataOpt = GetCRUDTypeData(CRUDType.Option, vm.ZipAngularSelected.ToList());
                if (crudZipDataOpt != null)
                {
                    string fileName = Path.Combine(crudZipDataOpt.ZipPath, crudZipDataOpt.ZipName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        consoleWriter.AddMessageLine("No Angular Zip files found to parse.", "Orange");
                        return;
                    }

                    // Parse Option Zip file
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, this.entityName,
                        vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular, CRUDType.Option);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                        return;
                    }



                }

                // TODO NMA : 
                // Parse Team Zip file
                CRUDTypeData crudZipDataTm = GetCRUDTypeData(CRUDType.Team, vm.ZipAngularSelected.ToList());
                if (crudZipDataTm != null)
                {
                    consoleWriter.AddMessageLine("Parsing Team Zip file not implemented.", "Red");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }
        }

        private CRUDTypeData GetCRUDTypeData(CRUDType type, List<string> zipSelected)
        {
            if (zipSelected == null || zipSelected.Count <= 0) { return null; }

            bool found = false;
            CRUDTypeData crudZipData = vm.CRUDZipDataList.Where(x => x.Type == type && x.IsChecked).FirstOrDefault();
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

        public string DtoReferenceSingular { get; }
        public string DtoReferencePlurial { get; }
        public string Back_DotNet { get; }
        public string Feature_Angular { get; }
        public string Option_Angular { get; }
        public string Team_Angular { get; }

        public CRUDSettings(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;

            DtoReferenceSingular = ReadSetting("DtoReferenceSingular");
            DtoReferencePlurial = ReadSetting("DtoReferencePlurial");
            Back_DotNet = ReadSetting("CRUD_Back_DotNet");
            Feature_Angular = ReadSetting("CRUD_Feature_Angular");
            Option_Angular = ReadSetting("CRUD_Option_Angular");
            Team_Angular = ReadSetting("CRUD_Team_Angular");
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
