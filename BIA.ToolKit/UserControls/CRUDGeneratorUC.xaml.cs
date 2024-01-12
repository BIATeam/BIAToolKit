namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
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
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;

            this.crudSettings = new(settingsService);
            crudService.SetSettings(this.crudSettings);
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
            // Set combobobox and checkbox enabled
            vm.IsProjectChosen = true;

            // Clean all lists (in case of current project change)
            vm.FeatureTypeDataList.Clear();
            vm.ZipDotNetSelected.Clear();
            vm.ZipAngularSelected.Clear();
            vm.ZipFilesContent.Clear();

            // List Dto files from Dto folder
            vm.DtoFiles = ListDtoFiles();

            // Read settings
            string dotnetFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, Constants.FolderDoc);
            string angularFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderAngular, Constants.FolderDoc);
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Back, this.crudSettings.ZipNameBack, dotnetFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.CRUD, this.crudSettings.ZipNameFeature, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Option, this.crudSettings.ZipNameOption, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Team, this.crudSettings.ZipNameTeam, angularFolderPath));
        }

        #region State change
        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;
            vm.IsDtoParsed = false;
            vm.CRUDNameSingular = GetEntityNameFromDto(vm.DtoSelected);
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
            vm.ZipFilesContent.Clear();

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
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlurial);

            // Generation DotNet + Angular files
            string path = crudService.GenerateCrudFiles(vm.CurrentProject, vm.DtoEntity, vm.ZipFilesContent);

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
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderDotNet, FeatureType.Back);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive not found: '{fileName}'.", "Orange");
                        return;
                    }

                    if (fileList.Count > 0)
                    {
                        ZipFilesContent filesContent = new(FeatureType.Back);
                        foreach (KeyValuePair<string, string> file in fileList)
                        {
                            BackFileType? type = zipService.GetFileType(file.Value);
                            // Ignore Dto, mapper and entity file
                            if (type == BackFileType.Mapper || type == BackFileType.Entity)
                            {
                                continue;
                            }

                            DotNetCRUDData data = new(file.Value, file.Key, workingDirectoryPath)
                            {
                                FileType = type
                            };

                            if (type == BackFileType.Dto)
                            {
                                ClassDefinition classFile = service.ParseClassFile(Path.Combine(workingDirectoryPath, file.Key));
                                if (classFile != null)
                                {
                                    classFile.EntityName = zipService.GetEntityName(file.Value, type);
                                }
                                data.ClassFileDefinition = classFile;
                            }
                            else if (type == BackFileType.Config || type == BackFileType.Dependency || type == BackFileType.Rights)
                            {
                                string filePath = Path.Combine(workingDirectoryPath, file.Key);
                                data.ExtractBlocks = zipService.AnalyzePartialFile(filePath);
                            }

                            filesContent.FeatureDataList.Add(data);
                        }
                        vm.ZipFilesContent.Add(filesContent);
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
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName,
                        vm.CurrentProject.CompanyName, vm.CurrentProject.Name, Constants.FolderAngular, type);
                    if (string.IsNullOrWhiteSpace(workingDirectoryPath))
                    {
                        consoleWriter.AddMessageLine($"Zip archive '{fileName}' not found.", "Orange");
                        return;
                    }

                    if (type == FeatureType.CRUD)
                    {
                        ClassDefinition cd = GetDtoClassDefinition();
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

                        List<string> options = null;
                        KeyValuePair<string, string> textFile = fileList.Where(x => x.Value.Equals(crudSettings.OptionsToRemoveFileName)).FirstOrDefault();
                        if (textFile.Key != null && textFile.Value != null)
                        {
                            BIAToolKitJson btkj = DeserializeJsonFile(Path.Combine(workingDirectoryPath, textFile.Key));
                            if (btkj != null && btkj.OptionsToRemove != null)
                            {
                                options = btkj.OptionsToRemove.ToList();
                            }
                            fileList.Remove(textFile.Key);
                        }

                        if (fileList.Count > 0)
                        {
                            ZipFilesContent crudFilesContent = new(type);
                            foreach (KeyValuePair<string, string> file in fileList)
                            {
                                string filePath = Path.Combine(workingDirectoryPath, file.Key);
                                AngularCRUDData data = new(file.Value, file.Key, workingDirectoryPath)
                                {
                                    ExtractBlocks = zipService.AnalyzeAngularFile(filePath, planeDtoProperties)
                                };
                                if (options != null && options.Count > 0)
                                {
                                    data.OptionToDelete = zipService.ExtractLinesContainsOptions(filePath, options);
                                }
                                crudFilesContent.FeatureDataList.Add(data);
                            }

                            vm.ZipFilesContent.Add(crudFilesContent);
                        }
                    }
                    else if (type == FeatureType.Option)
                    {
                        // Analyze angular files
                        if (fileList.Count > 0)
                        {
                            ZipFilesContent filesContent = new(type);
                            foreach (KeyValuePair<string, string> file in fileList)
                            {
                                AngularCRUDData data = new(file.Value, file.Key, workingDirectoryPath)
                                {
                                    ExtractBlocks = zipService.AnalyzeAngularFile(Path.Combine(workingDirectoryPath, file.Key), null)
                                };
                                filesContent.FeatureDataList.Add(data);
                            }

                            vm.ZipFilesContent.Add(filesContent);
                        }
                    }
                    else
                    {
                        throw new Exception("Parsing Team feature not implemented !");
                    }
                }
                //else
                //{
                //    consoleWriter.AddMessageLine($"Zip archive '{fileName}' is empty.", "Orange");
                //}
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

        private ClassDefinition GetDtoClassDefinition()
        {
            ZipFilesContent content = vm.ZipFilesContent.Where(x => x.Type == FeatureType.Back).FirstOrDefault();
            if (content != null)
            {
                foreach (DotNetCRUDData angularFile in content.FeatureDataList)
                {
                    if (angularFile.FileType == BackFileType.Dto)
                    {
                        return angularFile.ClassFileDefinition;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Deserialize Json file to object.
        /// </summary>
        /// <param name="fileName"></param>
        private BIAToolKitJson DeserializeJsonFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                consoleWriter.AddMessageLine($"Error on analysing json file: file not exist on disk: '{fileName}'", "Orange");
                return null;
            }

            using StreamReader r = new(fileName);
            string json = r.ReadToEnd();
            return JsonConvert.DeserializeObject<BIAToolKitJson>(json);
        }

        private string GetEntityNameFromDto(string dtoFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];   // name without 'dto' suffix
            }

            return fileName;
        }
        #endregion
    }

    class BIAToolKitJson
    {
        public IList<string> OptionsToRemove { get; set; }
    }


}
