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

    public partial class CRUDGeneratorViewModel : ObservableObject
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
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel(CSharpParserService parserService, ZipParserService zipService, GenerateCrudService crudService,
            SettingsService settingsService, IConsoleWriter consoleWriter,
            FileGeneratorService fileGeneratorService, IDialogService dialogService)
        {
            this.parserService = parserService;
            this.zipService = zipService;
            this.crudService = crudService;
            this.settings = new CRUDSettings(settingsService);
            this.consoleWriter = consoleWriter;
            this.fileGeneratorService = fileGeneratorService;
            this.dialogService = dialogService;

            OptionItems = [];
            ZipFeatureTypeList = [];
            FeatureNames = [];
            DtoEntities = [];

            WeakReferenceMessenger.Default.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            WeakReferenceMessenger.Default.Register<SolutionClassesParsedMessage>(this, (r, m) => OnSolutionClassesParsed());
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
        private ObservableCollection<string> biaFronts = [];

        public bool IsWebApiAvailable => UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.WebApi));
        public bool IsFrontAvailable => (UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.Front))) && (CurrentProject != null && CurrentProject.BIAFronts.Count > 0);

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

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed
                    && !string.IsNullOrWhiteSpace(CRUDNameSingular)
                    && !string.IsNullOrWhiteSpace(CRUDNamePlural)
                    && !string.IsNullOrEmpty(Domain)
                    && (!string.IsNullOrWhiteSpace(DtoDisplayItemSelected) || ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.FeatureType == FeatureType.Option))
                    && ((IsWebApiSelected && !IsFrontSelected) || (IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)) || (!IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)))
                    && !string.IsNullOrEmpty(FeatureNameSelected)
                    && (!HasParent || (HasParent && !string.IsNullOrEmpty(ParentName) && !string.IsNullOrEmpty(ParentNamePlural)))
                    && (!IsTeam || (IsTeam && !UseFileGenerator) || (UseFileGenerator && IsTeam && TeamRoleId > 0 && TeamTypeId > 0))
                    && !string.IsNullOrWhiteSpace(SelectedBaseKeyType);
            }
        }
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

                // Generation DotNet + Angular files
                List<string> optionsItems = OptionItems.Any() ? OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
                IsDtoGenerated = crudService.GenerateFiles(DtoEntity, ZipFeatureTypeList, DtoDisplayItemSelected, optionsItems, crudParent, FeatureNameSelected, Domain, BiaFront);

                // Generate generation history file
                UpdateCrudGenerationHistory();

                consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' generation.", "Blue");
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
