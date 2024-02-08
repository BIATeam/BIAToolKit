﻿namespace BIA.ToolKit.UserControls
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
            // Set combobobox and checkbox enabled
            vm.IsProjectChosen = true;

            // Clean all lists (in case of current project change)
            this.crudSettingsList.Clear();
            vm.FeatureTypeDataList.Clear();
            vm.ZipDotNetSelected.Clear();
            vm.ZipAngularSelected.Clear();
            vm.ZipFilesContent.Clear();

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
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.WebApi, this.webApiSettings?.ZipName, dotnetBiaFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.CRUD, this.crudSettings?.ZipName, angularBiaFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Option, this.optionSettings?.ZipName, angularBiaFolderPath));
            vm.FeatureTypeDataList.Add(new FeatureTypeData(FeatureType.Team, this.teamSettings?.ZipName, angularBiaFolderPath));

            // Load generation history
            this.crudHistoryFileName = Path.Combine(vm.CurrentProject.Folder, vm.CurrentProject.Name, settings.GenerationHistoryFileName);
            this.crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
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
            if (this.crudHistory != null)
            {
                string dtoName = GetDtoSelectedPath();
                CRUDGenerationHistory history = crudHistory.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping.Dto == dtoName);

                // Apply last generation values
                if (history != null)
                {
                    vm.CRUDNameSingular = history.EntityNameSingular;
                    vm.CRUDNamePlurial = history.EntityNamePlurial;

                    visible = Visibility.Visible;
                    isBackSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.WebApi.ToString()) != null);
                    isCrudSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.CRUD.ToString()) != null);
                    isOptionSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Option.ToString()) != null);
                    isTeamSelected = (history.Generation.FirstOrDefault(g => g.Feature == FeatureType.Team.ToString()) != null);
                }
            }

            CrudAlreadyGeneratedLabel.Visibility = visible;
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
            if (vm.ZipDotNetSelected.Count > 0 && vm.IsWebApiSelected)
            {
                // Parse WebApi Zip file
                ParseZipFile(FeatureType.WebApi);
            }

            // Parse Angular Zip files
            if (vm.ZipAngularSelected.Count > 0 && vm.IsCrudSelected)
            {
                // Parse CRUD Zip file
                ParseZipFile(FeatureType.CRUD);
            }

            if (vm.ZipAngularSelected.Count > 0 && vm.IsOptionSelected)
            {
                // Parse Option Zip file
                ParseZipFile(FeatureType.Option);
            }

            if (vm.ZipAngularSelected.Count > 0 && vm.IsTeamSelected)
            {
                // Parse Team Zip file
                ParseZipFile(FeatureType.Team);
            }

            vm.IsZipParsed = true;
        }

        /// <summary>
        /// Action linked with "Generate CRUD" button.
        /// </summary>
        private void GenerateCrud_Click(object sender, RoutedEventArgs e)
        {
            crudService.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlurial,
                this.crudSettings?.FeatureName, this.crudSettings?.FeatureNamePlurial,
                this.optionSettings?.FeatureName, this.optionSettings?.FeatureNamePlurial,
                this.teamSettings?.FeatureName, this.teamSettings?.FeatureNamePlurial);

            // Generation DotNet + Angular files
            string path = crudService.GenerateCrudFiles(vm.CurrentProject, vm.DtoEntity, vm.ZipFilesContent, this.settings.GenerateInProjectFolder);

            // Generate generation history file
            UpdateCrudGenerationHistory();

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        #endregion

        #region Private method
        private void UpdateCrudGenerationHistory()
        {
            this.crudHistory ??= new();

            CRUDGenerationHistory history = new()
            {
                Date = DateTime.Now,
                EntityNameSingular = vm.CRUDNameSingular,
                EntityNamePlurial = vm.CRUDNamePlurial,

                // Create "Mapping" part
                Mapping = new()
                {
                    Dto = GetDtoSelectedPath(),
                    Template = this.webApiSettings?.ZipName,
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
                    if (feature.Type == FeatureType.WebApi)
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
            try
            {
                string fileName = vm.DtoFiles[vm.DtoSelected];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    consoleWriter.AddMessageLine($"Dto file '{fileName}' not found to parse.", "Orange");
                    return;
                }

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
        /// Parse Zip files (WebApi, CRUD, option or team).
        /// </summary>
        private void ParseZipFile(FeatureType type)
        {
            try
            {
                List<string> zipSelectedList = (type == FeatureType.WebApi) ? vm.ZipDotNetSelected.ToList() : vm.ZipAngularSelected.ToList();

                FeatureTypeData zipData = GetFeatureTypeData(type, zipSelectedList);
                if (zipData != null)
                {
                    string folderName = type == FeatureType.WebApi ? Constants.FolderDotNet : vm.CurrentProject.BIAFronts;
                    List<string> options = crudSettingsList.FirstOrDefault(x => x.Type == type.ToString())?.Options;

                    ZipFilesContent filesContent = zipService.ParseZipFile(zipData, type, folderName/*, options*/);
                    if (filesContent != null)
                    {
                        vm.ZipFilesContent.Add(filesContent);
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
}
