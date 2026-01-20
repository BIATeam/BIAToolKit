namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public partial class OptionGeneratorViewModel : ObservableObject
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private readonly CSharpParserService parserService;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly CRUDSettings settings;
        private readonly IMessenger messenger;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly ITextParsingService textParsingService;

        private readonly List<FeatureGenerationSettings> backSettingsList = [];
        private List<FeatureGenerationSettings> frontSettingsList = [];
        private OptionGeneratorHelper optionHelper;
        private OptionGeneration optionGenerationHistory;

        /// <summary>  
        /// Constructor with dependency injection.
        /// </summary>
        public OptionGeneratorViewModel(
            CSharpParserService parserService,
            ZipParserService zipService,
            GenerateCrudService crudService,
            SettingsService settingsService,
            IConsoleWriter consoleWriter,
            IMessenger messenger,
            FileGeneratorService fileGeneratorService,
            ITextParsingService textParsingService)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.zipService = zipService ?? throw new ArgumentNullException(nameof(zipService));
            this.crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            this.settings = new CRUDSettings(settingsService ?? throw new ArgumentNullException(nameof(settingsService)));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.textParsingService = textParsingService ?? new TextParsingService();

            ZipFeatureTypeList = new();
            Entities = new();
            EntityDisplayItems = new();

            // Initialize commands
            OnEntitySelectedCommand = new RelayCommand(OnEntitySelected);
            OnBiaFrontSelectedCommand = new RelayCommand<string>(OnBiaFrontSelected);
            RefreshEntitiesListCommand = new RelayCommand(ListEntityFiles);
            GenerateOptionCommand = new AsyncRelayCommand(GenerateOptionAsync);
            DeleteLastGenerationCommand = new RelayCommand(DeleteLastGeneration);
            DeleteAnnotationsCommand = new AsyncRelayCommand(DeleteAnnotationsAsync);

            messenger.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            messenger.Register<SolutionClassesParsedMessage>(this, (r, m) => OnSolutionClassesParsed());
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
            }
        }

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                OnPropertyChanged(nameof(IsProjectChosen));
            }
        }

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                OnPropertyChanged(nameof(BiaFront));
            }
        }

        private ObservableCollection<string> _biaFronts = new();
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                OnPropertyChanged(nameof(BiaFronts));
            }
        }
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
                }
            }
        }

        private ObservableCollection<EntityInfo> entities;
        public ObservableCollection<EntityInfo> Entities
        {
            get => entities;
            set
            {
                if (entities != value)
                {
                    entities = value;
                    OnPropertyChanged(nameof(Entities));
                }
            }
        }

        private ObservableCollection<string> entityDisplayItems;
        public ObservableCollection<string> EntityDisplayItems
        {
            get => entityDisplayItems;
            set
            {
                if (entityDisplayItems != value)
                {
                    entityDisplayItems = value;
                    OnPropertyChanged(nameof(EntityDisplayItems));
                }
            }
        }

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

        private bool isGenerated = false;
        public bool IsGenerated
        {
            get => isGenerated;
            set
            {
                if (isGenerated != value)
                {
                    isGenerated = value;
                    OnPropertyChanged(nameof(IsGenerated));
                }
            }
        }
        #endregion

        #region Entity Name

        private string entityNamePlural;
        public string EntityNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    OnPropertyChanged(nameof(EntityNamePlural));
                }
            }
        }
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

        #region Commands
        public IRelayCommand OnEntitySelectedCommand { get; }
        public IRelayCommand<string> OnBiaFrontSelectedCommand { get; }
        public IRelayCommand RefreshEntitiesListCommand { get; }
        public IAsyncRelayCommand GenerateOptionCommand { get; }
        public IRelayCommand DeleteLastGenerationCommand { get; }
        public IAsyncRelayCommand DeleteAnnotationsCommand { get; }

        private void OnEntitySelected()
        {
            IsEntityParsed = false;
            EntityDisplayItems.Clear();
            Domain = null;
            EntityNamePlural = null;

            if (optionGenerationHistory != null && optionHelper != null)
            {
                string entityName = GetEntitySelectedPath();
                if (!string.IsNullOrEmpty(entityName))
                {
                    var history = optionHelper.LoadEntityHistory(optionGenerationHistory, entityName);
                    if (history != null)
                    {
                        EntityNamePlural = history.EntityNamePlural;
                        Domain = history.Domain;
                        BiaFront = history.BiaFront;
                        IsGenerated = true;
                    }
                }
            }

            IsEntityParsed = ParseEntityFile();
        }

        private void OnBiaFrontSelected(string selectedFront)
        {
            if (string.IsNullOrWhiteSpace(selectedFront) || optionHelper == null)
                return;

            SetFrontGenerationSettings(selectedFront);
        }

        private void DeleteLastGeneration()
        {
            try
            {
                var history = optionHelper.LoadEntityHistory(optionGenerationHistory, GetEntitySelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{Entity.Name}' generation found.", "Orange");
                    return;
                }

                crudService.DeleteLastGeneration(ZipFeatureTypeList, CurrentProject, history, FeatureType.Option.ToString(), new CrudParent { Domain = history.Domain });

                DeleteLastGenerationHistory(history);
                IsGenerated = false;

                consoleWriter.AddMessageLine($"End of '{Entity.Name}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{Entity.Name}' generation: {ex.Message}", "Red");
            }
        }

        private async Task DeleteAnnotationsAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Do you want to permanently remove all BIAToolkit annotations in code?\nAfter that you will no longer be able to regenerate old CRUDs.\n\nBe careful, this action is irreversible.",
                    "Warning",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Cancel);

                if (result == MessageBoxResult.OK)
                {
                    List<string> folders = [
                        Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
                    ];

                    await crudService.DeleteBIAToolkitAnnotations(folders);
                }

                consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on cleaning annotations for project '{CurrentProject?.Name}': {ex.Message}", "Red");
            }
        }

        private async Task GenerateOptionAsync()
        {
            if (CurrentProject is null)
                return;

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
        #endregion

        #region Lifecycle & helpers
        public void SetCurrentProject(Project project)
        {
            if (project == CurrentProject)
                return;

            ClearAll();
            CurrentProject = project;
            IsProjectChosen = project != null && project.BIAFronts.Count > 0;
            if (IsProjectChosen)
            {
                InitProject();
            }
            crudService.CurrentProject = project;
        }

        public void OnSolutionClassesParsed()
        {
            ListEntityFiles();
        }

        private void ClearAll()
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
            if (optionHelper == null)
            {
                optionHelper = new OptionGeneratorHelper(settings, fileGeneratorService, CurrentProject);
            }

            var (backSettings, frontSettings, history) = optionHelper.InitializeSettings(ZipFeatureTypeList);
            backSettingsList.Clear();
            backSettingsList.AddRange(backSettings);
            frontSettingsList.Clear();
            frontSettingsList.AddRange(frontSettings);
            optionGenerationHistory = history;
        }

        private void SetFrontGenerationSettings(string biaFront)
        {
            var frontSettings = optionHelper.LoadFrontSettings(biaFront, ZipFeatureTypeList);
            frontSettingsList.Clear();
            frontSettingsList.AddRange(frontSettings);
        }

        private void UpdateOptionGenerationHistory()
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

                optionHelper.UpdateHistory(optionGenerationHistory, history);
                IsGenerated = true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        private void DeleteLastGenerationHistory(OptionGenerationHistory history)
        {
            optionHelper.DeleteHistory(optionGenerationHistory, history);
        }

        private void ListEntityFiles()
        {
            if (CurrentProject is null)
                return;

            Entities = new ObservableCollection<EntityInfo>(parserService.GetDomainEntities(CurrentProject));
        }

        private bool ParseEntityFile()
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

                var history = optionGenerationHistory?
                    .OptionGenerationHistory?
                    .FirstOrDefault(gh => Entity.Name == textParsingService.ExtractClassNameFromFile(gh.Mapping.Entity));
                EntityDisplayItemSelected = history?.DisplayItem;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Entity File: {ex.Message}", "Red");
            }
            return false;
        }

        private string GetEntitySelectedPath()
        {
            if (Entity is null || CurrentProject is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return Entity.Name.Replace(dotNetPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
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
    }
}
