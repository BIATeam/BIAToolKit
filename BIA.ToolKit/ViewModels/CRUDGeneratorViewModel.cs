namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.CRUD;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
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

        private readonly ICRUDGenerationService crudGenerationService;
        private readonly GenerateCrudService crudService;
        private readonly IMessenger messenger;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly ITextParsingService textParsingService;

        private CRUDGeneration crudHistory;

        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel(
            ICRUDGenerationService crudGenerationService,
            GenerateCrudService crudService,
            IConsoleWriter consoleWriter,
            IMessenger messenger,
            FileGeneratorService fileGeneratorService,
            ITextParsingService textParsingService)
        {
            this.crudGenerationService = crudGenerationService ?? throw new ArgumentNullException(nameof(crudGenerationService));
            this.crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.textParsingService = textParsingService ?? new TextParsingService();

            messenger.Register<ProjectChangedMessage>(this, (r, m) => SetCurrentProject(m.Project));
            messenger.Register<SolutionClassesParsedMessage>(this, (r, m) => OnSolutionClassesParsed());
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
        [ObservableProperty]
        private EntityInfo dtoEntity;

        partial void OnDtoEntityChanged(EntityInfo value)
        {
            UpdateParentPreSelection();
            UpdateDomainPreSelection();
            OnDtoSelected();
        }

        [ObservableProperty]
        private ObservableCollection<EntityInfo> dtoEntities = [];

        [ObservableProperty]
        private List<string> dtoDisplayItems;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        [NotifyPropertyChangedFor(nameof(IsOptionItemEnable))]
        private bool isDtoParsed = false;

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
        private bool isDtoGenerated = false;

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
        private ObservableCollection<string> featureNames = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOptionItemEnable))]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        [NotifyPropertyChangedFor(nameof(IsWebApiAvailable))]
        [NotifyPropertyChangedFor(nameof(IsFrontAvailable))]
        private string featureNameSelected;

        partial void OnFeatureNameSelectedChanged(string value)
        {
            IsWebApiSelected = IsWebApiAvailable;
            IsFrontSelected = IsFrontAvailable;
            UpdateParentPreSelection();
            UpdateDomainPreSelection();

            if (!UseFileGenerator)
            {
                IsTeam = IsTeam || value == "Team";
            }
        }

        private void UpdateFeatureSelection()
        {
            ZipFeatureTypeList?.ForEach(x => x.IsChecked = false);
            if (string.IsNullOrEmpty(FeatureNameSelected))
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
            CRUDNamePlural = value?.Pluralize();
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
                if (UseFileGenerator)
                    return true;

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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool isWebApiSelected;

        partial void OnIsWebApiSelectedChanged(bool value)
        {
            UpdateFeatureSelection();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private bool isFrontSelected;

        partial void OnIsFrontSelectedChanged(bool value)
        {
            if (value == false)
                BiaFront = null;
            UpdateFeatureSelection();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateCrudEnable))]
        private string biaFront;

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
        [ObservableProperty]
        private List<ZipFeatureType> zipFeatureTypeList = [];
        #endregion

        #region Commands
        [RelayCommand]
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

            if (crudHistory != null)
            {
                string dtoName = GetDtoSelectedPath();
                if (!string.IsNullOrEmpty(dtoName))
                {
                    CRUDGenerationHistory history = crudGenerationService.LoadDtoHistory(crudHistory, dtoName);

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

        [RelayCommand]
        private void OnEntitySingularNameChanged()
        {
            CRUDNamePlural = string.Empty;
        }

        [RelayCommand]
        private void OnEntityPluralNameChanged()
        {
            // Trigger property changed for button enable
            OnPropertyChanged(nameof(IsButtonGenerateCrudEnable));
        }

        [RelayCommand]
        private void OnBiaFrontSelected(string selectedFront)
        {
            if (string.IsNullOrWhiteSpace(selectedFront))
                return;

            SetFrontGenerationSettings(selectedFront);
            ParseFrontDomains();
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            try
            {
                CRUDGenerationHistory history = crudGenerationService.LoadDtoHistory(crudHistory, GetDtoSelectedPath());
                if (history == null)
                {
                    consoleWriter.AddMessageLine($"No previous '{CRUDNameSingular}' generation found.", "Orange");
                    return;
                }

                var request = new CRUDDeletionRequest
                {
                    History = history,
                    FeatureName = FeatureNameSelected,
                    ParentDomain = history.Domain,
                    ParentName = history.ParentName,
                    ParentNamePlural = history.ParentNamePlural,
                    HasParent = history.HasParent,
                    ZipFeatureTypeList = ZipFeatureTypeList
                };

                crudGenerationService.DeleteLastGeneration(request);
                IsDtoGenerated = false;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{CRUDNameSingular}' generation: {ex.Message}", "Red");
            }
        }

        [RelayCommand]
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
                    List<string> folders =
                    [
                        Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                        Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
                    ];

                    await crudGenerationService.DeleteAnnotationsAsync(folders);
                }
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
            OptionItems?.Clear();
            ZipFeatureTypeList?.Clear();
            FeatureNames?.Clear();

            DtoEntity = null;
            DtoEntities?.Clear();
            IsWebApiSelected = false;
            IsFrontSelected = false;
            FeatureNameSelected = null;
            BiaFronts?.Clear();
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

        private async void SetGenerationSettings(Project project)
        {
            var initResult = await crudGenerationService.InitializeAsync(project);

            ZipFeatureTypeList = initResult.ZipFeatureTypeList;

            FeatureNames.Clear();
            foreach (var featureName in initResult.FeatureNames)
            {
                FeatureNames.Add(featureName);
            }

            if (initResult.UseFileGenerator)
            {
                FeatureNameSelected = "CRUD";
            }

            crudHistory = initResult.History;
            UseFileGenerator = initResult.UseFileGenerator;
            CurrentProject = project;
            IsProjectChosen = true;

            // Repopulate BiaFronts since ClearAll() may have cleared them and
            // OnCurrentProjectChanged won't fire if CurrentProject hasn't changed.
            BiaFronts.Clear();
            if (project != null)
            {
                foreach (var biaFront in project.BIAFronts)
                {
                    BiaFronts.Add(biaFront);
                }
                if (BiaFront == null || !BiaFronts.Contains(BiaFront))
                {
                    BiaFront = BiaFronts.FirstOrDefault();
                }
            }
        }

        private void SetFrontGenerationSettings(string biaFront)
        {
            crudGenerationService.LoadFrontSettings(biaFront);
        }

        private void UpdateCrudGenerationHistory()
        {
            try
            {
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

                var feature = ZipFeatureTypeList?.Where(x => x.Feature == FeatureNameSelected);

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

                crudGenerationService.UpdateHistory(history);
                IsDtoGenerated = true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        [RelayCommand]
        private void ListDtoFiles()
        {
            if (CurrentProject is null)
                return;

            var entities = crudGenerationService.ListDtoFiles(CurrentProject);
            DtoEntities = new(entities);
        }

        private bool ParseDtoFile()
        {
            var result = crudGenerationService.ParseDtoFile(DtoEntity);
            if (!result.Success)
                return false;

            DtoDisplayItems = result.DisplayItems;
            DtoDisplayItemSelected = result.DefaultDisplayItem;
            return true;
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
            var domains = crudGenerationService.ParseFrontDomains(BiaFront);
            AddOptionItems(domains.Select(x => new OptionItem(x)));
        }

        [RelayCommand]
        private async Task GenerateCrudAsync()
        {
            if (CurrentProject is null)
                return;

            var request = new CRUDGenerationRequest
            {
                DtoEntity = DtoEntity,
                CRUDNameSingular = CRUDNameSingular,
                CRUDNamePlural = CRUDNamePlural,
                DisplayItem = DtoDisplayItemSelected,
                Domain = Domain,
                FeatureName = FeatureNameSelected,
                HasParent = HasParent,
                ParentName = ParentName,
                ParentNamePlural = ParentNamePlural,
                BiaFront = BiaFront,
                IsWebApiSelected = IsWebApiSelected,
                IsFrontSelected = IsFrontSelected,
                IsTeam = IsTeam,
                TeamTypeId = TeamTypeId,
                TeamRoleId = TeamRoleId,
                UseHubClient = UseHubClient,
                HasCustomRepository = HasCustomRepository,
                HasFormReadOnlyMode = HasFormReadOnlyMode,
                FormReadOnlyMode = SelectedFormReadOnlyMode,
                UseImport = UseImport,
                IsFixable = IsFixable,
                HasFixableParent = HasFixableParent,
                IsVersioned = IsVersioned,
                IsArchivable = IsArchivable,
                UseAdvancedFilter = UseAdvancedFilter,
                AncestorTeam = AncestorTeam,
                BaseKeyType = SelectedBaseKeyType,
                DisplayHistorical = DisplayHistorical,
                UseDomainUrl = UseDomainUrl,
                SelectedOptions = OptionItems?.Where(o => o.Check).Select(o => o.OptionName).ToList() ?? [],
                ZipFeatureTypeList = ZipFeatureTypeList
            };

            IsDtoGenerated = await crudGenerationService.GenerateAsync(request);
            if (IsDtoGenerated)
            {
                UpdateCrudGenerationHistory();
            }
        }
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
        public List<string> FormReadOnlyModes { get; } = new(Enum.GetNames<FormReadOnlyMode>());

        [ObservableProperty]
        private string selectedFormReadOnlyMode;
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
