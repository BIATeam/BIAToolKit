namespace BIA.ToolKit.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel.Messaging.Messages;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class OptionGeneratorViewModel : ViewModelBase
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private readonly CSharpParserService service;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;

        private OptionGeneration optionGenerationHistory;
        private string optionHistoryFileName;
        private List<FeatureGenerationSettings> backSettingsList = [];
        private List<FeatureGenerationSettings> frontSettingsList = [];

        /// <summary>  
        /// Constructor.
        /// </summary>
        public OptionGeneratorViewModel(IMessenger messenger, CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService, FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
            : base(messenger)
        {
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.settings = new CRUDSettings(settingsService);
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;

            ZipFeatureTypeList = new();
            Entities = new();
            EntityDisplayItems = new();
        }

        public override void Initialize()
        {
            Messenger.Subscribe<ProjectChangedMessage>(OnProjectChanged);
            Messenger.Subscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        public override void Cleanup()
        {
            Messenger.Unsubscribe<ProjectChangedMessage>(OnProjectChanged);
            Messenger.Unsubscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        private void OnProjectChanged(ProjectChangedMessage msg)
        {
            SetCurrentProject(msg.Project);
        }

        private void OnSolutionParsed(SolutionParsedMessage msg)
        {
            ListEntityFiles();
        }

        public void SetCurrentProject(Project currentProject)
        {
            if (currentProject == CurrentProject)
                return;

            ClearAll();
            CurrentProject = currentProject;
            crudService.CurrentProject = currentProject;

            if (CurrentProject == null || CurrentProject.BIAFronts.Count == 0)
                return;

            IsProjectChosen = true;
            InitProject();
        }

        public void ClearAll()
        {
            backSettingsList.Clear();
            frontSettingsList.Clear();
            ZipFeatureTypeList.Clear();

            EntityDisplayItems.Clear();
            EntityDisplayItemSelected = null;
            Entity = null;
            Entities.Clear();
            EntityNamePlural = null;
            Domain = null;
            BiaFronts.Clear();
            BiaFront = null;

            optionGenerationHistory = null;
        }

        private void InitProject()
        {
            try
            {
                SetGenerationSettings();
                crudService.CrudNames = new(backSettingsList, frontSettingsList);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on initializing project: {ex.Message}", "Red");
            }
        }

        private void SetGenerationSettings()
        {
            string dotnetBiaFolderPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            optionHistoryFileName = Path.Combine(CurrentProject.Folder, Constants.FolderBia, settings.OptionGenerationHistoryFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                optionGenerationHistory = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFileName);
                return;
            }

            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                foreach (var setting in backSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.WebApi, setting.ZipName, dotnetBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain)
                    {
                        IsChecked = true
                    };
                    ZipFeatureTypeList.Add(zipFeatureType);
                }
            }

            optionGenerationHistory = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFileName);
        }

        public void SetFrontGenerationSettings(string biaFront)
        {
            frontSettingsList.Clear();
            ZipFeatureTypeList.RemoveAll(x => x.GenerationType == GenerationType.Front);

            string angularBiaFolderPath = Path.Combine(CurrentProject.Folder, biaFront, Constants.FolderBia);
            string frontSettingsFileName = Path.Combine(CurrentProject.Folder, biaFront, settings.GenerationSettingsFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                return;
            }

            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));
                foreach (var setting in frontSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.Front, setting.ZipName, angularBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain)
                    {
                        IsChecked = true
                    };

                    ZipFeatureTypeList.Add(zipFeatureType);
                }
            }
        }

        public void ListEntityFiles()
        {
            if (CurrentProject is null)
                return;

            Entities = new ObservableCollection<EntityInfo>(service.GetDomainEntities(CurrentProject));
        }

        public bool ParseEntityFile()
        {
            try
            {
                if (Entity is null)
                    return false;

                var namespaceParts = Entity.Namespace.Split('.').ToList();
                var domainIndex = namespaceParts.IndexOf("Domain");
                if (domainIndex != -1)
                {
                    Domain = namespaceParts[domainIndex + 1];
                }
                EntityNamePlural = Entity.NamePluralized;
                if (Entity.Properties.Count == 0)
                {
                    consoleWriter.AddMessageLine("No properties found on entity file.", "Orange");
                    return false;
                }

                foreach (var property in Entity.Properties.OrderBy(x => x.Name))
                {
                    EntityDisplayItems.Add(property.Name);
                }

                var history = optionGenerationHistory?.OptionGenerationHistory?.FirstOrDefault(gh => (Entity.Name == Path.GetFileNameWithoutExtension(gh.Mapping.Entity)));
                EntityDisplayItemSelected = history?.DisplayItem;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Entity File: {ex.Message}", "Red");
            }
            return false;
        }

        public void OnEntitySelected(EntityInfo entity)
        {
            IsEntityParsed = false;
            EntityDisplayItems.Clear();

            Domain = null;
            EntityNamePlural = null;

            bool wasAlreadyGenerated = false;
            if (optionGenerationHistory != null)
            {
                string entityName = GetEntitySelectedPath();
                if (!string.IsNullOrEmpty(entityName))
                {
                    var history = optionGenerationHistory.OptionGenerationHistory.FirstOrDefault(h => h.Mapping.Entity == entityName);

                    if (history != null)
                    {
                        EntityNamePlural = history.EntityNamePlural;
                        Domain = history.Domain;
                        BiaFront = history.BiaFront;
                        UseHubClient = history.UseHubClient;
                        wasAlreadyGenerated = true;
                    }
                }
            }

            IsGenerated = wasAlreadyGenerated;
            IsEntityParsed = ParseEntityFile();
        }

        public void UpdateOptionGenerationHistory()
        {
            try
            {
                optionGenerationHistory ??= new();

                OptionGenerationHistory history = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = Entity.Name,
                    EntityNamePlural = EntityNamePlural,
                    DisplayItem = EntityDisplayItemSelected,
                    Domain = Domain,
                    BiaFront = BiaFront,
                    UseHubClient = UseHubClient,
                    Mapping = new()
                    {
                        Entity = GetEntitySelectedPath(),
                        Type = DOTNET_TYPE,
                    }
                };

                ZipFeatureTypeList.Where(f => f.FeatureDataList != null).ToList().ForEach(feature =>
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
                            crudGeneration.Folder = BiaFront;
                        }
                        history.Generation.Add(crudGeneration);
                    }
                });

                var genFound = optionGenerationHistory.OptionGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    optionGenerationHistory.OptionGenerationHistory.Remove(genFound);
                }

                optionGenerationHistory.OptionGenerationHistory.Add(history);

                CommonTools.SerializeToJsonFile(optionGenerationHistory, optionHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        public void DeleteLastGenerationHistory(OptionGenerationHistory history)
        {
            optionGenerationHistory?.OptionGenerationHistory?.Remove(history);
            CommonTools.SerializeToJsonFile(optionGenerationHistory, optionHistoryFileName);
        }

        public OptionGenerationHistory GetCurrentEntityHistory()
        {
            return optionGenerationHistory?.OptionGenerationHistory?.FirstOrDefault(h => h.Mapping.Entity == GetEntitySelectedPath());
        }

        public async Task GenerateOptionAsync()
        {
            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                await fileGeneratorService.GenerateOptionAsync(new FileGeneratorOptionContext
                {
                    CompanyName = CurrentProject.CompanyName,
                    ProjectName = CurrentProject.Name,
                    DomainName = Domain,
                    EntityName = Entity.Name,
                    EntityNamePlural = Entity.NamePluralized,
                    BaseKeyType = Entity.BaseKeyType,
                    DisplayName = EntityDisplayItemSelected,
                    AngularFront = BiaFront,
                    UseHubForClient = UseHubClient,
                    GenerateFront = true,
                    GenerateBack = true,
                });
                UpdateOptionGenerationHistory();
                return;
            }

            if (!zipService.ParseZips(ZipFeatureTypeList, CurrentProject, BiaFront, settings))
                return;

            crudService.CrudNames.InitRenameValues(Entity.Name, EntityNamePlural);

            var featureName = ZipFeatureTypeList.FirstOrDefault(x => x.FeatureType == FeatureType.Option)?.Feature;
            IsGenerated = crudService.GenerateFiles(Entity, ZipFeatureTypeList, EntityDisplayItemSelected, null, null, featureName, Domain, BiaFront);

            UpdateOptionGenerationHistory();

            consoleWriter.AddMessageLine($"End of '{Entity.Name}' option generation.", "Blue");
        }

        public async Task DeleteLastOptionAsync(OptionGenerationHistory history)
        {
            if (history == null)
            {
                consoleWriter.AddMessageLine($"No previous '{Entity.Name}' generation found.", "Orange");
                return;
            }

            crudService.DeleteLastGeneration(ZipFeatureTypeList, CurrentProject, history, FeatureType.Option.ToString(), new CrudParent { Domain = history.Domain });

            DeleteLastGenerationHistory(history);

            consoleWriter.AddMessageLine($"End of '{Entity.Name}' suppression.", "Purple");
        }

        public async Task DeleteAnnotationsAsync(List<string> folders)
        {
            await crudService.DeleteBIAToolkitAnnotations(folders);
            consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
        }

        public async Task DeleteAnnotationsAsync()
        {
            var folders = new List<string>
            {
                Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
            };
            await DeleteAnnotationsAsync(folders);
        }

        #region Commands
        [RelayCommand]
        private void GenerateOption()
        {
            Messenger.Send(new ExecuteWithWaiterMessage { Task = GenerateOptionAsync });
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            try
            {
                var history = GetCurrentEntityHistory();
                Messenger.Send(new ExecuteWithWaiterMessage
                {
                    Task = async () =>
                    {
                        await DeleteLastOptionAsync(history);
                        IsGenerated = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error on deleting last '{Entity?.Name}' generation: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        #endregion

        public string GetEntitySelectedPath()
        {
            if (Entity is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return Entity.Name.Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }

        #region CurrentProject
        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
                BiaFronts.Clear();
                if(currentProject != null)
                {
                    foreach(var biaFront in currentProject.BIAFronts)
                    {
                        BiaFronts.Add(biaFront);
                    }
                    BiaFront = BiaFronts.FirstOrDefault();
                }

                OnPropertyChanged(nameof(IsProjectCompatibleV7));
            }
        }

        [ObservableProperty]
        private bool isProjectChosen;

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                OnPropertyChanged(nameof(BiaFront));
                if (!string.IsNullOrEmpty(value) && CurrentProject != null)
                {
                    SetFrontGenerationSettings(value);
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<string> _biaFronts = new();

        #endregion

        #region Entity
        private EntityInfo entity;
        public EntityInfo Entity
        {
            get => entity;
            set
            {
                if (entity != value)
                {
                    entity = value;
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                    OnEntitySelected(value);
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<EntityInfo> entities;

        [ObservableProperty]
        private ObservableCollection<string> entityDisplayItems;

        private bool isEntityParsed = false;
        public bool IsEntityParsed
        {
            get => isEntityParsed;
            set
            {
                if (isEntityParsed != value)
                {
                    isEntityParsed = value;
                    OnPropertyChanged(nameof(IsEntityParsed));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private string entityDisplayItemSelected;
        public string EntityDisplayItemSelected
        {
            get => entityDisplayItemSelected;
            set
            {
                if (entityDisplayItemSelected != value)
                {
                    entityDisplayItemSelected = value;
                    OnPropertyChanged(nameof(EntityDisplayItemSelected));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        [ObservableProperty]
        private bool isGenerated;
        #endregion

        #region Entity Name

        [ObservableProperty]
        private string entityNamePlural;
        #endregion

        #region Domain

        private string domain;
        public string Domain
        {
            get { return domain; }
            set 
            {
                if (domain != value)
                {
                    domain = value;
                    OnPropertyChanged(nameof(Domain));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        #endregion

        #region ZipFile
        private List<ZipFeatureType> zipFeatureTypeList;
        public List<ZipFeatureType> ZipFeatureTypeList
        {
            get => zipFeatureTypeList;
            set
            {
                if (zipFeatureTypeList != value)
                {
                    zipFeatureTypeList = value;
                }
            }
        }
        #endregion

        #region Button
        public bool IsButtonGenerateOptionEnable
        {
            get
            {
                return IsEntityParsed
                    && !string.IsNullOrWhiteSpace(EntityNamePlural)
                    && !string.IsNullOrWhiteSpace(entityDisplayItemSelected)
                    && !string.IsNullOrEmpty(Domain);
            }
        }
        #endregion

        #region CheckBox

        public bool IsProjectCompatibleV7 => Version.TryParse(CurrentProject?.FrameworkVersion, out var version) && version.Major >= 7;

        [ObservableProperty]
        private bool _useHubClient;

        #endregion
    }
}
