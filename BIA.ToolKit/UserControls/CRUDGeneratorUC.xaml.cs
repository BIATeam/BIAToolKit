namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for DtoGenerator.xaml
    /// </summary>
    public partial class CRUDGeneratorUC : UserControl
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";
        private const int FRAMEWORK_VERSION_MINIMUM = 390;

        private IConsoleWriter consoleWriter;
        private CSharpParserService service;
        private ZipParserService zipService;
        private GenerateCrudService crudService;
        private CRUDSettings settings;

        private readonly CRUDGeneratorViewModel vm;
        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;
        private List<CrudGenerationSettings> backSettingsList;
        private List<CrudGenerationSettings> frontSettingsList;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
            backSettingsList = new();
            frontSettingsList = new();
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
            this.settings = new(settingsService);
        }

        /// <summary>
        /// Update CurrentProject value.
        /// </summary>
        public void SetCurrentProject(Project currentProject)
        {
            vm.CurrentProject = currentProject;
            CurrentProjectChange();
            crudService.CurrentProject = currentProject;
        }

        #region State change
        /// <summary>
        /// Init data based on current page (from settings).
        /// </summary>
        private void CurrentProjectChange()
        {
            ClearAll();

            // Set form enabled
            vm.IsProjectChosen = true;

            // Load BIA settings + history + parse zips
            InitProject();

            // List Dto files from Dto folder
            ListDtoFiles();
        }

        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChange(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            vm.IsDtoParsed = false;
            vm.DtoDisplayItems = null;
            bool isBackSelected = true, isFrontSelected = true;
            Visibility msgVisibility = Visibility.Hidden;

            vm.CRUDNameSingular = GetEntityNameFromDto(vm.DtoSelected);
            ParseDomains();

            if (this.crudHistory != null)
            {
                string dtoName = GetDtoSelectedPath();
                if (!string.IsNullOrEmpty(dtoName))
                {
                    CRUDGenerationHistory history = crudHistory.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping.Dto == dtoName);

                    if (history != null)
                    {
                        // Apply last generation values
                        vm.CRUDNameSingular = history.EntityNameSingular;
                        vm.CRUDNamePlural = history.EntityNamePlural;
                        vm.FeatureSelected = history.Feature;
                        history.OptionItems?.ForEach(o =>
                        {
                            OptionItem item = vm.OptionItems.FirstOrDefault(x => x.OptionName == o);
                            if (item != null) item.Check = true;
                        });

                        isBackSelected = history.Generation.Any(g => g.GenerationType == GenerationType.WebApi.ToString());
                        isFrontSelected = history.Generation.Any(g => g.GenerationType == GenerationType.Front.ToString());
                        msgVisibility = Visibility.Visible;
                    }
                }

                // Get generated options
                List<CRUDGenerationHistory> histories = crudHistory.CRUDGenerationHistory.Where(h =>
                    (h.Mapping.Dto != dtoName) &&
                    h.Generation.Any(g => g.FeatureType == FeatureType.Option.ToString())).ToList();
            }

            CrudAlreadyGeneratedLabel.Visibility = msgVisibility;
            vm.IsDtoGenerated = CrudAlreadyGeneratedLabel.Visibility == Visibility.Visible;
            vm.IsWebApiSelected = isBackSelected;
            vm.IsFrontSelected = isFrontSelected;

            vm.IsDtoParsed = ParseDtoFile();
        }

        /// <summary>
        /// Action linked with "Entity name (singular)" textbox.
        /// </summary>
        private void ModifyEntitySingular_TextChange(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlural = string.Empty;
        }

        /// <summary>
        /// Action linked with "Entity name (plural)" textbox.
        /// </summary>
        private void ModifyEntityPlural_TextChange(object sender, TextChangedEventArgs e)
        {
            vm.IsSelectionChange = true;
        }
        #endregion

        #region Button Action
        /// <summary>
        /// Action linked with "Refresh Dto List" button.
        /// </summary>
        private void RefreshDtoList_Click(object sender, RoutedEventArgs e)
        {
            // List Dto files from Dto folder
            ListDtoFiles();
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            crudService.CrudNames.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlural, vm.FeatureSelected, vm.IsWebApiSelected, vm.IsFrontSelected);

            // Generation DotNet + Angular files
            List<string> optionsItems = vm.OptionItems.Any() ? vm.OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
            vm.IsDtoGenerated = crudService.GenerateFiles(vm.DtoEntity, vm.ZipFeatureTypeList, vm.DtoDisplayItemSelected, optionsItems);

            // Generate generation history file
            UpdateCrudGenerationHistory();

            consoleWriter.AddMessageLine($"End of '{vm.CRUDNameSingular}' generation.", "Blue");
        }

        /// <summary>
        /// Action linked with "Delete last generation" button.
        /// </summary>
        private void DeleteLastGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get last generation history
                CRUDGenerationHistory history = crudHistory?.CRUDGenerationHistory?.FirstOrDefault(h => h.Mapping.Dto == GetDtoSelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{vm.CRUDNameSingular}' generation found.", "Orange");
                    return;
                }

                // Get generation histories used by options
                List<CRUDGenerationHistory> historyOptions = crudHistory?.CRUDGenerationHistory?.Where(h => h.OptionItems.Contains(vm.CRUDNameSingular)).ToList();

                // Delete last generation
                crudService.DeleteLastGeneration(vm.ZipFeatureTypeList, vm.CurrentProject, history, historyOptions, vm.FeatureSelected);

                // Update history
                DeleteLastGenerationHistory(history);
                CrudAlreadyGeneratedLabel.Visibility = Visibility.Hidden;

                consoleWriter.AddMessageLine($"End of '{vm.CRUDNameSingular}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{vm.CRUDNameSingular}' generation: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Action linked with "Delete Annotations" button.
        /// </summary>
        private void DeleteBIAToolkitAnnotations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder message = new();
                message.AppendLine("Do you want to permanently remove all BIAToolkit annotations in code?");
                message.AppendLine("After that you will no longer be able to regenerate old CRUDs.");
                message.AppendLine();
                message.AppendLine("Be careful, this action is irreversible.");
                MessageBoxResult result = MessageBox.Show(message.ToString(), "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.OK)
                {
                    List<string> folders = new() {
                        Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.BIAFronts, "src",  "app")
                    };

                    crudService.DeleteBIAToolkitAnnotations(folders);
                }

                consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on cleaning annotations for project '{vm.CurrentProject.Name}': {ex.Message}", "Red");
            }
        }
        #endregion

        #region Private method
        private void ClearAll()
        {
            // Clean all lists (in case of current project change)
            this.backSettingsList.Clear();
            this.frontSettingsList.Clear();
            vm.OptionItems?.Clear();
            vm.ZipFeatureTypeList.Clear();
            vm.Features.Clear();

            vm.DtoFiles = null;
            vm.IsWebApiSelected = true;
            vm.IsFrontSelected = true;
            vm.FeatureSelected = null;

            this.crudHistory = null;
        }

        private void InitProject()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                InitFormProject();
                ParseZips();
                crudService.CrudNames = new(backSettingsList, frontSettingsList);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on intializing project: {ex.Message}", "Red");
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private void InitFormProject()
        {
            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string angularBiaFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.BIAFronts, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(dotnetBiaFolderPath, settings.GenerationSettingsFileName);
            string frontSettingsFileName = Path.Combine(angularBiaFolderPath, settings.GenerationSettingsFileName);
            this.crudHistoryFileName = Path.Combine(vm.CurrentProject.Folder, settings.GenerationHistoryFileName);


            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<CrudGenerationSettings>>(backSettingsFileName));
            }
            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<CrudGenerationSettings>>(frontSettingsFileName));
            }

            foreach(var setting in backSettingsList)
            {
                var featureType = (FeatureType)Enum.Parse(typeof(FeatureType), setting.Type);
                var zipFeatureType = new ZipFeatureType(featureType, GenerationType.WebApi, setting.ZipName, dotnetBiaFolderPath, setting.Feature);
                vm.ZipFeatureTypeList.Add(zipFeatureType);
            }

            foreach (var setting in frontSettingsList)
            {
                var featureType = (FeatureType)Enum.Parse(typeof(FeatureType), setting.Type);
                var zipFeatureType = new ZipFeatureType(featureType, GenerationType.Front, setting.ZipName, angularBiaFolderPath, setting.Feature);
                vm.ZipFeatureTypeList.Add(zipFeatureType);
            }

            foreach (var feature in vm.ZipFeatureTypeList.Select(x => x.Feature).Distinct())
            {
                vm.Features.Add(feature);
            }

            // Load generation history
            this.crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(this.crudHistoryFileName);
        }

        /// <summary>
        /// Update CRUD generation history file.
        /// </summary>
        private void UpdateCrudGenerationHistory()
        {
            try
            {
                this.crudHistory ??= new();

                CRUDGenerationHistory history = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = vm.CRUDNameSingular,
                    EntityNamePlural = vm.CRUDNamePlural,
                    DisplayItem = vm.DtoDisplayItemSelected,
                    OptionItems = vm.OptionItems?.Where(o => o.Check).Select(o => o.OptionName).ToList(),
                    Feature = vm.FeatureSelected,

                    // Create "Mapping" part
                    Mapping = new()
                    {
                        Dto = GetDtoSelectedPath(),
                        Type = DOTNET_TYPE,
                    }
                };

                // Create "Generation" list part
                vm.ZipFeatureTypeList.Where(f => f.FeatureDataList != null).ToList().ForEach(feature =>
                {
                    if (feature.IsChecked)
                    {
                        Generation crudGeneration = new()
                        {
                            GenerationType = feature.GenerationType.ToString(),
                            FeatureType = feature.FeatureType.ToString(),
                            Template = feature.ZipName
                        };
                        if (feature.GenerationType == GenerationType.WebApi)
                        {
                            crudGeneration.Type = DOTNET_TYPE;
                            crudGeneration.Folder = Constants.FolderDotNet;
                        }
                        else if (feature.GenerationType == GenerationType.Front)
                        {
                            crudGeneration.Type = ANGULAR_TYPE;
                            crudGeneration.Folder = vm.CurrentProject.BIAFronts;
                        }
                        history.Generation.Add(crudGeneration);
                    }
                });

                // Get existing to verify if previous generation for same entity name was already done
                CRUDGenerationHistory genFound = this.crudHistory.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    // Remove last generation to replace by new generation
                    this.crudHistory.CRUDGenerationHistory.Remove(genFound);
                }

                this.crudHistory.CRUDGenerationHistory.Add(history);

                // Generate history file
                CommonTools.SerializeToJsonFile<CRUDGeneration>(this.crudHistory, this.crudHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Delete last CRUD generation on history file.
        /// </summary>
        private void DeleteLastGenerationHistory(CRUDGenerationHistory history)
        {
            // Delete CRUDGenerationHistory
            crudHistory?.CRUDGenerationHistory?.Remove(history);

            // Delete in options
            foreach (CRUDGenerationHistory optionHistory in crudHistory?.CRUDGenerationHistory)
            {
                optionHistory.OptionItems.Remove(history.EntityNameSingular);
            }

            // Generate history file
            CommonTools.SerializeToJsonFile<CRUDGeneration>(this.crudHistory, this.crudHistoryFileName);
        }

        /// <summary>
        /// List all Dto files from current project.
        /// </summary>
        private void ListDtoFiles()
        {
            Dictionary<string, string> dtoFiles = new();

            string dtoFolder = $"{vm.CurrentProject.CompanyName}.{vm.CurrentProject.Name}.Domain.Dto";
            string path = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet, dtoFolder);

            try
            {
                if (Directory.Exists(path))
                {
                    // List files
                    List<string> files = Directory.EnumerateFiles(path, "*Dto.cs", SearchOption.AllDirectories).ToList();
                    // Build dictionnary: key = file Name, Value = full path
                    files.ForEach(x => dtoFiles.Add(new FileInfo(x).Name, new FileInfo(x).FullName));
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            vm.DtoFiles = dtoFiles;
        }

        /// <summary>
        /// Parse all zips.
        /// </summary>
        private void ParseZips()
        {
            vm.IsZipParsed = false;

            // Verify version to avoid to try to parse zip when ".bia" folders are missing
            if (!string.IsNullOrEmpty(vm.CurrentProject.FrameworkVersion))
            {
                string version = vm.CurrentProject.FrameworkVersion.Replace(".", "");
                if (int.TryParse(version, out int value))
                {
                    if (value < FRAMEWORK_VERSION_MINIMUM)
                        return;
                }
            }

            bool parsed = false;
            // *** Parse DotNet Zip files ***
            // Parse CRUD Zip file
            parsed |= ParseZipFile(FeatureType.CRUD, GenerationType.WebApi);
            // Parse Option Zip file
            parsed |= ParseZipFile(FeatureType.Option, GenerationType.WebApi);
            // Parse Team Zip file
            parsed |= ParseZipFile(FeatureType.Team, GenerationType.WebApi);

            // *** Parse Angular Zip files ***
            // Parse CRUD Zip file
            parsed |= ParseZipFile(FeatureType.CRUD, GenerationType.Front);
            // Parse Option Zip file
            parsed |= ParseZipFile(FeatureType.Option, GenerationType.Front);
            // Parse Team Zip file
            parsed |= ParseZipFile(FeatureType.Team, GenerationType.Front);

            vm.IsZipParsed = parsed;
        }

        /// <summary>
        /// Parse the Dto file.
        /// </summary>
        private bool ParseDtoFile()
        {
            try
            {
                if (string.IsNullOrEmpty(vm.DtoSelected))
                    return false;

                // Check selected Dto file
                string fileName = vm.DtoFiles[vm.DtoSelected];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    consoleWriter.AddMessageLine($"Dto file '{fileName}' not found to parse.", "Orange");
                    return false;
                }

                // Parse Dto entity file
                vm.DtoEntity = service.ParseEntity(fileName, settings.DtoCustomAttributeFieldName, settings.DtoCustomAttributeClassName);
                if (vm.DtoEntity == null || vm.DtoEntity.Properties == null || vm.DtoEntity.Properties.Count <= 0)
                {
                    consoleWriter.AddMessageLine("No properties found on Dto file.", "Orange");
                    return false;
                }

                // Fill display item list
                List<string> displayItems = new();
                vm.DtoEntity.Properties.ForEach(p => displayItems.Add(p.Name));
                vm.DtoDisplayItems = displayItems;

                // Set by default previous generation selected value
                CRUDGenerationHistory history = this.crudHistory?.CRUDGenerationHistory?.FirstOrDefault(gh => (vm.DtoSelected == Path.GetFileName(gh.Mapping.Dto)));
                vm.DtoDisplayItemSelected = history?.DisplayItem;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Dto File: {ex.Message}", "Red");
            }
            return false;
        }

        /// <summary>
        /// Parse domain folders.
        /// </summary>
        private void ParseDomains()
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            List<string> foldersName = new();

            // List Options folders
            string folderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.BIAFronts, domainsPath);
            List<string> folders = Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories).ToList();
            folders.ForEach(f => foldersName.Add(new DirectoryInfo(f).Name.Replace(suffix, "")));

            // Get Options name
            vm.OptionItems?.Clear();
            foldersName.ForEach(f => vm.OptionItems.Add(new OptionItem(CommonTools.ConvertKebabToPascalCase(f))));
        }

        /// <summary>
        /// Parse Zip files (WebApi, CRUD, option or team).
        /// </summary>
        private bool ParseZipFile(FeatureType featureType, GenerationType generationType)
        {
            try
            {
                string folderName = (generationType == GenerationType.WebApi) ? Constants.FolderDotNet : vm.CurrentProject.BIAFronts;
                string biaFolder = Path.Combine(vm.CurrentProject.Folder, folderName, Constants.FolderBia);
                if (!new DirectoryInfo(biaFolder).Exists)
                {
                    return false;
                }

                ZipFeatureType zipData = vm.ZipFeatureTypeList.Where(x => x.GenerationType == generationType && x.FeatureType == featureType).FirstOrDefault();
                if (zipData != null)
                {
                    return zipService.ParseZipFile(zipData, biaFolder, settings.DtoCustomAttributeFieldName);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing '{featureType}' Zip File: {ex.Message}", "Red");
            }
            return false;
        }

        /// <summary>
        /// Extract the entity name form Dto file name.
        /// </summary>
        private string GetEntityNameFromDto(string dtoFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];   // name without 'dto' suffix
            }

            return fileName;
        }

        /// <summary>
        /// Get the Dto path from select dto file.
        /// </summary>
        private string GetDtoSelectedPath()
        {
            if (string.IsNullOrWhiteSpace(vm.DtoSelected))
                return null;

            string dotNetPath = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet);
            return vm.DtoFiles[vm.DtoSelected].Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }
        #endregion
    }
}
