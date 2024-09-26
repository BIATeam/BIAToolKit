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
    /// Interaction logic for OptionGenerator.xaml
    /// </summary>
    public partial class OptionGeneratorUC : UserControl
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";
        private const int FRAMEWORK_VERSION_MINIMUM = 390;

        private IConsoleWriter consoleWriter;
        private CSharpParserService service;
        private ZipParserService zipService;
        private GenerateCrudService crudService;
        private CRUDSettings settings;

        private readonly OptionGeneratorViewModel vm;
        private OptionGeneration optionGenerationHistory;
        private string optionHistoryFileName;
        private List<FeatureGenerationSettings> backSettingsList;
        private List<FeatureGenerationSettings> frontSettingsList;
        private readonly List<string> excludedEntitiesFolders = new List<string> { "bin", "obj" };
        private readonly List<string> excludedEntitiesFilesSuffixes = new List<string> { "Mapper", "Service", "Repository", "Customizer", "Specification" };

        /// <summary>
        /// Constructor
        /// </summary>
        public OptionGeneratorUC()
        {
            InitializeComponent();
            vm = (OptionGeneratorViewModel)base.DataContext;
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

            // List Entity files from Entity folder
            ListEntityFiles();
        }

        /// <summary>
        /// Action linked with "Entity files" combobox.
        /// </summary>
        private void ModifyEntity_SelectionChange(object sender, RoutedEventArgs e)
        {
            if (vm == null) return;

            vm.IsEntityParsed = false;
            vm.EntityDisplayItems = null;
            Visibility msgVisibility = Visibility.Hidden;

            vm.Domain = vm.EntitySelected;
            vm.EntityNamePlural = null;

            if (this.optionGenerationHistory != null)
            {
                string entityName = GetEntitySelectedPath();
                if (!string.IsNullOrEmpty(entityName))
                {
                    var history = optionGenerationHistory.OptionGenerationHistory.FirstOrDefault(h => h.Mapping.Entity == entityName);

                    if (history != null)
                    {
                        // Apply last generation values
                        vm.EntitySelected = history.EntityNameSingular;
                        vm.EntityNamePlural = history.EntityNamePlural;
                        vm.Domain = history.Domain;
                        msgVisibility = Visibility.Visible;
                    }
                }

                // Get generated options
                var histories = optionGenerationHistory.OptionGenerationHistory.Where(h =>
                    (h.Mapping.Entity != entityName) &&
                    h.Generation.Any(g => g.FeatureType == FeatureType.Option.ToString())).ToList();
            }

            OptionAlreadyGeneratedLabel.Visibility = msgVisibility;
            vm.IsGenerated = OptionAlreadyGeneratedLabel.Visibility == Visibility.Visible;

            vm.IsEntityParsed = ParseEntityFile();
        }
        #endregion

        #region Button Action
        /// <summary>
        /// Action linked with "Refresh entities List" button.
        /// </summary>
        private void RefreshEntitiesList_Click(object sender, RoutedEventArgs e)
        {
            // List Entity files from Entity folder
            ListEntityFiles();
        }

        /// <summary>
        /// Action linked with "Generate" button.
        /// </summary>
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            var crudParent = new CrudParent
            {
                Domain = vm.Domain
            };

            crudService.CrudNames.InitRenameValues(vm.EntitySelected, vm.EntityNamePlural);

            // Generation DotNet + Angular files
            var featureName = vm.ZipFeatureTypeList.FirstOrDefault(x => x.FeatureType == FeatureType.Option)?.Feature;
            vm.IsGenerated = crudService.GenerateFiles(vm.Entity, vm.ZipFeatureTypeList, vm.EntityDisplayItemSelected, null, crudParent, FeatureType.Option.ToString());
            
            // Generate generation history file
            UpdateOptionGenerationHistory();

            consoleWriter.AddMessageLine($"End of '{vm.EntitySelected}' option generation.", "Blue");
        }

        /// <summary>
        /// Action linked with "Delete last generation" button.
        /// </summary>
        private void DeleteLastGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get last generation history
                var history = optionGenerationHistory?.OptionGenerationHistory?.FirstOrDefault(h => h.Mapping.Entity == GetEntitySelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{vm.EntitySelected}' generation found.", "Orange");
                    return;
                }

                // Delete last generation
                //crudService.DeleteLastGeneration(vm.ZipFeatureTypeList, vm.CurrentProject, history, null, vm.FeatureNameSelected);

                // Update history
                DeleteLastGenerationHistory(history);
                OptionAlreadyGeneratedLabel.Visibility = Visibility.Hidden;

                consoleWriter.AddMessageLine($"End of '{vm.EntitySelected}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{vm.EntitySelected}' generation: {ex.Message}", "Red");
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
            vm.ZipFeatureTypeList.Clear();

            vm.Entity = null;
            vm.EntitySelected = null;
            vm.EntityFiles = null;
            vm.EntityNamePlural = null;
            vm.Domain = null;

            this.optionGenerationHistory = null;
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
            this.optionHistoryFileName = Path.Combine(vm.CurrentProject.Folder, Constants.FolderBia, settings.OptionGenerationHistoryFileName);

            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                foreach (var setting in backSettingsList)
                {
                    var featureType = (FeatureType)Enum.Parse(typeof(FeatureType), setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.WebApi, setting.ZipName, dotnetBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent)
                    {
                        IsChecked = true
                    };
                    vm.ZipFeatureTypeList.Add(zipFeatureType);
                }
            }
            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));
                foreach (var setting in frontSettingsList)
                {
                    var featureType = (FeatureType)Enum.Parse(typeof(FeatureType), setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.Front, setting.ZipName, angularBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent)
                    {
                        IsChecked = true
                    };

                    vm.ZipFeatureTypeList.Add(zipFeatureType);
                }
            }

            // Load generation history
            this.optionGenerationHistory = CommonTools.DeserializeJsonFile<OptionGeneration>(this.optionHistoryFileName);
        }

        /// <summary>
        /// Update option generation history file.
        /// </summary>
        private void UpdateOptionGenerationHistory()
        {
            try
            {
                this.optionGenerationHistory ??= new();

                OptionGenerationHistory history = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = vm.EntitySelected,
                    EntityNamePlural = vm.EntityNamePlural,
                    DisplayItem = vm.EntityDisplayItemSelected,
                    Domain = vm.Domain,

                    // Create "Mapping" part
                    Mapping = new()
                    {
                        Entity = GetEntitySelectedPath(),
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
                var genFound = this.optionGenerationHistory.OptionGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    // Remove last generation to replace by new generation
                    this.optionGenerationHistory.OptionGenerationHistory.Remove(genFound);
                }

                this.optionGenerationHistory.OptionGenerationHistory.Add(history);

                // Generate history file
                CommonTools.SerializeToJsonFile(this.optionGenerationHistory, this.optionHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Delete last generation on history file.
        /// </summary>
        private void DeleteLastGenerationHistory(OptionGenerationHistory history)
        {
            // Delete GenerationHistory
            optionGenerationHistory?.OptionGenerationHistory?.Remove(history);

            // Generate history file
            CommonTools.SerializeToJsonFile(this.optionGenerationHistory, this.optionHistoryFileName);
        }

        /// <summary>
        /// List all entity files from current project.
        /// </summary>
        private void ListEntityFiles()
        {
            Dictionary<string, string> entityFiles = new();

            string entitiesFolder = $"{vm.CurrentProject.CompanyName}.{vm.CurrentProject.Name}.Domain";
            string path = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet, entitiesFolder);

            try
            {
                var files = new List<string>();
                if (Directory.Exists(path))
                {
                    var subFolders = Directory.GetDirectories(path).Where(x => !excludedEntitiesFolders.Contains(Path.GetFileName(x))).ToList();
                    foreach(var subFolder in subFolders)
                    {
                        var subFolderFiles = Directory.EnumerateFiles(subFolder, "*.cs", SearchOption.AllDirectories);
                        files.AddRange(subFolderFiles.Where(file => !excludedEntitiesFilesSuffixes.Any(suffix => Path.GetFileNameWithoutExtension(file).EndsWith(suffix))));
                    }
                }
                // Build dictionnary: key = file Name, Value = full path
                foreach(var file in files.OrderBy(x => Path.GetFileName(x)))
                {
                    entityFiles.Add(Path.GetFileNameWithoutExtension(file), file);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            vm.EntityFiles = entityFiles;
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

            var biaBackFolder = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet, Constants.FolderBia);
            if (Directory.Exists(biaBackFolder))
            {
                foreach (var directory in Directory.GetDirectories(biaBackFolder))
                {
                    Directory.Delete(directory, true);
                }
            }

            var biaFrontFolder = Path.Combine(vm.CurrentProject.Folder, Constants.FolderAngular, Constants.FolderBia);
            if (Directory.Exists(biaFrontFolder))
            {
                foreach (var directory in Directory.GetDirectories(biaFrontFolder))
                {
                    Directory.Delete(directory, true);
                }
            }

            bool parsed = false;
            foreach(var zipFeatureType in vm.ZipFeatureTypeList)
            {
                parsed |= ParseZipFile(zipFeatureType);
            }
            vm.IsZipParsed = parsed;
        }

        /// <summary>
        /// Parse the Entity file.
        /// </summary>
        private bool ParseEntityFile()
        {
            try
            {
                if (string.IsNullOrEmpty(vm.EntitySelected))
                    return false;

                // Check selected Entity file
                string fileName = vm.EntityFiles[vm.EntitySelected];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    consoleWriter.AddMessageLine($"Entity file '{fileName}' not found to parse.", "Orange");
                    return false;
                }

                // Parse Entity entity file
                vm.Entity = service.ParseEntity(fileName, settings.DtoCustomAttributeFieldName, settings.DtoCustomAttributeClassName);
                if (vm.Entity == null || vm.Entity.Properties == null || vm.Entity.Properties.Count <= 0)
                {
                    consoleWriter.AddMessageLine("No properties found on entity file.", "Orange");
                    return false;
                }

                // Fill display item list
                List<string> displayItems = new();
                vm.Entity.Properties.ForEach(p => {
                    if (!p.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                        displayItems.Add(p.Name);
                    });
                vm.EntityDisplayItems = displayItems;

                // Set by default previous generation selected value
                var history = this.optionGenerationHistory?.OptionGenerationHistory?.FirstOrDefault(gh => (vm.EntitySelected == Path.GetFileNameWithoutExtension(gh.Mapping.Entity)));
                vm.EntityDisplayItemSelected = history?.DisplayItem;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Entity File: {ex.Message}", "Red");
            }
            return false;
        }

        /// <summary>
        /// Parse Zip feature files.
        /// </summary>
        private bool ParseZipFile(ZipFeatureType zipData)
        {
            try
            {
                string folderName = (zipData.GenerationType == GenerationType.WebApi) ? Constants.FolderDotNet : vm.CurrentProject.BIAFronts;
                string biaFolder = Path.Combine(vm.CurrentProject.Folder, folderName, Constants.FolderBia);
                if (!new DirectoryInfo(biaFolder).Exists)
                {
                    return false;
                }

                return zipService.ParseZipFile(zipData, biaFolder, settings.DtoCustomAttributeFieldName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing '{zipData.FeatureType}' Zip File: {ex.Message}", "Red");
            }
            return false;
        }

        /// <summary>
        /// Get the Entity path from select entity file.
        /// </summary>
        private string GetEntitySelectedPath()
        {
            if (string.IsNullOrWhiteSpace(vm.EntitySelected))
                return null;

            string dotNetPath = Path.Combine(vm.CurrentProject.Folder, Constants.FolderDotNet);
            return vm.EntityFiles[vm.EntitySelected].Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }
        #endregion
    }
}
