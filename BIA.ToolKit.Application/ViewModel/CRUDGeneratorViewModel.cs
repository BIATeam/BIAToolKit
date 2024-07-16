namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
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
            OptionItems = new();
            ZipFeatureTypeList = new();
            ZipDotNetCollection = new();
            ZipAngularCollection = new();
        }

        #region CurrentProject
        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
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
                }
            }
        }

        private Dictionary<string, string> dtoFiles;
        public Dictionary<string, string> DtoFiles
        {
            get => dtoFiles;
            set
            {
                if (dtoFiles != value)
                {
                    dtoFiles = value;
                    RaisePropertyChanged(nameof(DtoFiles));
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

        private string dtoSelected;
        public string DtoSelected
        {
            get => dtoSelected;
            set
            {
                if (dtoSelected != value)
                {
                    dtoSelected = value;
                    RaisePropertyChanged(nameof(IsButtonParseDtoEnable));
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

        private ObservableCollection<OptionItem> optionItems;
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

        private List<string> optionItemsSelected;
        public List<string> OptionItemsSelected
        {
            get => optionItemsSelected;
            set
            {
                if (optionItemsSelected != value)
                {
                    optionItemsSelected = value;
                    RaisePropertyChanged(nameof(OptionItemsSelected));
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
                    RaisePropertyChanged(nameof(IsButtonParseZipEnable));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
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

        #region CheckBox
        private bool isSelectionChange = false;
        public bool IsSelectionChange
        {
            get => isSelectionChange;
            set
            {
                if (isSelectionChange != value)
                {
                    RaisePropertyChanged(nameof(IsButtonParseZipEnable));
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
                    UpdateFeatureSelection();
                }
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
                    UpdateFeatureSelection();
                }
            }
        }

        private bool isCrudSelected;
        public bool IsCrudSelected
        {
            get => isCrudSelected;
            set
            {
                if (isCrudSelected != value)
                {
                    isCrudSelected = value;
                    RaisePropertyChanged(nameof(IsCrudSelected));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                    UpdateFeatureSelection();
                }
            }
        }

        private bool isOptionSelected;
        public bool IsOptionSelected
        {
            get => isOptionSelected;
            set
            {
                if (isOptionSelected != value)
                {
                    isOptionSelected = value;
                    RaisePropertyChanged(nameof(IsOptionSelected));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                    UpdateFeatureSelection();
                }
            }
        }

        private bool isTeamSelected;
        public bool IsTeamSelected
        {
            get => isTeamSelected;
            set
            {
                if (isTeamSelected != value)
                {
                    isTeamSelected = value;
                    RaisePropertyChanged(nameof(IsTeamSelected));
                    UpdateFeatureSelection();
                }
            }
        }

        private void UpdateFeatureSelection()
        {
            IsSelectionChange = true;
            IsZipParsed = false;

            foreach (GenerationType generation in Enum.GetValues(typeof(GenerationType)))
            {
                bool generationSelected = (generation == GenerationType.WebApi) ? IsWebApiSelected : IsFrontSelected;
                foreach (FeatureType type in Enum.GetValues(typeof(FeatureType)))
                {
                    ZipFeatureType feature = ZipFeatureTypeList.Where(x => x.FeatureType == type && x.GenerationType == generation).FirstOrDefault();
                    if (feature != null)
                    {
                        bool typeSelected = false;
                        switch (type)
                        {
                            case FeatureType.CRUD:
                                typeSelected = isCrudSelected && generationSelected;
                                break;

                            case FeatureType.Option:
                                typeSelected = isOptionSelected && generationSelected;
                                break;

                            case FeatureType.Team:
                                typeSelected = IsTeamSelected && generationSelected;
                                break;
                        }

                        AddRemoveZipToList(generation, typeSelected, feature.ZipName);
                        feature.IsChecked = typeSelected;
                    }
                }
            }
        }

        private void AddRemoveZipToList(GenerationType generation, bool isChecked, string featureName)
        {
            if (generation == GenerationType.WebApi)
            {
                if (isChecked)
                {
                    if (!zipDotNetCollection.Contains(featureName))
                        ZipDotNetCollection.Add(featureName);
                }
                else
                    ZipDotNetCollection.Remove(featureName);
                RaisePropertyChanged(nameof(ZipDotNetCollection));
            }
            else if (generation == GenerationType.Front)
            {
                if (isChecked)
                {
                    if (!ZipAngularCollection.Contains(featureName))
                        ZipAngularCollection.Add(featureName);
                }
                else
                    ZipAngularCollection.Remove(featureName);
                RaisePropertyChanged(nameof(ZipAngularCollection));
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

        private ObservableCollection<string> zipDotNetCollection;
        public ObservableCollection<string> ZipDotNetCollection
        {
            get => zipDotNetCollection;
            set
            {
                if (zipDotNetCollection != value)
                {
                    zipDotNetCollection = value;
                    RaisePropertyChanged(nameof(ZipDotNetCollection));
                }
            }
        }

        private ObservableCollection<string> zipAngularCollection;
        public ObservableCollection<string> ZipAngularCollection
        {
            get => zipAngularCollection;
            set
            {
                if (zipAngularCollection != value)
                {
                    zipAngularCollection = value;
                    RaisePropertyChanged(nameof(ZipAngularCollection));
                }
            }
        }

        private bool isZipParsed = false;
        public bool IsZipParsed
        {
            get => isZipParsed;
            set
            {
                isZipParsed = value;
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }
        #endregion

        #region Button

        public bool IsOptionItemEnable
        {
            get
            {
                return IsButtonParseDtoEnable && IsCrudSelected && !IsOptionSelected;
            }
        }

        public bool IsButtonParseDtoEnable
        {
            get
            {
                return !string.IsNullOrWhiteSpace(DtoSelected);
            }
        }

        public bool IsButtonParseZipEnable
        {
            get
            {
                return IsDtoParsed
                    && ((IsWebApiSelected || IsFrontSelected)
                    && (IsCrudSelected || IsOptionSelected || IsTeamSelected));
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed
                    && IsZipParsed
                    && !string.IsNullOrWhiteSpace(CRUDNamePlural)
                    && !string.IsNullOrWhiteSpace(dtoDisplayItemSelected);
            }
        }
        #endregion
    }

    public class OptionItem
    {
        public bool Check { get; set; }
        public string OptionName { get; set; }

        public OptionItem(string name, bool check = false)
        {
            this.Check = check;
            this.OptionName = name;
        }
    }

    public class ZipFeatureType
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The Feature type.
        /// </summary>
        public FeatureType FeatureType { get; }

        /// <summary>
        /// The Generation type.
        /// </summary>
        public GenerationType GenerationType { get; }

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; }

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; }

        public List<FeatureData> FeatureDataList { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipFeatureType(FeatureType type, GenerationType generation, string name, string path)
        {
            this.FeatureType = type;
            this.GenerationType = generation;
            this.ZipName = name;
            this.ZipPath = path;
        }
    }

    #region FeatureData
    public class FeatureData
    {
        /// <summary>
        /// File name (only).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File full path on Temporary working directory.
        /// </summary>
        public string FilePath { get; }

        public bool IsPartialFile { get; } = false;

        public bool IsPropertyFile { get; set; } = false;

        /// <summary>
        /// Temporary working directory full path.
        /// </summary>
        public string ExtractDirPath { get; }

        /// <summary>
        /// List of ExtractBlocks.
        /// </summary>
        public List<ExtractBlock> ExtractBlocks { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FeatureData(string fileName, string filePath, string tmpDir)
        {
            this.FileName = fileName;
            this.FilePath = filePath;
            this.ExtractDirPath = tmpDir;
            this.IsPartialFile = fileName.EndsWith(Constants.PartialFileSuffix);
        }
    }

    public class WebApiFeatureData : FeatureData
    {
        /// <summary>
        /// File type.
        /// </summary>
        public WebApiFileType? FileType { get; }

        /// <summary>
        /// List of Options to delete.
        /// </summary>
        public ClassDefinition ClassFileDefinition { get; set; }

        public string Namespace { get; set; }

        public List<PropertyInfo> PropertiesInfos { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WebApiFeatureData(string fileName, string filePath, string tmpDir, WebApiFileType? type) : base(fileName, filePath, tmpDir)
        {
            this.FileType = type;
        }
    }
    #endregion

    public class WebApiNamespace
    {
        public WebApiFileType FileType { get; }

        public string CrudNamespace { get; }

        public string CrudNamespaceGenerated { get; set; }

        public WebApiNamespace(WebApiFileType fileType, string crudNamespace)
        {
            this.FileType = fileType;
            this.CrudNamespace = crudNamespace;
        }
    }

    #region ExtractBlock
    public class ExtractBlock
    {
        public CRUDDataUpdateType DataUpdateType { get; }

        public string Name { get; }

        public List<string> BlockLines { get; }

        public ExtractBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines)
        {
            this.DataUpdateType = dataUpdateType;
            this.Name = name;
            this.BlockLines = lines;
        }
    }

    public class ExtractPropertiesBlock : ExtractBlock
    {
        public List<CRUDPropertyType> PropertiesList { get; set; }

        public ExtractPropertiesBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) :
            base(dataUpdateType, name, lines)
        { }
    }

    public class ExtractPartialBlock : ExtractBlock
    {
        public string Index { get; }

        public ExtractPartialBlock(CRUDDataUpdateType dataUpdateType, string name, string index, List<string> lines) :
            base(dataUpdateType, name, lines)
        {
            this.Index = index;
        }
    }

    public class ExtractOptionFieldBlock : ExtractBlock
    {
        public string PropertyField { get; }

        public ExtractOptionFieldBlock(CRUDDataUpdateType dataUpdateType, string name, string propertyField, List<string> lines) :
            base(dataUpdateType, name, lines)
        {
            this.PropertyField = propertyField;
        }
    }

    public class ExtractDisplayBlock : ExtractBlock
    {
        public string ExtractLine { get; set; }

        public string ExtractItem { get; set; }

        public ExtractDisplayBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) :
            base(dataUpdateType, name, lines)
        { }
    }

    public class CRUDPropertyType
    {
        public string Name { get; }

        private string type;
        public string Type
        {
            get { return type; }
            set
            {
                type = value;
                SimplifiedType = type.Split('|')[0].Trim();
            }
        }

        public string SimplifiedType { get; private set; }

        public CRUDPropertyType(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
    #endregion

    #region enum
    public enum GenerationType
    {
        WebApi,
        Front,
    }

    public enum FeatureType
    {
        CRUD,
        Option,
        Team
    }

    public enum CRUDDataUpdateType
    {
        Properties,
        Block,
        Child,
        Option,
        OptionField,
        Display,
        Parent,
        AncestorTeam,
        // Partial
        Config,
        Dependency,
        Navigation,
        Permission,
        Rights,
        Routing
    }

    public enum WebApiFileType
    {
        AppService,
        Controller,
        Dto,
        Entity,
        IAppService,
        Mapper,
        Partial
    }
    #endregion
}
