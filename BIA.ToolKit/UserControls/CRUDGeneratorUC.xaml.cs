namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
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
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private IConsoleWriter consoleWriter;
        private CSharpParserService service;
        private ZipParserService zipService;
        private GenerateCrudService crudService;
        private CRUDSettings settings;

        private readonly CRUDGeneratorViewModel vm;
        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;
        private List<CrudGenerationSettings> crudSettingsList;
        private CrudGenerationSettings webApiSettings;
        private CrudGenerationSettings crudSettings;
        private CrudGenerationSettings optionSettings;
        private CrudGenerationSettings teamSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
            crudSettingsList = new();
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
        }

        /// <summary>
        /// Init data based on current page (from settings).
        /// </summary>
        private void CurrentProjectChange()
        {
            ClearAll();

            // Set combobobox and checkbox enabled
            vm.IsProjectChosen = true;

            // List Dto files from Dto folder
            vm.DtoFiles = ListDtoFiles();

            string dotnetBiaFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet, Constants.FolderBia);
            string angularBiaFolderPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, vm.CurrentProject.BIAFronts, Constants.FolderBia);

            // Load generation settings
            string backSettingsFileName = Path.Combine(dotnetBiaFolderPath, settings.GenerationSettingsFileName);
            string frontSettingsFileName = Path.Combine(angularBiaFolderPath, settings.GenerationSettingsFileName);
            if (File.Exists(backSettingsFileName))
            {
                crudSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<CrudGenerationSettings>>(backSettingsFileName));
            }
            if (File.Exists(frontSettingsFileName))
            {
                crudSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<CrudGenerationSettings>>(frontSettingsFileName));
            }
            this.webApiSettings = this.crudSettingsList.FirstOrDefault(x => x.Type == FeatureType.WebApi.ToString());
            this.crudSettings = this.crudSettingsList.FirstOrDefault(x => x.Type == FeatureType.CRUD.ToString());
            this.optionSettings = this.crudSettingsList.FirstOrDefault(x => x.Type == FeatureType.Option.ToString());
            this.teamSettings = this.crudSettingsList.FirstOrDefault(x => x.Type == FeatureType.Team.ToString());

            // Associate zip files to features
            vm.ZipFeatureTypeList.Add(new ZipFeatureType(FeatureType.WebApi, this.webApiSettings?.ZipName, dotnetBiaFolderPath));
            vm.ZipFeatureTypeList.Add(new ZipFeatureType(FeatureType.CRUD, this.crudSettings?.ZipName, angularBiaFolderPath));
            vm.ZipFeatureTypeList.Add(new ZipFeatureType(FeatureType.Option, this.optionSettings?.ZipName, angularBiaFolderPath));
            vm.ZipFeatureTypeList.Add(new ZipFeatureType(FeatureType.Team, this.teamSettings?.ZipName, angularBiaFolderPath));

            // Load generation history
            this.crudHistoryFileName = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, settings.GenerationHistoryFileName);
            this.crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
        }

        #region State change
        private void ClearAll()
        {
            // Clean all lists (in case of current project change)
            this.crudSettingsList.Clear();
            vm.ZipFeatureTypeList.Clear();
            vm.ZipDotNetCollection.Clear();
            vm.ZipAngularCollection.Clear();

            this.webApiSettings = null;
            this.crudSettings = null;
            this.optionSettings = null;
            this.teamSettings = null;

            vm.DtoFiles = null;
            vm.IsWebApiSelected = false;
            vm.IsCrudSelected = false;
            vm.IsOptionSelected = false;
            vm.IsTeamSelected = false;

            this.crudHistory = null;
        }

        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;
            vm.IsDtoParsed = false;
            vm.DtoDisplayItems = null;
            vm.OptionItems = null;
            vm.CRUDNameSingular = GetEntityNameFromDto(vm.DtoSelected);

            bool isBackSelected = false, isCrudSelected = false, isOptionSelected = false, isTeamSelected = false;
            Visibility msgVisibility = Visibility.Hidden;
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

                        msgVisibility = Visibility.Visible;
                        isBackSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.WebApi.ToString()) != null);
                        isCrudSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.CRUD.ToString()) != null);
                        isOptionSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Option.ToString()) != null);
                        isTeamSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Team.ToString()) != null);

                        // Get generated options
                        List<CRUDGenerationHistory> histories = crudHistory.CRUDGenerationHistory.Where(h => (h.Mapping.Dto != dtoName) &&
                            (h.Generation.Any(g => g.Feature == FeatureType.Option.ToString()))).ToList();
                        if (histories.Any())
                        {
                            List<string> options = new();
                            histories.ForEach(h => options.Add(h.EntityNameSingular));
                            vm.OptionItems = options;
                        }
                    }
                }
            }

            CrudAlreadyGeneratedLabel.Visibility = msgVisibility;
            vm.IsWebApiSelected = isBackSelected;
            vm.IsCrudSelected = isCrudSelected;
            vm.IsOptionSelected = isOptionSelected;
            vm.IsTeamSelected = isTeamSelected;
        }

        /// <summary>
        /// Action linked with "Entity name (singular)" textbox.
        /// </summary>
        private void ModifyEntitySingularText_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.CRUDNamePlural = string.Empty;
        }

        /// <summary>
        /// Action linked with "Entity name (plural)" textbox.
        /// </summary>
        private void ModifyEntityPluralText_TextChanged(object sender, TextChangedEventArgs e)
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
            vm.DtoFiles = ListDtoFiles();
        }

        /// <summary>
        /// Action linked with "Parse Dto" button.
        /// </summary>
        private void ParseDto_Click(object sender, RoutedEventArgs e)
        {
            vm.IsDtoParsed = ParseDtoFile();
        }

        /// <summary>
        /// Action linked with "Parse Zip" button.
        /// </summary>
        private void ParseZip_Click(object sender, RoutedEventArgs e)
        {
            vm.IsZipParsed = false;

            // Parse DotNet Zip files
            if (vm.ZipDotNetCollection.Count > 0 && vm.IsWebApiSelected)
            {
                // Parse WebApi Zip file
                vm.IsZipParsed |= ParseZipFile(FeatureType.WebApi);
            }

            // Parse Angular Zip files
            if (vm.ZipAngularCollection.Count > 0 && vm.IsCrudSelected)
            {
                // Parse CRUD Zip file
                vm.IsZipParsed |= ParseZipFile(FeatureType.CRUD);
            }

            if (vm.ZipAngularCollection.Count > 0 && vm.IsOptionSelected)
            {
                // Parse Option Zip file
                vm.IsZipParsed |= ParseZipFile(FeatureType.Option);
            }

            if (vm.ZipAngularCollection.Count > 0 && vm.IsTeamSelected)
            {
                // Parse Team Zip file
                vm.IsZipParsed |= ParseZipFile(FeatureType.Team);
            }
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlural,
                this.crudSettings?.FeatureName, this.crudSettings?.FeatureNamePlural,
                this.optionSettings?.FeatureName, this.optionSettings?.FeatureNamePlural,
                this.teamSettings?.FeatureName, this.teamSettings?.FeatureNamePlural);

            // Generation DotNet + Angular files
            string path = crudService.GenerateCrudFiles(vm.CurrentProject, vm.DtoEntity, vm.ZipFeatureTypeList, vm.DtoDisplayItemSelected, vm.OptionItemSelected, this.settings.GenerateInProjectFolder);

            // Generate generation history file
            UpdateCrudGenerationHistory();

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        #endregion

        #region Private method
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

                    // Create "Mapping" part
                    Mapping = new()
                    {
                        Dto = GetDtoSelectedPath(),
                        Template = this.webApiSettings?.ZipName,
                        Type = DOTNET_TYPE,
                    }
                };

                // Create "Generation" list part
                vm.ZipFeatureTypeList.ForEach(feature =>
                {
                    if (feature.IsChecked)
                    {
                        Generation crudGeneration = new()
                        {
                            Template = feature.ZipName,
                            Feature = feature.FeatureType.ToString()
                        };
                        if (feature.FeatureType == FeatureType.WebApi)
                        {
                            crudGeneration.Type = DOTNET_TYPE;
                            crudGeneration.Folder = Constants.FolderDotNet;
                        }
                        else
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
                CommonTools.SerializeToJsonFile<CRUDGeneration>(this.crudHistory, crudHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
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
                List<string> files = Directory.EnumerateFiles(path, "*Dto.cs", SearchOption.AllDirectories).ToList();
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
        private bool ParseDtoFile()
        {
            try
            {
                // Check selected Dto file
                string fileName = vm.DtoFiles[vm.DtoSelected];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    consoleWriter.AddMessageLine($"Dto file '{fileName}' not found to parse.", "Orange");
                    return false;
                }

                // Parse Dto entity file
                vm.DtoEntity = service.ParseEntity(fileName, settings.DtoCustomAttributeName);
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
        /// Parse Zip files (WebApi, CRUD, option or team).
        /// </summary>
        private bool ParseZipFile(FeatureType type)
        {
            try
            {
                List<string> zipSelectedList = (type == FeatureType.WebApi) ? vm.ZipDotNetCollection.ToList() : vm.ZipAngularCollection.ToList();

                ZipFeatureType zipData = GetFeatureTypeData(type, zipSelectedList);
                if (zipData != null)
                {
                    string folderName = (zipData.FeatureType == FeatureType.WebApi) ? Constants.FolderDotNet : vm.CurrentProject.BIAFronts;
                    //List<string> options = crudSettingsList.FirstOrDefault(x => x.Type == type.ToString())?.Options;
                    return zipService.ParseZipFile(zipData, folderName, settings.DtoCustomAttributeName);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing '{type}' Zip File: {ex.Message}", "Red");
            }
            return false;
        }

        private ZipFeatureType GetFeatureTypeData(FeatureType type, List<string> zipSelected)
        {
            if (zipSelected == null || zipSelected.Count <= 0) { return null; }

            bool found = false;
            ZipFeatureType crudZipData = vm.ZipFeatureTypeList.Where(x => x.FeatureType == type && x.IsChecked).FirstOrDefault();
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
            if (string.IsNullOrWhiteSpace(vm.DtoSelected))
                return null;

            string dotNetPath = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, Constants.FolderDotNet);
            return vm.DtoFiles[vm.DtoSelected].Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }
        #endregion
    }
}
