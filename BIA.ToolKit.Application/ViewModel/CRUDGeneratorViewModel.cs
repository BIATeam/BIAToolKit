namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class CRUDGeneratorViewModel : ObservableObject
    {
        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel()
        {
            OptionItems = [];
            ZipFeatureTypeList = [];
            FeatureNames = [];
            DtoEntities = [];
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

    /// <summary>
    /// Constructor.
    /// </summary>
    public class ZipFeatureType(FeatureType type, GenerationType generation, string zipName, string zipPath, string feature, List<FeatureParent> parents, bool needParent, List<FeatureAdaptPath> adaptPaths, string domain)
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The Feature type.
        /// </summary>
        public FeatureType FeatureType { get; } = type;

        /// <summary>
        /// The Generation type.
        /// </summary>
        public GenerationType GenerationType { get; } = generation;

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; } = zipName;

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; } = zipPath;

        /// <summary>
        /// Name of the feature associated to the ZIP
        /// </summary>
        public string Feature { get; } = feature;

        public List<FeatureData> FeatureDataList { get; set; }

        /// <summary>
        /// Parents of the feature
        /// </summary>
        public List<FeatureParent> Parents { get; set; } = parents;

        /// <summary>
        /// Indicates if the feature needs a parent
        /// </summary>
        public bool NeedParent { get; set; } = needParent;

        /// <summary>
        /// The adapt paths to apply
        /// </summary>
        public List<FeatureAdaptPath> AdaptPaths { get; set; } = adaptPaths;

        /// <summary>
        /// The feature domain
        /// </summary>
        public string Domain { get; set; } = domain;
    }

    public class WebApiNamespace(WebApiFileType fileType, string crudNamespace)
    {
        public WebApiFileType FileType { get; } = fileType;

        public string CrudNamespace { get; } = crudNamespace;

        public string CrudNamespaceGenerated { get; set; }
    }

    public class CrudParent
    {
        public bool Exists { get; set; }
        public string Name { get; set; }
        public string NamePlural { get; set; }
        public string Domain { get; set; }
    }
}