﻿namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using static BIA.ToolKit.Application.Services.UIEventBroker;

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
        private UIEventBroker uiEventBroker;
        private FileGeneratorService fileGeneratorService;

        private readonly CRUDGeneratorViewModel vm;
        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;
        private readonly List<FeatureGenerationSettings> backSettingsList;
        private List<FeatureGenerationSettings> frontSettingsList;

        /// <summary>
        /// Constructor
        /// </summary>
        public CRUDGeneratorUC()
        {
            InitializeComponent();
            vm = (CRUDGeneratorViewModel)base.DataContext;
            backSettingsList = [];
            frontSettingsList = [];
        }

        /// <summary>
        /// Injection of services.
        /// </summary>
        public void Inject(CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService,
            IConsoleWriter consoleWriter, UIEventBroker uiEventBroker, FileGeneratorService fileGeneratorService)
        {
            this.consoleWriter = consoleWriter;
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.settings = new(settingsService);
            this.uiEventBroker = uiEventBroker;
            this.uiEventBroker.OnProjectChanged += UiEventBroker_OnProjectChanged;
            this.fileGeneratorService = fileGeneratorService;
        }

        private void UiEventBroker_OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        /// <summary>
        /// Update CurrentProject value.
        /// </summary>
        public void SetCurrentProject(Project currentProject)
        {
            if (currentProject == vm.CurrentProject)
                return;

            CurrentProjectChange(currentProject);
        }

        #region State change
        /// <summary>
        /// Init data based on current page (from settings).
        /// </summary>
        private void CurrentProjectChange(Project project)
        {
            if (project is null || project.BIAFronts.Count == 0)
                return;

            uiEventBroker.ExecuteActionWithWaiter(() => InitProjectTask(project));
        }

        private Task InitProjectTask(Project project)
        {
            ClearAll();

            // Load BIA settings + history + parse zips
            InitProject(project);

            // List Dto files from Dto folder
            ListDtoFiles();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Action linked with "Dto files" combobox.
        /// </summary>
        private void ModifyDto_SelectionChange(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            vm.IsDtoParsed = false;
            vm.DtoDisplayItems = null;
            Visibility msgVisibility = Visibility.Collapsed;

            if (vm.CurrentProject is null)
                return;

            vm.ParentName = null;
            vm.ParentNamePlural = null;
            vm.HasParent = false;
            vm.Domain = null;
            vm.IsDtoParsed = ParseDtoFile();
            vm.CRUDNameSingular = GetEntityNameFromDto(vm.DtoSelected);
            vm.IsTeam = vm.DtoEntity?.IsTeam == true;
            vm.IsVersioned = vm.DtoEntity?.IsVersioned == true;
            vm.IsFixable = vm.DtoEntity?.IsFixable == true;
            vm.IsArchivable = vm.DtoEntity?.IsArchivable == true;
            vm.AncestorTeam = vm.DtoEntity?.AncestorTeamName;
            vm.SelectedBaseKeyType = vm.DtoEntity?.BaseKeyType;
            var isBackSelected = vm.IsWebApiAvailable;
            var isFrontSelected = vm.IsFrontAvailable;

            foreach (var optionItem in vm.OptionItems)
            {
                optionItem.Check = false;
            }
            if (vm.DtoEntity != null)
            {
                foreach (var property in vm.DtoEntity.Properties.Where(p => p.IsOptionDto))
                {
                    var optionType = property.Annotations.FirstOrDefault(a => a.Key == "Type").Value;
                    if (string.IsNullOrEmpty(optionType))
                        continue;

                    var optionItem = vm.OptionItems.FirstOrDefault(oi => oi.OptionName == optionType);
                    if (optionItem is null)
                        continue;

                    optionItem.Check = true;
                }
            }

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
                        vm.DtoDisplayItemSelected = history.DisplayItem;
                        vm.FeatureNameSelected = history.Feature;
                        vm.HasParent = history.HasParent;
                        vm.ParentName = history.ParentName;
                        vm.ParentNamePlural = history.ParentNamePlural;
                        vm.Domain = history.Domain;
                        vm.BiaFront = history.BiaFront;
                        vm.IsTeam = history.IsTeam;
                        vm.TeamTypeId = history.TeamTypeId;
                        vm.TeamRoleId = history.TeamRoleId;
                        vm.UseHubClient = history.UseHubClient;
                        vm.HasCustomRepository = history.UseCustomRepository;
                        vm.HasFormReadOnlyMode = history.HasFormReadOnlyMode;
                        vm.UseImport = history.UseImport;
                        vm.IsFixable = history.IsFixable;
                        vm.HasFixableParent = history.HasFixableParent;
                        vm.UseAdvancedFilter = history.HasAdvancedFilter;
                        vm.AncestorTeam = history.AncestorTeam;
                        vm.SelectedFormReadOnlyMode = history.FormReadOnlyMode;
                        vm.IsVersioned = history.IsVersioned;
                        vm.IsArchivable = history.IsArchivable;
                        vm.SelectedBaseKeyType = history.EntityBaseKeyType;
                        if (history.OptionItems != null)
                        {
                            foreach (var option in vm.OptionItems)
                            {
                                option.Check = false;
                            }

                            history.OptionItems.ForEach(o =>
                            {
                                OptionItem item = vm.OptionItems.FirstOrDefault(x => x.OptionName == o);
                                if (item != null) item.Check = item != null;
                            });
                        }

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
            uiEventBroker.ExecuteActionWithWaiter(async () =>
            {
                if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                {
                    await fileGeneratorService.GenerateCRUDAsync(new FileGeneratorCrudContext
                    {
                        CompanyName = vm.CurrentProject.CompanyName,
                        ProjectName = vm.CurrentProject.Name,
                        DomainName = vm.Domain,
                        EntityName = vm.CRUDNameSingular,
                        EntityNamePlural = vm.CRUDNamePlural,
                        BaseKeyType = vm.SelectedBaseKeyType,
                        IsTeam = vm.IsTeam,
                        Properties = [.. vm.DtoEntity.Properties],
                        OptionItems = [.. vm.OptionItems.Where(x => x.Check).Select(x => x.OptionName)],
                        HasParent = vm.HasParent,
                        ParentName = vm.ParentName,
                        ParentNamePlural = vm.ParentNamePlural,
                        AncestorTeamName = vm.AncestorTeam,
                        HasAncestorTeam = !string.IsNullOrWhiteSpace(vm.AncestorTeam),
                        AngularFront = vm.BiaFront,
                        GenerateBack = vm.IsWebApiSelected,
                        GenerateFront = vm.IsFrontSelected,
                        DisplayItemName = vm.DtoDisplayItemSelected,
                        TeamTypeId = vm.TeamTypeId,
                        TeamRoleId = vm.TeamRoleId,
                        UseHubForClient = vm.UseHubClient,
                        HasCustomRepository = vm.HasCustomRepository,
                        HasReadOnlyMode = vm.HasFormReadOnlyMode,
                        CanImport = vm.UseImport,
                        IsFixable = vm.IsFixable,
                        HasFixableParent = vm.HasFixableParent,
                        HasAdvancedFilter = vm.UseAdvancedFilter,
                        FormReadOnlyMode = vm.SelectedFormReadOnlyMode,
                        IsVersioned = vm.IsVersioned,
                        IsArchivable = vm.IsArchivable,
                    });

                    UpdateCrudGenerationHistory();
                    return;
                }

                if (!zipService.ParseZips(vm.ZipFeatureTypeList, vm.CurrentProject, vm.BiaFront, settings))
                    return;

                var crudParent = new CrudParent
                {
                    Exists = vm.HasParent,
                    Name = vm.ParentName,
                    NamePlural = vm.ParentNamePlural,
                    Domain = vm.Domain
                };

                crudService.CrudNames.InitRenameValues(vm.CRUDNameSingular, vm.CRUDNamePlural);

                // Generation DotNet + Angular files
                List<string> optionsItems = vm.OptionItems.Any() ? vm.OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
                vm.IsDtoGenerated = crudService.GenerateFiles(vm.DtoEntity, vm.ZipFeatureTypeList, vm.DtoDisplayItemSelected, optionsItems, crudParent, vm.FeatureNameSelected, vm.Domain, vm.BiaFront);

                // Generate generation history file
                UpdateCrudGenerationHistory();

                consoleWriter.AddMessageLine($"End of '{vm.CRUDNameSingular}' generation.", "Blue");
            });
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
                crudService.DeleteLastGeneration(vm.ZipFeatureTypeList, vm.CurrentProject, history, vm.FeatureNameSelected, new CrudParent { Exists = history.HasParent, Domain = history.Domain, Name = history.ParentName, NamePlural = history.ParentNamePlural }, historyOptions);

                // Update history
                DeleteLastGenerationHistory(history);
                CrudAlreadyGeneratedLabel.Visibility = Visibility.Collapsed;

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
        private async void DeleteBIAToolkitAnnotations_Click(object sender, RoutedEventArgs e)
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
                    List<string> folders = [
                        Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(vm.CurrentProject.Folder, vm.BiaFront, "src",  "app")
                    ];

                    await crudService.DeleteBIAToolkitAnnotations(folders);
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
            vm.FeatureNames.Clear();

            vm.DtoEntity = null;
            vm.DtoSelected = null;
            vm.DtoFiles = null;
            vm.IsWebApiSelected = false;
            vm.IsFrontSelected = false;
            vm.FeatureNameSelected = null;
            vm.BiaFronts.Clear();
            vm.BiaFront = null;
            vm.IsTeam = false;

            this.crudHistory = null;
        }

        private void InitProject(Project project)
        {
            try
            {
                SetGenerationSettings(project);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on intializing project: {ex.Message}", "Red");
            }
        }

        private void SetGenerationSettings(Project project)
        {
            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(project.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            this.crudHistoryFileName = Path.Combine(project.Folder, Constants.FolderBia, settings.CrudGenerationHistoryFileName);

            // Handle old path of CRUD history file
            var oldCrudHistoryFilePath = Path.Combine(project.Folder, settings.CrudGenerationHistoryFileName);
            if (File.Exists(oldCrudHistoryFilePath))
            {
                File.Move(oldCrudHistoryFilePath, this.crudHistoryFileName);
            }

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                vm.UseFileGenerator = true;
                vm.FeatureNames.Add("CRUD");
                vm.FeatureNameSelected = "CRUD";
                this.crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(this.crudHistoryFileName);
            }
            else
            {
                vm.UseFileGenerator = false;

                // Load BIA settings
                if (File.Exists(backSettingsFileName))
                {
                    backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                    if (project.FrameworkVersion == "3.9.0")
                    {
                        var crudPlanesFeature = backSettingsList.FirstOrDefault(x => x.Feature == "crud-planes");
                        if (crudPlanesFeature != null)
                        {
                            crudPlanesFeature.Feature = "planes";
                        }
                    }
                }

                foreach (var setting in backSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType == FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.WebApi, setting.ZipName, dotnetBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain);
                    vm.ZipFeatureTypeList.Add(zipFeatureType);
                }

                foreach (var featureName in vm.ZipFeatureTypeList.Select(x => x.Feature).Distinct())
                {
                    vm.FeatureNames.Add(featureName);
                }

                // Load generation history
                this.crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(this.crudHistoryFileName);
            }

            vm.CurrentProject = project;
            vm.IsProjectChosen = true;

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);

            vm.IsTeam = vm.IsTeam;
        }

        private void SetFrontGenerationSettings(string biaFront)
        {
            this.frontSettingsList.Clear();
            vm.ZipFeatureTypeList.RemoveAll(x => x.GenerationType == GenerationType.Front);

            string angularBiaFolderPath = Path.Combine(vm.CurrentProject.Folder, biaFront, Constants.FolderBia);
            string frontSettingsFileName = Path.Combine(vm.CurrentProject.Folder, biaFront, settings.GenerationSettingsFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                return;
            }

            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));
                if (vm.CurrentProject.FrameworkVersion == "3.9.0")
                {
                    var featuresToRemove = frontSettingsList.Where(x => x.Feature == "planes-full-code" || x.Feature == "aircraft-maintenance-companies");
                    frontSettingsList = frontSettingsList.Except(featuresToRemove).ToList();
                }
            }

            foreach (var setting in frontSettingsList)
            {
                var featureType = Enum.Parse<FeatureType>(setting.Type);
                if (featureType == FeatureType.Option)
                    continue;

                var zipFeatureType = new ZipFeatureType(featureType, GenerationType.Front, setting.ZipName, angularBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain);
                vm.ZipFeatureTypeList.Add(zipFeatureType);
            }
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
                    Feature = vm.FeatureNameSelected,
                    HasParent = vm.HasParent,
                    ParentName = vm.ParentName,
                    ParentNamePlural = vm.ParentNamePlural,
                    Domain = vm.Domain,
                    BiaFront = vm.BiaFront,
                    IsTeam = vm.IsTeam,
                    TeamTypeId = vm.TeamTypeId,
                    TeamRoleId = vm.TeamRoleId,
                    UseHubClient = vm.UseHubClient,
                    UseCustomRepository = vm.HasCustomRepository,
                    HasFormReadOnlyMode = vm.HasFormReadOnlyMode,
                    UseImport = vm.UseImport,
                    IsFixable = vm.IsFixable,
                    HasFixableParent = vm.HasFixableParent,
                    HasAdvancedFilter = vm.UseAdvancedFilter,
                    AncestorTeam = vm.AncestorTeam,
                    FormReadOnlyMode = vm.SelectedFormReadOnlyMode,
                    IsVersioned = vm.IsVersioned,
                    IsArchivable = vm.IsArchivable,
                    EntityBaseKeyType = vm.SelectedBaseKeyType,
                    // Create "Mapping" part
                    Mapping = new()
                    {
                        Dto = GetDtoSelectedPath(),
                        Type = DOTNET_TYPE,
                    }
                };

                if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                {
                    if (vm.IsWebApiSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.WebApi.ToString(),
                            Type = DOTNET_TYPE,
                            Folder = Constants.FolderDotNet
                        });
                    }
                    if (vm.IsFrontSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.Front.ToString(),
                            Type = ANGULAR_TYPE,
                            Folder = vm.BiaFront
                        });
                    }
                }
                else
                {
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
                                crudGeneration.Folder = vm.BiaFront;
                            }
                            history.Generation.Add(crudGeneration);
                        }
                    });
                }

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
            Dictionary<string, string> dtoFiles = [];

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

            vm.DtoFiles = dtoFiles.OrderBy(x => x.Key).ToDictionary();
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

                // Fill display item list
                List<string> displayItems = [];
                vm.DtoEntity.Properties.ForEach(p => displayItems.Add(p.Name));
                vm.DtoDisplayItems = displayItems;
                vm.DtoDisplayItemSelected = vm.DtoEntity.Properties.FirstOrDefault(p => p.Type.StartsWith("string", StringComparison.CurrentCultureIgnoreCase))?.Name;

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
        private void ParseFrontDomains()
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            List<string> foldersName = [];

            // List Options folders
            string folderPath = Path.Combine(vm.CurrentProject.Folder, vm.BiaFront, domainsPath);
            List<string> folders = [.. Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories)];
            folders.ForEach(f => foldersName.Add(new DirectoryInfo(f).Name.Replace(suffix, "")));

            // Get Options name
            vm.AddOptionItems(foldersName.Select(x => new OptionItem(CommonTools.ConvertKebabToPascalCase(x))));
        }

        

        /// <summary>
        /// Extract the entity name form Dto file name.
        /// </summary>
        private static string GetEntityNameFromDto(string dtoFileName)
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

        private void BiaFront_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SetFrontGenerationSettings(e.AddedItems[0] as string);
                ParseFrontDomains();
            }
        }
    }
}
