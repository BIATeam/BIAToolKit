namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Common;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    public partial class CRUDGeneratorViewModel : ObservableObject
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
        private CRUDGeneratorHelper crudHelper;
        private CRUDGeneration crudHistory;

        // Helper to keep existing RaisePropertyChanged calls after migrating to CommunityToolkit
        protected void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel(
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

            OptionItems = [];
            ZipFeatureTypeList = [];
            FeatureNames = [];
            DtoEntities = [];

            OnDtoSelectedCommand = new RelayCommand(OnDtoSelected);
            OnEntitySingularNameChangedCommand = new RelayCommand(OnEntitySingularNameChanged);
            OnEntityPluralNameChangedCommand = new RelayCommand(OnEntityPluralNameChanged);
            OnBiaFrontSelectedCommand = new RelayCommand<string>(OnBiaFrontSelected);

            RefreshDtoListCommand = new RelayCommand(ListDtoFiles);
            GenerateCrudCommand = new AsyncRelayCommand(GenerateCrudAsync);
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

                RaisePropertyChanged(nameof(IsProjectCompatibleV6));
            }
        }

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                RaisePropertyChanged(nameof(IsProjectChosen));
            }
        }

        private bool _useFileGenerator;
        public bool UseFileGenerator
        {
            get => _useFileGenerator;
            set
            {
                _useFileGenerator = value;
                RaisePropertyChanged(nameof(UseFileGenerator));
            }
        }
        #endregion

        #region Dto
        private EntityInfo dtoEntity;
        public EntityInfo DtoEntity
        {
            get => dtoEntity;
            set
            {
                if (dtoEntity != value)
                {
                    dtoEntity = value;
                    UpdateParentPreSelection();
                    UpdateDomainPreSelection();
                }
            }
        }

        private ObservableCollection<EntityInfo> dtoEntities;
        public ObservableCollection<EntityInfo> DtoEntities
        {
            get => dtoEntities;
            set
            {
                if (dtoEntities != value)
                {
                    dtoEntities = value;
                    RaisePropertyChanged(nameof(DtoEntities));
                }
            }
        }

        private List<string> dtoDisplayItems;
        public List<string> DtoDisplayItems
        {
            get => dtoDisplayItems;
            set
            {
                if (dtoDisplayItems != value)
                {
                    dtoDisplayItems = value;
                    RaisePropertyChanged(nameof(DtoDisplayItems));
                }
            }
        }

        private bool isDtoParsed = false;
        public bool IsDtoParsed
        {
            get => isDtoParsed;
            set
            {
                if (isDtoParsed != value)
                {
                    isDtoParsed = value;
                    RaisePropertyChanged(nameof(IsDtoParsed));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                }
            }
        }

        private string dtoDisplayItemSelected;
        public string DtoDisplayItemSelected
        {
            get => dtoDisplayItemSelected;
            set
            {
                if (dtoDisplayItemSelected != value)
                {
                    dtoDisplayItemSelected = value;
                    RaisePropertyChanged(nameof(DtoDisplayItemSelected));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private ObservableCollection<OptionItem> optionItems = [];
        public ObservableCollection<OptionItem> OptionItems
        {
            get => optionItems;
            set
            {
                if (optionItems != value)
                {
                    optionItems = value;
                    RaisePropertyChanged(nameof(OptionItems));
                }
            }
        }

        public static IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;
        private string selectedBaseKeyType;

        public string SelectedBaseKeyType
        {
            get { return selectedBaseKeyType; }
            set
            {
                selectedBaseKeyType = value;
                RaisePropertyChanged(nameof(SelectedBaseKeyType));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        public string SelectedOptionItems => string.Join(", ", OptionItems.Where(x => x.Check).Select(x => x.OptionName));

        private bool isDtoGenerated = false;
        public bool IsDtoGenerated
        {
            get => isDtoGenerated;
            set
            {
                if (isDtoGenerated != value)
                {
                    isDtoGenerated = value;
                    RaisePropertyChanged(nameof(IsDtoGenerated));
                }
            }
        }

        public void AddOptionItems(IEnumerable<OptionItem> optionItems)
        {
            OptionItems.Clear();
            foreach (var optionItem in optionItems.OrderBy(x => x.OptionName))
            {
                OptionItems.Add(optionItem);
            }
            foreach(var optionItem in OptionItems)
            {
                optionItem.PropertyChanged += OptionItem_PropertyChanged;
            }
        }

        private void OptionItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(SelectedOptionItems));
        }
        #endregion

        #region Feature
        private ObservableCollection<string> featureNames;
        public ObservableCollection<string> FeatureNames
        {
            get => featureNames;
            set
            {
                if (featureNames != value)
                {
                    featureNames = value;
                    RaisePropertyChanged(nameof(FeatureNames));
                }
            }
        }

        private string featureNameSelected;
        public string FeatureNameSelected
        {
            get => featureNameSelected;
            set
            {
                if (featureNameSelected != value)
                {
                    featureNameSelected = value;
                    RaisePropertyChanged(nameof(FeatureNameSelected));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    RaisePropertyChanged(nameof(IsWebApiAvailable));
                    RaisePropertyChanged(nameof(IsFrontAvailable));
                    IsWebApiSelected = IsWebApiAvailable;
                    IsFrontSelected = IsFrontAvailable;
                    UpdateParentPreSelection();
                    UpdateDomainPreSelection();

                    if (!UseFileGenerator)
                    {
                        IsTeam = IsTeam || featureNameSelected == "Team";
                    }
                }
            }
        }

        private void UpdateFeatureSelection()
        {
            ZipFeatureTypeList.ForEach(x => x.IsChecked = false);
            if (string.IsNullOrEmpty(featureNameSelected))
                return;

            foreach (var zipFeatureType in ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected))
            {
                if (zipFeatureType.GenerationType == GenerationType.WebApi && isWebApiSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
                if (zipFeatureType.GenerationType == GenerationType.Front && isFrontSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
            }
        }

        #endregion

        #region CRUD Name
        private string entityNameSingular;
        public string CRUDNameSingular
        {
            get => entityNameSingular;
            set
            {
                if (entityNameSingular != value)
                {
                    entityNameSingular = value;
                    RaisePropertyChanged(nameof(CRUDNameSingular));
                    CRUDNamePlural = value.Pluralize();
                }
            }
        }

        private string entityNamePlural;
        public string CRUDNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    RaisePropertyChanged(nameof(CRUDNamePlural));
                }
            }
        }
        #endregion

        #region Parent
        public bool IsCheckboxParentEnable
        {
            get
            {
                if(UseFileGenerator)
                {
                    return true;
                }

                // CRUD feature always disable
                if (!string.IsNullOrEmpty(FeatureNameSelected) && FeatureNameSelected.Equals("CRUD"))
                    return false;

                var selectedFeaturesWithParent = ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected && x.Parents.Any(y => y.IsPrincipal));
                // Let checkbox editable if parent is not needed
                if (selectedFeaturesWithParent.Any() && selectedFeaturesWithParent.All(x => !x.NeedParent))
                    return true;

                return false;
            }
        }

        private bool hasParent;
        public bool HasParent
        {
            get { return hasParent; }
            set
            {
                if (hasParent != value)
                {
                    hasParent = value;
                    RaisePropertyChanged(nameof(HasParent));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));

                    if (value == false)
                    {
                        ParentName = null;
                        ParentNamePlural = null;
                    }
                    else
                    {
                        UpdateParentPreSelection();
                    }
                }
            }
        }

        private string domain;
        public string Domain
        {
            get { return domain; }
            set
            {
                if (domain != value)
                {
                    domain = value;
                    RaisePropertyChanged(nameof(Domain));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private string parentName;
        public string ParentName
        {
            get { return parentName; }
            set
            {
                if (parentName != value)
                {
                    parentName = value;
                    RaisePropertyChanged(nameof(ParentName));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    ParentNamePlural = value.Pluralize();
                }
            }
        }

        private string parentNamePlural;
        public string ParentNamePlural
        {
            get { return parentNamePlural; }
            set
            {
                if (parentNamePlural != value)
                {
                    parentNamePlural = value;
                    RaisePropertyChanged(nameof(ParentNamePlural));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private void UpdateParentPreSelection()
        {
            if(!string.IsNullOrWhiteSpace(ParentName))
            {
                return;
            }

            var selectedFeaturesWithParent = ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected && x.Parents.Any(y => y.IsPrincipal));
            if ((UseFileGenerator || selectedFeaturesWithParent.Any()) && DtoEntity != null)
            {
                var propertiesWithParent = DtoEntity.Properties.Where(x => x.Annotations != null && x.Annotations.Any(y => y.Key == "IsParent"));
                var parentPropertyName = propertiesWithParent.FirstOrDefault(x => x.Name.EndsWith("Id"))?.Name;
                if (!string.IsNullOrEmpty(parentPropertyName))
                {
                    var parentName = parentPropertyName.Replace("Id", string.Empty);
                    ParentName = parentName;
                    HasParent = true;
                }
                if(!UseFileGenerator && !HasParent)
                {
                    HasParent = selectedFeaturesWithParent.Any(x => x.NeedParent);
                }
            }
            else
            {
                HasParent = selectedFeaturesWithParent.Any(x => x.NeedParent);
            }

            RaisePropertyChanged(nameof(IsCheckboxParentEnable));
        }

        private void UpdateDomainPreSelection()
        {
            if (DtoEntity == null)
            {
                Domain = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(Domain))
            {
                return;
            }

            var namespaceParts = DtoEntity.Namespace.Split('.').ToList();
            var domainIndex = namespaceParts.IndexOf("Dto");
            if (domainIndex != -1)
            {
                Domain = namespaceParts[domainIndex + 1];
            }
        }

        #endregion

        #region CheckBox
        private readonly bool isSelectionChange = false;
        public bool IsSelectionChange
        {
            get => isSelectionChange;
            set
            {
                if (isSelectionChange != value)
                {
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private bool isWebApiSelected;
        public bool IsWebApiSelected
        {
            get => isWebApiSelected;
            set
            {
                if (isWebApiSelected != value)
                {
                    isWebApiSelected = value;
                    RaisePropertyChanged(nameof(IsWebApiSelected));
                }
                UpdateFeatureSelection();
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private bool isFrontSelected;
        public bool IsFrontSelected
        {
            get => isFrontSelected;
            set
            {
                if (isFrontSelected != value)
                {
                    isFrontSelected = value;
                    RaisePropertyChanged(nameof(IsFrontSelected));
                    if(value == false)
                    {
                        BiaFront = null;
                    }
                }
                UpdateFeatureSelection();
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                RaisePropertyChanged(nameof(BiaFront));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private ObservableCollection<string> _biaFronts = [];
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                RaisePropertyChanged(nameof(BiaFronts));
            }
        }

        public bool IsWebApiAvailable => UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.WebApi));
        public bool IsFrontAvailable => UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.Front));

        private bool _isTeam;

        public bool IsTeam
        {
            get => _isTeam;
            set
            {
                _isTeam = value;
                RaisePropertyChanged(nameof(IsTeam));
                RaisePropertyChanged(nameof(IsCheckBoxIsTeamEnable));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        public bool IsCheckBoxIsTeamEnable
        {
            get
            {
                if (UseFileGenerator)
                    return true;

                if (!string.IsNullOrEmpty(FeatureNameSelected) && FeatureNameSelected.Equals("Team"))
                    return false;

                return IsDtoParsed;
            }
        }

        private bool _useHubClient;

        public bool UseHubClient
        {
            get { return _useHubClient; }
            set 
            { 
                _useHubClient = value; 
                RaisePropertyChanged(nameof(UseHubClient));
            }
        }

        private bool _hasCustomRepository;

        public bool HasCustomRepository
        {
            get { return _hasCustomRepository; }
            set
            {
                _hasCustomRepository = value;
                RaisePropertyChanged(nameof(HasCustomRepository));
            }
        }

        private bool _hasFormReadOnlyMode;

        public bool HasFormReadOnlyMode
        {
            get { return _hasFormReadOnlyMode; }
            set
            {
                _hasFormReadOnlyMode = value;
                RaisePropertyChanged(nameof(HasFormReadOnlyMode));
                SelectedFormReadOnlyMode = value ? FormReadOnlyModes.First() : string.Empty;
            }
        }

        private bool _useImport;

        public bool UseImport
        {
            get { return _useImport; }
            set
            {
                _useImport = value;
                RaisePropertyChanged(nameof(UseImport));
            }
        }

        private bool _isFixable;

        public bool IsFixable
        {
            get { return _isFixable; }
            set
            {
                _isFixable = value;
                RaisePropertyChanged(nameof(IsFixable));
            }
        }

        private bool _hasFixableParent;

        public bool HasFixableParent
        {
            get { return _hasFixableParent; }
            set
            {
                _hasFixableParent = value;
                RaisePropertyChanged(nameof(HasFixableParent));
            }
        }

        private bool _isVersioned;

        public bool IsVersioned
        {
            get { return _isVersioned; }
            set
            {
                _isVersioned = value;
                RaisePropertyChanged(nameof(IsVersioned));
            }
        }

        private bool _isArchivable;

        public bool IsArchivable
        {
            get { return _isArchivable; }
            set
            {
                _isArchivable = value;
                RaisePropertyChanged(nameof(IsArchivable));
            }
        }



        private bool _useAdvancedFilter;

        public bool UseAdvancedFilter
        {
            get { return _useAdvancedFilter; }
            set
            {
                _useAdvancedFilter = value;
                RaisePropertyChanged(nameof(UseAdvancedFilter));
            }
        }

        public bool IsProjectCompatibleV6 => Version.TryParse(CurrentProject?.FrameworkVersion, out var version) && version.Major >= 6;

        private bool displayHistorical;
        public bool DisplayHistorical
        {
            get { return displayHistorical; }
            set
            {
                displayHistorical = value;
                RaisePropertyChanged(nameof(DisplayHistorical));
            }
        }

        private bool useDomainUrl;
        public bool UseDomainUrl
        {
            get { return useDomainUrl; }
            set
            {
                useDomainUrl = value;
                RaisePropertyChanged(nameof(UseDomainUrl));
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

        #region Commands (Phase 6 - Step 40)
        public IRelayCommand OnDtoSelectedCommand { get; }

        public IRelayCommand OnEntitySingularNameChangedCommand { get; }

        public IRelayCommand OnEntityPluralNameChangedCommand { get; }

        public IRelayCommand OnBiaFrontSelectedCommand { get; }

        public IRelayCommand RefreshDtoListCommand { get; }

        public IRelayCommand GenerateCrudCommand { get; }

        public IRelayCommand DeleteLastGenerationCommand { get; }

        public IRelayCommand DeleteAnnotationsCommand { get; }

        private void OnDtoSelected()
        {
            if (CurrentProject is null)
                return;

            IsDtoParsed = ParseDtoFile();
            CRUDNameSingular = textParsingService.ExtractEntityNameFromDtoFile(DtoEntity?.Name);
            IsTeam = DtoEntity?.IsTeam == true;
            IsVersioned = DtoEntity?.IsVersioned == true;
            IsFixable = DtoEntity?.IsFixable == true;
            IsArchivable = DtoEntity?.IsArchivable == true;
            AncestorTeam = DtoEntity?.AncestorTeamName;
            SelectedBaseKeyType = DtoEntity?.BaseKeyType;
            var isBackSelected = IsWebApiAvailable;
            var isFrontSelected = IsFrontAvailable;

            foreach (var optionItem in OptionItems)
            {
                optionItem.Check = false;
            }

            if (DtoEntity != null)
            {
                foreach (var property in DtoEntity.Properties.Where(p => p.IsOptionDto))
                {
                    var optionType = property.Annotations.FirstOrDefault(a => a.Key == "Type").Value;
                    if (string.IsNullOrEmpty(optionType))
                        continue;

                    var optionItem = OptionItems.FirstOrDefault(oi => oi.OptionName == optionType);
                    if (optionItem is null)
                        continue;

                    optionItem.Check = true;
                }
            }

            if (crudHistory != null && crudHelper != null)
            {
                string dtoName = GetDtoSelectedPath();
                if (!string.IsNullOrEmpty(dtoName))
                {
                    CRUDGenerationHistory history = crudHelper.LoadDtoHistory(crudHistory, dtoName);

                    if (history != null)
                    {
                        CRUDNameSingular = history.EntityNameSingular;
                        CRUDNamePlural = history.EntityNamePlural;
                        DtoDisplayItemSelected = history.DisplayItem;
                        FeatureNameSelected = history.Feature;
                        HasParent = history.HasParent;
                        ParentName = history.ParentName;
                        ParentNamePlural = history.ParentNamePlural;
                        Domain = history.Domain;
                        BiaFront = history.BiaFront;
                        IsTeam = history.IsTeam;
                        TeamTypeId = history.TeamTypeId;
                        TeamRoleId = history.TeamRoleId;
                        UseHubClient = history.UseHubClient;
                        HasCustomRepository = history.UseCustomRepository;
                        HasFormReadOnlyMode = history.HasFormReadOnlyMode;
                        UseImport = history.UseImport;
                        IsFixable = history.IsFixable;
                        HasFixableParent = history.HasFixableParent;
                        UseAdvancedFilter = history.HasAdvancedFilter;
                        AncestorTeam = history.AncestorTeam;
                        SelectedFormReadOnlyMode = history.FormReadOnlyMode;
                        IsVersioned = history.IsVersioned;
                        IsArchivable = history.IsArchivable;
                        SelectedBaseKeyType = history.EntityBaseKeyType;
                        DisplayHistorical = history.DisplayHistorical;
                        UseDomainUrl = history.UseDomainUrl;

                        foreach (var optionItem in OptionItems)
                        {
                            optionItem.Check = history.OptionItems?.Contains(optionItem.OptionName) == true;
                        }
                    }
                    else
                    {
                        IsDtoGenerated = false;
                    }
                }
            }

            IsDtoGenerated = crudHistory?.CRUDGenerationHistory?.Any(h => h.EntityNameSingular == CRUDNameSingular) == true;
            IsWebApiSelected = isBackSelected;
            IsFrontSelected = isFrontSelected;
        }

        private void OnEntitySingularNameChanged()
        {
            CRUDNamePlural = string.Empty;
        }

        private void OnEntityPluralNameChanged()
        {
            IsSelectionChange = true;
        }

        private void OnBiaFrontSelected(string selectedFront)
        {
            if (string.IsNullOrWhiteSpace(selectedFront) || crudHelper == null)
                return;

            SetFrontGenerationSettings(selectedFront);
            ParseFrontDomains();
        }

        private void DeleteLastGeneration()
        {
            try
            {
                CRUDGenerationHistory history = crudHelper.LoadDtoHistory(crudHistory, GetDtoSelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{CRUDNameSingular}' generation found.", "Orange");
                    return;
                }

                List<CRUDGenerationHistory> historyOptions = crudHelper.GetHistoriesUsingOption(crudHistory, CRUDNameSingular);

                crudService.DeleteLastGeneration(ZipFeatureTypeList, CurrentProject, history, FeatureNameSelected, new CrudParent { Exists = history.HasParent, Domain = history.Domain, Name = history.ParentName, NamePlural = history.ParentNamePlural }, historyOptions);

                DeleteLastGenerationHistory(history);
                IsDtoGenerated = false;

                consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{CRUDNameSingular}' generation: {ex.Message}", "Red");
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
                        Path.Combine(CurrentProject.Folder, BiaFront, "src",  "app")
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
        #endregion

        #region Lifecycle & helpers
        public void SetCurrentProject(Project project)
        {
            if (project == CurrentProject)
                return;

            CurrentProject = project;
            InitProjectTask(project);
        }

        public void OnSolutionClassesParsed()
        {
            if (CurrentProject is not null)
            {
                InitProjectTask(CurrentProject);
                ListDtoFiles();
            }
        }

        private void InitProjectTask(Project project)
        {
            ClearAll();

            if (project is not null)
            {
                InitProject(project);
            }
        }

        private void ClearAll()
        {
            backSettingsList.Clear();
            frontSettingsList.Clear();
            OptionItems?.Clear();
            ZipFeatureTypeList.Clear();
            FeatureNames.Clear();

            DtoEntity = null;
            DtoEntities.Clear();
            IsWebApiSelected = false;
            IsFrontSelected = false;
            FeatureNameSelected = null;
            BiaFronts.Clear();
            BiaFront = null;
            IsTeam = false;
            DisplayHistorical = false;
            UseDomainUrl = false;

            crudHistory = null;
        }

        private void InitProject(Project project)
        {
            try
            {
                SetGenerationSettings(project);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on initializing project: {ex.Message}", "Red");
            }
        }

        private void SetGenerationSettings(Project project)
        {
            if (crudHelper == null)
            {
                crudHelper = new CRUDGeneratorHelper(settings, fileGeneratorService, project);
            }

            var (backSettings, frontSettings, featureNames, history, useFileGenerator) =
                crudHelper.InitializeSettings(ZipFeatureTypeList);

            backSettingsList.Clear();
            backSettingsList.AddRange(backSettings);
            frontSettingsList.Clear();
            frontSettingsList.AddRange(frontSettings);

            FeatureNames.Clear();
            foreach (var featureName in featureNames)
            {
                FeatureNames.Add(featureName);
            }

            if (useFileGenerator)
            {
                FeatureNameSelected = "CRUD";
            }

            crudHistory = history;
            UseFileGenerator = useFileGenerator;
            CurrentProject = project;
            IsProjectChosen = true;

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);
        }

        private void SetFrontGenerationSettings(string biaFront)
        {
            var frontSettings = crudHelper.LoadFrontSettings(biaFront, ZipFeatureTypeList);
            frontSettingsList.Clear();
            frontSettingsList = frontSettings;
        }

        private void UpdateCrudGenerationHistory()
        {
            try
            {
                crudHistory ??= new();

                CRUDGenerationHistory history = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = CRUDNameSingular,
                    EntityNamePlural = CRUDNamePlural,
                    DisplayItem = DtoDisplayItemSelected,
                    OptionItems = OptionItems?.Where(o => o.Check).Select(o => o.OptionName).ToList(),
                    Feature = FeatureNameSelected,
                    HasParent = HasParent,
                    ParentName = ParentName,
                    ParentNamePlural = ParentNamePlural,
                    Domain = Domain,
                    BiaFront = BiaFront,
                    IsTeam = IsTeam,
                    TeamTypeId = TeamTypeId,
                    TeamRoleId = TeamRoleId,
                    UseHubClient = UseHubClient,
                    UseCustomRepository = HasCustomRepository,
                    HasFormReadOnlyMode = HasFormReadOnlyMode,
                    UseImport = UseImport,
                    IsFixable = IsFixable,
                    HasFixableParent = HasFixableParent,
                    HasAdvancedFilter = UseAdvancedFilter,
                    AncestorTeam = AncestorTeam,
                    FormReadOnlyMode = SelectedFormReadOnlyMode,
                    IsVersioned = IsVersioned,
                    IsArchivable = IsArchivable,
                    EntityBaseKeyType = SelectedBaseKeyType,
                    DisplayHistorical = DisplayHistorical,
                    UseDomainUrl = UseDomainUrl,
                };

                var feature = ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected);

                if (feature != null)
                {
                    feature.ToList().ForEach(f =>
                    {
                        if (f.IsChecked)
                        {
                            Generation crudGeneration = new()
                            {
                                GenerationType = f.GenerationType.ToString(),
                                FeatureType = f.FeatureType.ToString(),
                                Template = f.ZipName
                            };
                            if (f.GenerationType == GenerationType.WebApi)
                            {
                                crudGeneration.Type = DOTNET_TYPE;
                                crudGeneration.Folder = Constants.FolderDotNet;
                            }
                            else if (f.GenerationType == GenerationType.Front)
                            {
                                crudGeneration.Type = ANGULAR_TYPE;
                                crudGeneration.Folder = BiaFront;
                            }
                            history.Generation.Add(crudGeneration);
                        }
                    });
                }

                crudHelper.UpdateHistory(crudHistory, history);
                IsDtoGenerated = true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        private void DeleteLastGenerationHistory(CRUDGenerationHistory history)
        {
            crudHelper.DeleteHistory(crudHistory, history);
        }

        private void ListDtoFiles()
        {
            if (CurrentProject is null)
                return;

            List<EntityInfo> dtoEntities = [];

            string dtoFolder = $"{CurrentProject.CompanyName}.{CurrentProject.Name}.Domain.Dto";
            string dtoFolderPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet, dtoFolder);

            try
            {
                if (Directory.Exists(dtoFolderPath))
                {
                    foreach (var dtoClass in parserService.CurrentSolutionClasses.Where(x =>
                        x.FilePath.StartsWith(dtoFolderPath, StringComparison.InvariantCultureIgnoreCase)
                        && x.FilePath.EndsWith("Dto.cs")))
                    {
                        dtoEntities.Add(new EntityInfo(dtoClass));
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            DtoEntities = new(dtoEntities.OrderBy(x => x.Name));
        }

        private bool ParseDtoFile()
        {
            try
            {
                if (DtoEntity is null)
                    return false;

                List<string> displayItems = [];
                foreach(var property in DtoEntity.Properties.OrderBy(x => x.Name))
                {
                    displayItems.Add(property.Name);
                }

                DtoDisplayItems = displayItems;
                DtoDisplayItemSelected = DtoEntity.Properties.FirstOrDefault(p => p.Type.StartsWith("string", StringComparison.CurrentCultureIgnoreCase))?.Name;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Dto File: {ex.Message}", "Red");
            }
            return false;
        }

        private string GetDtoSelectedPath()
        {
            if (DtoEntity is null || CurrentProject is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return DtoEntity.Path.Replace(dotNetPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
        }

        private void ParseFrontDomains()
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            List<string> foldersName = [];

            string folderPath = Path.Combine(CurrentProject.Folder, BiaFront, domainsPath);
            if(!Directory.Exists(folderPath))
                return;

            List<string> folders = [.. Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories)];
            folders.ForEach(f => foldersName.Add(new DirectoryInfo(f).Name.Replace(suffix, "")));

            AddOptionItems(foldersName.Select(x => new OptionItem(CommonTools.ConvertKebabToPascalCase(x))));
        }

        private async Task GenerateCrudAsync()
        {
            if (CurrentProject is null)
                return;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                await fileGeneratorService.GenerateCRUDAsync(new FileGeneratorCrudContext
                {
                    CompanyName = CurrentProject.CompanyName,
                    ProjectName = CurrentProject.Name,
                    DomainName = Domain,
                    EntityName = CRUDNameSingular,
                    EntityNamePlural = CRUDNamePlural,
                    BaseKeyType = SelectedBaseKeyType,
                    IsTeam = IsTeam,
                    Properties = [.. DtoEntity.Properties],
                    OptionItems = [.. OptionItems.Where(x => x.Check).Select(x => x.OptionName)],
                    HasParent = HasParent,
                    ParentName = ParentName,
                    ParentNamePlural = ParentNamePlural,
                    AncestorTeamName = AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrWhiteSpace(AncestorTeam),
                    AngularFront = BiaFront,
                    GenerateBack = IsWebApiSelected,
                    GenerateFront = IsFrontSelected,
                    DisplayItemName = DtoDisplayItemSelected,
                    TeamTypeId = TeamTypeId,
                    TeamRoleId = TeamRoleId,
                    UseHubForClient = UseHubClient,
                    HasCustomRepository = HasCustomRepository,
                    HasReadOnlyMode = HasFormReadOnlyMode,
                    CanImport = UseImport,
                    IsFixable = IsFixable,
                    HasFixableParent = HasFixableParent,
                    HasAdvancedFilter = UseAdvancedFilter,
                    FormReadOnlyMode = SelectedFormReadOnlyMode,
                    IsVersioned = IsVersioned,
                    IsArchivable = IsArchivable,
                    DisplayHistorical = DisplayHistorical,
                    UseDomainUrl = UseDomainUrl,
                });

                UpdateCrudGenerationHistory();
                return;
            }

            if (!zipService.ParseZips(ZipFeatureTypeList, CurrentProject, BiaFront, settings))
                return;

            var crudParent = new CrudParent
            {
                Exists = HasParent,
                Name = ParentName,
                NamePlural = ParentNamePlural,
                Domain = Domain
            };

            crudService.CrudNames.InitRenameValues(CRUDNameSingular, CRUDNamePlural);

            List<string> optionsItems = OptionItems.Any() ? OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
            IsDtoGenerated = crudService.GenerateFiles(DtoEntity, ZipFeatureTypeList, DtoDisplayItemSelected, optionsItems, crudParent, FeatureNameSelected, Domain, BiaFront);

            UpdateCrudGenerationHistory();

            consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' generation.", "Blue");
        }
        #endregion

        #region Button
        public bool IsOptionItemEnable
        {
            get
            {
                return isDtoParsed && UseFileGenerator || (!string.IsNullOrEmpty(featureNameSelected) && !ZipFeatureTypeList.Any(x => x.Feature == featureNameSelected && x.FeatureType == FeatureType.Option));
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed
                    && !string.IsNullOrWhiteSpace(CRUDNameSingular)
                    && !string.IsNullOrWhiteSpace(CRUDNamePlural)
                    && !string.IsNullOrEmpty(Domain)
                    && (!string.IsNullOrWhiteSpace(dtoDisplayItemSelected) || ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.FeatureType == FeatureType.Option))
                    && ((IsWebApiSelected && !IsFrontSelected) || (IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)) || (!IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)))
                    && !string.IsNullOrEmpty(featureNameSelected)
                    && (!HasParent || (HasParent && !string.IsNullOrEmpty(ParentName) && !string.IsNullOrEmpty(parentNamePlural)))
                    && (!IsTeam || (IsTeam && !UseFileGenerator) || (UseFileGenerator && IsTeam && TeamRoleId > 0 && TeamTypeId > 0))
                    && !string.IsNullOrWhiteSpace(SelectedBaseKeyType);
            }
        }
        #endregion

        #region Team
        private string _ancestorTeam;

        public string AncestorTeam
        {
            get { return _ancestorTeam; }
            set 
            { 
                _ancestorTeam = value;
                RaisePropertyChanged(nameof(AncestorTeam));
            }
        }

        private int _teamTypeId;

        public int TeamTypeId
        {
            get { return _teamTypeId; }
            set 
            { 
                _teamTypeId = value; 
                RaisePropertyChanged(nameof(TeamTypeId));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private int _teamRoleId;

        public int TeamRoleId
        {
            get { return _teamRoleId; }
            set
            {
                _teamRoleId = value;
                RaisePropertyChanged(nameof(TeamRoleId));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        #endregion

        #region FormReadOnly
        private List<string> _formReadOnlyModes;

        public List<string> FormReadOnlyModes
        {
            get
            {
                _formReadOnlyModes ??= new List<string>(Enum.GetNames<FormReadOnlyMode>());
                return _formReadOnlyModes;
            }
        }

        private string _selectedFormReadOnlyMode;

        public string SelectedFormReadOnlyMode
        {
            get { return _selectedFormReadOnlyMode; }
            set 
            { 
                _selectedFormReadOnlyMode = value; 
                RaisePropertyChanged(nameof(SelectedFormReadOnlyMode));
            }
        }
        #endregion
    }

    public class OptionItem : ObservableObject
    {
        protected void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        private bool check;
        public bool Check
        {
            get => check;
            set
            {
                check = value;
                RaisePropertyChanged(nameof(Check));
            }
        }

        public string OptionName { get; set; }

        public OptionItem(string name, bool check = false)
        {
            this.Check = check;
            this.OptionName = name;
        }
    }
}
