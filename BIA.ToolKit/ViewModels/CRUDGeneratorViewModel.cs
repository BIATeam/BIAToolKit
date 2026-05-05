namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Models.CrudGenerator;
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public partial class CRUDGeneratorViewModel : ObservableObject, IDisposable
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private readonly CSharpParserService parserService;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly IDialogService dialogService;
        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;
        private readonly List<FeatureGenerationSettings> backSettingsList = [];
        private List<FeatureGenerationSettings> frontSettingsList = [];

        /// <summary>
        /// Shared app session (DEV mode flag). Exposed so XAML can bind via
        /// AppSession.IsDeveloperMode.
        /// </summary>
        public BIA.ToolKit.Helper.AppSessionService AppSession { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel(CSharpParserService parserService, ZipParserService zipService, GenerateCrudService crudService,
            SettingsService settingsService, IConsoleWriter consoleWriter,
            FileGeneratorService fileGeneratorService, IDialogService dialogService,
            BIA.ToolKit.Helper.AppSessionService appSession)
        {
            this.parserService = parserService;
            this.zipService = zipService;
            this.crudService = crudService;
            this.settings = new CRUDSettings(settingsService);
            this.consoleWriter = consoleWriter;
            this.fileGeneratorService = fileGeneratorService;
            this.dialogService = dialogService;
            AppSession = appSession;

            OptionItems = [];
            ZipFeatureTypeList = [];
            FeatureNames = [];
            DtoEntities = [];

            WeakReferenceMessenger.Default.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            WeakReferenceMessenger.Default.Register<SolutionClassesParsedMessage>(this, (r, m) => OnSolutionClassesParsed());

            PropertyChanged += OnAnyPropertyChanged;
        }

        private void OnAnyPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Fan-out property changes to computed summary/preview/badge properties without
            // having to modify every existing partial OnXxxChanged method.
            switch (e.PropertyName)
            {
                case nameof(IsVersioned):
                case nameof(IsArchivable):
                case nameof(IsFixable):
                case nameof(DisplayHistorical):
                    OnPropertyChanged(nameof(LifecycleActiveCount));
                    OnPropertyChanged(nameof(HasLifecycleActive));
                    OnPropertyChanged(nameof(GenerationSummary));
                    break;
                case nameof(IsTeam):
                case nameof(HasFormReadOnlyMode):
                    OnPropertyChanged(nameof(AccessActiveCount));
                    OnPropertyChanged(nameof(HasAccessActive));
                    OnPropertyChanged(nameof(GenerationSummary));
                    break;
                case nameof(AncestorTeam):
                    OnPropertyChanged(nameof(TeamRolesPreview));
                    OnPropertyChanged(nameof(HasTeamRolesPreview));
                    break;
                case nameof(HasParent):
                case nameof(HasFixableParent):
                    OnPropertyChanged(nameof(RelationsActiveCount));
                    OnPropertyChanged(nameof(HasRelationsActive));
                    OnPropertyChanged(nameof(GenerationSummary));
                    OnPropertyChanged(nameof(ParentRolesPreview));
                    OnPropertyChanged(nameof(HasParentRolesPreview));
                    break;
                case nameof(ParentName):
                    OnPropertyChanged(nameof(ParentRolesPreview));
                    OnPropertyChanged(nameof(HasParentRolesPreview));
                    break;
                case nameof(UseImport):
                case nameof(UseHubClient):
                case nameof(UseDomainUrl):
                    OnPropertyChanged(nameof(IntegrationsActiveCount));
                    OnPropertyChanged(nameof(HasIntegrationsActive));
                    OnPropertyChanged(nameof(GenerationSummary));
                    break;
                case nameof(UseAdvancedFilter):
                case nameof(HasCustomRepository):
                    OnPropertyChanged(nameof(BehaviorActiveCount));
                    OnPropertyChanged(nameof(HasBehaviorActive));
                    OnPropertyChanged(nameof(GenerationSummary));
                    break;
                case nameof(CRUDNameSingular):
                case nameof(IsWebApiSelected):
                case nameof(IsFrontSelected):
                case nameof(SelectedBaseKeyType):
                    OnPropertyChanged(nameof(GenerationSummary));
                    break;
                case nameof(CRUDNamePlural):
                    OnPropertyChanged(nameof(IsPluralAutoDerived));
                    break;
            }

            // Whenever the button-enable flag flips, refresh tooltip + the
            // blocked-state visibility flag so the rich tooltip switches
            // between its "ready" and "cannot generate yet" panels.
            if (e.PropertyName == nameof(IsButtonGenerateCrudEnable))
            {
                OnPropertyChanged(nameof(GenerateButtonTooltip));
                OnPropertyChanged(nameof(HasGenerationBlockingReasons));
            }
        }

        #region CurrentProject

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsProjectCompatibleV6))]
        [NotifyPropertyChangedFor(nameof(IsFrontAvailable))]
        private Project currentProject;

        partial void OnCurrentProjectChanged(Project value)
        {
            BiaFronts.Clear();
            if (value != null)
            {
                foreach (var biaFront in value.BIAFronts)
                {
                    BiaFronts.Add(biaFront);
                }
                BiaFront = BiaFronts.FirstOrDefault();
            }
        }

        [ObservableProperty]
        private bool isProjectChosen;

        [ObservableProperty]
        private bool useFileGenerator;
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
                    OnDtoEntitySelected();
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<EntityInfo> dtoEntities;

        [ObservableProperty]
        private List<string> dtoDisplayItems;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        [NotifyPropertyChangedFor(nameof(IsOptionItemEnable))]
        private bool isDtoParsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string dtoDisplayItemSelected;

        [ObservableProperty]
        private ObservableCollection<OptionItem> optionItems = [];

        public static IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string selectedBaseKeyType;

        public string SelectedOptionItems => string.Join(", ", OptionItems.Where(x => x.Check).Select(x => x.OptionName));

        [ObservableProperty]
        private bool isDtoGenerated;

        public void AddOptionItems(IEnumerable<OptionItem> optionItems)
        {
            foreach (var oldItem in OptionItems)
            {
                oldItem.PropertyChanged -= OptionItem_PropertyChanged;
            }
            OptionItems.Clear();
            foreach (var optionItem in optionItems.OrderBy(x => x.OptionName))
            {
                OptionItems.Add(optionItem);
            }
            foreach (var optionItem in OptionItems)
            {
                optionItem.PropertyChanged += OptionItem_PropertyChanged;
            }
        }

        private void OptionItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SelectedOptionItems));
        }
        #endregion

        #region Feature

        [ObservableProperty]
        private ObservableCollection<string> featureNames;

        private string featureNameSelected;
        public string FeatureNameSelected
        {
            get => featureNameSelected;
            set
            {
                if (featureNameSelected != value)
                {
                    featureNameSelected = value;
                    OnPropertyChanged(nameof(FeatureNameSelected));
                    OnPropertyChanged(nameof(IsOptionItemEnable));
                    OnPropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    OnPropertyChanged(nameof(IsWebApiAvailable));
                    OnPropertyChanged(nameof(IsFrontAvailable));
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
                if (zipFeatureType.GenerationType == GenerationType.WebApi && IsWebApiSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
                if (zipFeatureType.GenerationType == GenerationType.Front && IsFrontSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
            }
        }

        #endregion

        #region CRUD Name

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string cRUDNameSingular;

        partial void OnCRUDNameSingularChanged(string value)
        {
            CRUDNamePlural = value.Pluralize();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string cRUDNamePlural;
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool hasParent;

        partial void OnHasParentChanged(bool value)
        {
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string domain;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string parentName;

        partial void OnParentNameChanged(string value)
        {
            ParentNamePlural = value?.Pluralize();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string parentNamePlural;

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

            OnPropertyChanged(nameof(IsCheckboxParentEnable));
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
                    OnPropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool isWebApiSelected;

        partial void OnIsWebApiSelectedChanged(bool value) => UpdateFeatureSelection();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool isFrontSelected;

        partial void OnIsFrontSelectedChanged(bool value)
        {
            if (value == false)
            {
                BiaFront = null;
            }
            else if (string.IsNullOrWhiteSpace(BiaFront) && BiaFronts.Count > 0)
            {
                BiaFront = BiaFronts.FirstOrDefault();
            }
            UpdateFeatureSelection();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string biaFront;

        partial void OnBiaFrontChanged(string value) => OnBiaFrontChanged();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBIAFrontSelectorVisible))]
        private ObservableCollection<string> biaFronts = [];

        public bool IsWebApiAvailable => UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.WebApi));
        public bool IsFrontAvailable => (UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.Front))) && (CurrentProject != null && CurrentProject.BIAFronts.Count > 0);

        /// <summary>
        /// Show the BIA Front version selector only when the project has more than one front.
        /// With a single front it auto-selects and adds noise.
        /// </summary>
        public bool IsBIAFrontSelectorVisible => BiaFronts?.Count > 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCheckBoxIsTeamEnable))]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool isTeam;

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

        [ObservableProperty]
        private bool useHubClient;

        [ObservableProperty]
        private bool hasCustomRepository;

        [ObservableProperty]
        private bool hasFormReadOnlyMode;

        partial void OnHasFormReadOnlyModeChanged(bool value)
        {
            SelectedFormReadOnlyMode = value ? FormReadOnlyModes.First() : string.Empty;
        }

        [ObservableProperty]
        private bool useImport;

        [ObservableProperty]
        private bool isFixable;

        [ObservableProperty]
        private bool hasFixableParent;

        [ObservableProperty]
        private bool isVersioned;

        [ObservableProperty]
        private bool isArchivable;

        [ObservableProperty]
        private bool useAdvancedFilter;

        public bool IsProjectCompatibleV6 => Version.TryParse(CurrentProject?.FrameworkVersion, out var version) && version.Major >= 6;

        [ObservableProperty]
        private bool displayHistorical;

        [ObservableProperty]
        private bool useDomainUrl;

        // --- "Detected from DTO" snapshot flags ---
        // Captured at DTO parse time from EntityInfo. Independent of the
        // user-toggleable IsVersioned/IsArchivable/... flags (the user can
        // turn a feature off, but the badge should still reflect that we
        // detected it in the DTO heritage).

        [ObservableProperty]
        private bool isDtoDetectedVersioned;

        [ObservableProperty]
        private bool isDtoDetectedArchivable;

        [ObservableProperty]
        private bool isDtoDetectedFixable;

        [ObservableProperty]
        private bool isDtoDetectedTeam;

        [ObservableProperty]
        private bool isDtoDetectedAncestorTeam;

        #endregion

        #region ZipFile
        public List<ZipFeatureType> ZipFeatureTypeList { get; set; }
        #endregion

        #region Button
        public bool IsOptionItemEnable
        {
            get
            {
                return IsDtoParsed && UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && !ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.FeatureType == FeatureType.Option));
            }
        }

        public bool IsButtonGenerateCrudEnable => GenerationBlockingReasons.Count == 0;

        // Lists all the required-input failures preventing generation. Surfaced
        // as a tooltip on the disabled Generate button so the user sees what
        // to fix without reverse-engineering the boolean chain.
        public List<string> GenerationBlockingReasons
        {
            get
            {
                var reasons = new List<string>();

                if (!IsDtoParsed)
                    reasons.Add("Select a DTO Entity File.");
                if (string.IsNullOrEmpty(FeatureNameSelected))
                    reasons.Add("Choose a Feature to generate.");
                if (string.IsNullOrWhiteSpace(SelectedBaseKeyType))
                    reasons.Add("Choose a Base Key Type.");
                if (string.IsNullOrEmpty(Domain))
                    reasons.Add("Set the Domain.");
                if (string.IsNullOrWhiteSpace(CRUDNameSingular))
                    reasons.Add("Set the Name (singular).");
                if (string.IsNullOrWhiteSpace(CRUDNamePlural))
                    reasons.Add("Set the Plural.");
                if (string.IsNullOrWhiteSpace(DtoDisplayItemSelected)
                    && !ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.FeatureType == FeatureType.Option))
                    reasons.Add("Select a Display property (the DTO must expose at least one string property).");
                if (!IsWebApiSelected && !IsFrontSelected)
                    reasons.Add("Tick at least one Generation Target (Web API or Angular Front).");
                else if (IsFrontSelected && string.IsNullOrWhiteSpace(BiaFront))
                    reasons.Add("Pick a BIA Front when Angular Front is targeted.");
                if (HasParent && (string.IsNullOrEmpty(ParentName) || string.IsNullOrEmpty(ParentNamePlural)))
                    reasons.Add("Set the Parent name and plural (Parent entity is enabled).");
                if (IsTeam && UseFileGenerator && (TeamRoleId <= 0 || TeamTypeId <= 0))
                    reasons.Add("Set Team Type ID and Team Role Type ID > 0 (Team is enabled).");

                return reasons;
            }
        }

        public bool HasGenerationBlockingReasons => GenerationBlockingReasons.Count > 0;

        public string GenerateButtonTooltip =>
            GenerationBlockingReasons.Count == 0
                ? "Generate the selected feature for this entity."
                : "Cannot generate yet — fix the following:\n• " + string.Join("\n• ", GenerationBlockingReasons);
        #endregion

        #region Team

        [ObservableProperty]
        private string ancestorTeam;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private int teamTypeId;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private int teamRoleId;

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

        [ObservableProperty]
        private string selectedFormReadOnlyMode;
        #endregion

        #region Commands
        [RelayCommand]
        private void Generate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
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
                    }, ct);

                    UpdateCrudGenerationHistory();
                    WeakReferenceMessenger.Default.Send(new EntityGenerationCompletedMessage());
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

                // Generation DotNet + Angular files
                List<string> optionsItems = OptionItems.Any() ? OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
                IsDtoGenerated = crudService.GenerateFiles(DtoEntity, ZipFeatureTypeList, DtoDisplayItemSelected, optionsItems, crudParent, FeatureNameSelected, Domain, BiaFront);

                // Generate generation history file
                UpdateCrudGenerationHistory();

                consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' generation.", "Blue");

                WeakReferenceMessenger.Default.Send(new EntityGenerationCompletedMessage());
            }));
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            try
            {
                // Get last generation history
                CRUDGenerationHistory history = crudHistory?.CRUDGenerationHistory?.FirstOrDefault(h => h.Mapping.Dto == GetDtoSelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{CRUDNameSingular}' generation found.", "Orange");
                    return;
                }

                var confirmMessage = $"Reverse the last generation for '{CRUDNameSingular}'?\n\n"
                    + "This will:\n"
                    + "  • delete every file that was produced by the last Generate for this DTO,\n"
                    + "  • remove the BIAToolkit-inserted blocks from shared files (routes, DI, permissions…),\n"
                    + "  • also reverse linked CRUD options if any.\n\n"
                    + "Warning: this is not a git revert. Any manual edit you made to a generated file "
                    + "will be lost, and there is no way to restore it from this tool.\n\n"
                    + "Continue?";

                if (!dialogService.Confirm(confirmMessage))
                    return;

                // Get generation histories used by options
                List<CRUDGenerationHistory> historyOptions = crudHistory?.CRUDGenerationHistory?.Where(h => h.OptionItems.Contains(CRUDNameSingular)).ToList();

                // Delete last generation
                crudService.DeleteLastGeneration(ZipFeatureTypeList, CurrentProject, history, FeatureNameSelected, new CrudParent { Exists = history.HasParent, Domain = history.Domain, Name = history.ParentName, NamePlural = history.ParentNamePlural }, historyOptions);

                // Update history
                DeleteLastGenerationHistory(history);
                IsDtoGenerated = false;

                consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{CRUDNameSingular}' generation: {ex.Message}", "Red");
            }
        }

        [RelayCommand]
        private async Task DeleteBIAToolkitAnnotations()
        {
            try
            {
                var message = "Do you want to permanently remove all BIAToolkit annotations in code?\n"
                    + "After that you will no longer be able to regenerate old CRUDs.\n\n"
                    + "Be careful, this action is irreversible.";

                if (!dialogService.Confirm(message))
                    return;

                List<string> folders = [
                    Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                    Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
                ];

                await GenerateCrudService.DeleteBIAToolkitAnnotations(folders);

                consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on cleaning annotations for project '{CurrentProject.Name}': {ex.Message}", "Red");
            }
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Called when the current project changes.
        /// </summary>
        public void SetCurrentProject(Project project)
        {
            if (project == CurrentProject)
                return;

            CurrentProjectChange(project);
        }

        /// <summary>
        /// Called when solution classes have been parsed.
        /// </summary>
        public void OnSolutionClassesParsed()
        {
            InitProjectTask(CurrentProject);
            if (CurrentProject is not null)
            {
                ListDtoFiles();
            }
        }
        #endregion

        #region Private methods
        private void CurrentProjectChange(Project project)
        {
            if (project is null)
                return;

            InitProjectTask(project);
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

            CurrentProject = null;
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
            string dotnetBiaFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(project.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            crudHistoryFileName = Path.Combine(project.Folder, Constants.FolderBia, settings.CrudGenerationHistoryFileName);

            var oldCrudHistoryFilePath = Path.Combine(project.Folder, settings.CrudGenerationHistoryFileName);
            if (File.Exists(oldCrudHistoryFilePath))
            {
                File.Move(oldCrudHistoryFilePath, crudHistoryFileName);
            }

            CurrentProject = project;
            IsProjectChosen = true;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                UseFileGenerator = true;
                FeatureNames.Add("CRUD");
                FeatureNameSelected = "CRUD";
                crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
            }
            else
            {
                UseFileGenerator = false;

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
                    ZipFeatureTypeList.Add(zipFeatureType);
                }

                foreach (var featureName in ZipFeatureTypeList.Select(x => x.Feature).Distinct())
                {
                    FeatureNames.Add(featureName);
                }

                crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
            }

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);

            IsTeam = IsTeam;
        }

        private void SetFrontGenerationSettings(string biaFront)
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
                if (CurrentProject.FrameworkVersion == "3.9.0")
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
                ZipFeatureTypeList.Add(zipFeatureType);
            }
        }

        private void OnDtoEntitySelected()
        {
            IsDtoParsed = false;
            DtoDisplayItems = null;

            if (CurrentProject is null)
                return;

            IsDtoParsed = ParseDtoFile();
            CRUDNameSingular = GetEntityNameFromDto(DtoEntity?.Name);
            IsTeam = DtoEntity?.IsTeam == true;
            IsVersioned = DtoEntity?.IsVersioned == true;
            IsFixable = DtoEntity?.IsFixable == true;
            IsArchivable = DtoEntity?.IsArchivable == true;
            AncestorTeam = DtoEntity?.AncestorTeamName;
            SelectedBaseKeyType = DtoEntity?.BaseKeyType;

            // Snapshot detection flags so the "detected from DTO" badges
            // stay accurate even if the user toggles the feature off.
            IsDtoDetectedTeam = DtoEntity?.IsTeam == true;
            IsDtoDetectedVersioned = DtoEntity?.IsVersioned == true;
            IsDtoDetectedFixable = DtoEntity?.IsFixable == true;
            IsDtoDetectedArchivable = DtoEntity?.IsArchivable == true;
            IsDtoDetectedAncestorTeam = DtoEntity?.HasAncestorTeam == true;
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

            if (crudHistory != null)
            {
                string dtoName = GetDtoSelectedPath();
                if (!string.IsNullOrEmpty(dtoName))
                {
                    CRUDGenerationHistory history = crudHistory.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping.Dto == dtoName);

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
                        BiaFront = !string.IsNullOrEmpty(history.BiaFront) ? history.BiaFront : BiaFronts.FirstOrDefault();
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
                        if (history.OptionItems != null)
                        {
                            foreach (var option in OptionItems)
                            {
                                option.Check = false;
                            }

                            history.OptionItems.ForEach(o =>
                            {
                                OptionItem item = OptionItems.FirstOrDefault(x => x.OptionName == o);
                                if (item != null) item.Check = item != null;
                            });
                        }

                        isBackSelected = history.Generation.Any(g => g.GenerationType == GenerationType.WebApi.ToString());
                        isFrontSelected = history.Generation.Any(g => g.GenerationType == GenerationType.Front.ToString());
                        IsDtoGenerated = true;
                    }
                    else
                    {
                        IsDtoGenerated = false;
                    }
                }

                // Get generated options
                List<CRUDGenerationHistory> histories = crudHistory.CRUDGenerationHistory.Where(h =>
                    (h.Mapping.Dto != GetDtoSelectedPath()) &&
                    h.Generation.Any(g => g.FeatureType == FeatureType.Option.ToString())).ToList();
            }
            else
            {
                IsDtoGenerated = false;
            }

            IsWebApiSelected = isBackSelected;
            IsFrontSelected = isFrontSelected;
        }

        private void OnBiaFrontChanged()
        {
            if (!string.IsNullOrEmpty(BiaFront))
            {
                SetFrontGenerationSettings(BiaFront);
                ParseFrontDomains();
            }
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
                    Mapping = new()
                    {
                        Dto = GetDtoSelectedPath(),
                        Type = DOTNET_TYPE,
                    }
                };

                if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                {
                    if (IsWebApiSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.WebApi.ToString(),
                            Type = DOTNET_TYPE,
                            Folder = Constants.FolderDotNet
                        });
                    }
                    if (IsFrontSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.Front.ToString(),
                            Type = ANGULAR_TYPE,
                            Folder = BiaFront
                        });
                    }
                }
                else
                {
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
                }

                CRUDGenerationHistory genFound = crudHistory.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    crudHistory.CRUDGenerationHistory.Remove(genFound);
                }

                crudHistory.CRUDGenerationHistory.Add(history);
                CommonTools.SerializeToJsonFile<CRUDGeneration>(crudHistory, crudHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        private void DeleteLastGenerationHistory(CRUDGenerationHistory history)
        {
            crudHistory?.CRUDGenerationHistory?.Remove(history);

            foreach (CRUDGenerationHistory optionHistory in crudHistory?.CRUDGenerationHistory)
            {
                optionHistory.OptionItems.Remove(history.EntityNameSingular);
            }

            CommonTools.SerializeToJsonFile<CRUDGeneration>(crudHistory, crudHistoryFileName);
        }

        private void ListDtoFiles()
        {
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
                foreach (var property in DtoEntity.Properties.OrderBy(x => x.Name))
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

        private void ParseFrontDomains()
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            List<string> foldersName = [];

            string folderPath = Path.Combine(CurrentProject.Folder, BiaFront, domainsPath);
            if (!Directory.Exists(folderPath))
                return;

            List<string> folders = [.. Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories)];
            folders.ForEach(f => foldersName.Add(new DirectoryInfo(f).Name.Replace(suffix, "")));

            AddOptionItems(foldersName.Select(x => new OptionItem(CommonTools.ConvertKebabToPascalCase(x))));
        }

        private static string GetEntityNameFromDto(string dtoFileName)
        {
            if (string.IsNullOrWhiteSpace(dtoFileName))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];
            }

            return fileName;
        }

        private string GetDtoSelectedPath()
        {
            if (DtoEntity is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return DtoEntity.Path.Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }
        #endregion

        #region Pedagogy helpers (summary, previews, badge counts)

        public bool IsPluralAutoDerived =>
            !string.IsNullOrWhiteSpace(CRUDNameSingular)
            && !string.IsNullOrWhiteSpace(CRUDNamePlural)
            && string.Equals(CRUDNamePlural, CRUDNameSingular.Pluralize(), StringComparison.Ordinal);

        public int LifecycleActiveCount =>
            (IsVersioned ? 1 : 0) + (IsArchivable ? 1 : 0) + (IsFixable ? 1 : 0) + (DisplayHistorical ? 1 : 0);

        public bool HasLifecycleActive => LifecycleActiveCount > 0;

        public int AccessActiveCount =>
            (IsTeam ? 1 : 0) + (HasFormReadOnlyMode ? 1 : 0);

        public bool HasAccessActive => AccessActiveCount > 0;

        public int RelationsActiveCount => HasParent ? 1 : 0;

        public bool HasRelationsActive => RelationsActiveCount > 0;

        public int IntegrationsActiveCount =>
            (UseImport ? 1 : 0) + (UseHubClient ? 1 : 0) + (UseDomainUrl ? 1 : 0);

        public bool HasIntegrationsActive => IntegrationsActiveCount > 0;

        public int BehaviorActiveCount =>
            (UseAdvancedFilter ? 1 : 0) + (HasCustomRepository ? 1 : 0);

        public bool HasBehaviorActive => BehaviorActiveCount > 0;

        public string TeamRolesPreview =>
            string.IsNullOrWhiteSpace(AncestorTeam)
                ? null
                : $"{AncestorTeam}_Admin · {AncestorTeam}_Member";

        public bool HasTeamRolesPreview => !string.IsNullOrWhiteSpace(AncestorTeam);

        public string ParentRolesPreview =>
            HasParent && !string.IsNullOrWhiteSpace(ParentName)
                ? $"{ParentName}_Admin · {ParentName}_Member"
                : null;

        public bool HasParentRolesPreview => HasParent && !string.IsNullOrWhiteSpace(ParentName);

        public string GenerationSummary
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(CRUDNameSingular) ? "—" : CRUDNameSingular;
                string target;
                if (IsWebApiSelected && IsFrontSelected) target = "Back+Front";
                else if (IsWebApiSelected) target = "Back";
                else if (IsFrontSelected) target = "Front";
                else target = "—";

                var key = string.IsNullOrWhiteSpace(SelectedBaseKeyType) ? "—" : SelectedBaseKeyType;

                var total = LifecycleActiveCount + AccessActiveCount + RelationsActiveCount
                    + IntegrationsActiveCount + BehaviorActiveCount;
                var featuresLabel = total <= 1 ? "feature" : "features";

                return $"{name} · {target} · {key} · {total} {featuresLabel}";
            }
        }

        #endregion

        public void Dispose()
        {
            PropertyChanged -= OnAnyPropertyChanged;
            foreach (var item in OptionItems)
            {
                item.PropertyChanged -= OptionItem_PropertyChanged;
            }
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }

    public partial class OptionItem : ObservableObject
    {
        [ObservableProperty]
        private bool check;

        public string OptionName { get; set; }

        public OptionItem(string name, bool check = false)
        {
            this.Check = check;
            this.OptionName = name;
        }
    }

}
