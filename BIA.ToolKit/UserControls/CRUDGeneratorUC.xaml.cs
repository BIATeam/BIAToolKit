namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
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
        private IConsoleWriter consoleWriter;
        private CSharpParserService service;
        private ZipParserService zipService;
        private GenerateCrudService crudService;
        private CRUDSettings crudSettings;
        private CRUDGeneration crudGeneration;
        private readonly CRUDGeneratorViewModel vm;
        private string crudHistoryFileName;

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
            string angularFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, vm.CurrentProject.BIAFronts, Constants.FolderDoc);
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Back, this.crudSettings.ZipNameBack, dotnetFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.CRUD, this.crudSettings.ZipNameFeature, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Option, this.crudSettings.ZipNameOption, angularFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Team, this.crudSettings.ZipNameTeam, angularFolderPath));

            // Load generation history
            crudHistoryFileName = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, crudSettings.GenerationHistoryFileName);
            this.crudGeneration = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
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

            bool isBackSelected = false, isCrudSelected = false, isOptionSelected = false, isTeamSelected = false;
            Visibility visible = Visibility.Hidden;
            if (this.crudGeneration != null)
            {
                string dtoName = GetDtoSelectedPath();
                CRUDGenerationHistory history = crudGeneration.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping.Dto == dtoName);

                // Apply last generation values
                if (history != null)
                {
                    vm.CRUDNameSingular = history.EntityNameSingular;
                    vm.CRUDNamePlurial = history.EntityNamePlurial;

                    visible = Visibility.Visible;
                    isBackSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Back.ToString()) != null);
                    isCrudSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.CRUD.ToString()) != null);
                    isOptionSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Option.ToString()) != null);
                    isTeamSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Team.ToString()) != null);
                }
            }

            CrudAlreadyGeneratedLabel.Visibility = visible;
            vm.IsBackSelected = isBackSelected;
            vm.IsCrudSelected = isCrudSelected;
            vm.IsOptionSelected = isOptionSelected;
            vm.IsTeamSelected = isTeamSelected;
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

            // Generate generation history file
            UpdateCrudGenerationHistory();

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        #endregion

        #region Private method
        private void UpdateCrudGenerationHistory()
        {
            if (this.crudGeneration == null)
                this.crudGeneration = new();

            CRUDGenerationHistory history = new()
            {
                Date = DateTime.Now,
                EntityNameSingular = vm.CRUDNameSingular,
                EntityNamePlurial = vm.CRUDNamePlurial,

                // Create "Mapping" part
                Mapping = new()
                {
                    Dto = GetDtoSelectedPath(),
                    Template = crudSettings.ZipNameBack,
                    Type = "DotNet",
                }
            };

            // Create "Generation" list part
            vm.FeatureTypeDataList.ForEach(feature =>
            {
                if (feature.IsChecked)
                {
                    Generation crudGeneration = new()
                    {
                        Template = feature.ZipName,
                        Feature = feature.Type.ToString()
                    };
                    if (feature.Type == FeatureType.Back)
                    {
                        crudGeneration.Type = "DotNet";
                        crudGeneration.Folder = Constants.FolderDotNet;
                    }
                    else
                    {
                        crudGeneration.Type = "Angular";
                        crudGeneration.Folder = vm.CurrentProject.BIAFronts;
                    }
                    history.Generation.Add(crudGeneration);
                }
            });

            // Get existing to verify if previous generation for same entity name was already done
            CRUDGenerationHistory genFound = this.crudGeneration.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
            if (genFound != null)
            {
                // Remove last generation to replace by new generation
                this.crudGeneration.CRUDGenerationHistory.Remove(genFound);
            }

            this.crudGeneration.CRUDGenerationHistory.Add(history);

            // Generate history file
            CommonTools.SerializeToJsonFile<CRUDGeneration>(this.crudGeneration, crudHistoryFileName);
        }

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
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, Constants.FolderDotNet, FeatureType.Back);
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
                            if (type != null && (type == BackFileType.Mapper || type == BackFileType.Entity))
                            {
                                continue;
                            }

                            DotNetCRUDData data = new(file.Value, file.Key, workingDirectoryPath)
                            {
                                FileType = type
                            };

                            if (data.IsPartialFile)
                            {
                                string filePath = Path.Combine(workingDirectoryPath, file.Key);
                                data.ExtractBlocks = zipService.AnalyzePartialFile(filePath);
                            }
                            else if (type == BackFileType.Dto)
                            {
                                ClassDefinition classFile = service.ParseClassFile(Path.Combine(workingDirectoryPath, file.Key));
                                if (classFile != null)
                                {
                                    classFile.EntityName = zipService.GetEntityName(file.Value, type);
                                }
                                data.ClassFileDefinition = classFile;
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
                    (string workingDirectoryPath, Dictionary<string, string> fileList) = zipService.ReadZipAndExtract(fileName, vm.CurrentProject.BIAFronts, type);
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
                            BIAToolKitJson btkj = CommonTools.DeserializeJsonFile<BIAToolKitJson>(Path.Combine(workingDirectoryPath, textFile.Key));
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
                                if (data.IsPartialFile)
                                {
                                    data.ExtractBlocks = zipService.AnalyzePartialFile(filePath);
                                }
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

        private string GetEntityNameFromDto(string dtoFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];   // name without 'dto' suffix
            }

            return fileName;
        }

        private string GetDtoSelectedPath()
        {
            string dotNetPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet);
            return vm.DtoFiles[vm.DtoSelected].Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }
        #endregion
    }

    class BIAToolKitJson
    {
        public string Feature { get; set; }
        public List<string> OptionsToRemove { get; set; }
    }



}
