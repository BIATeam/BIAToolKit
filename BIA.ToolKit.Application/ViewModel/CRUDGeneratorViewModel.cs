namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
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

        private string entityNamePlurial;
        public string CRUDNamePlurial
        {
            get => entityNamePlurial;
            set
            {
                if (entityNamePlurial != value)
                {
                    entityNamePlurial = value;
                    RaisePropertyChanged(nameof(CRUDNamePlurial));
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
                    UpdateFeatureSelection(FeatureType.WebApi, value);
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
                    UpdateFeatureSelection(FeatureType.CRUD, value);
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
                    UpdateFeatureSelection(FeatureType.Option, value);
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
                    UpdateFeatureSelection(FeatureType.Team, value);
                }
            }
        }

        private void UpdateFeatureSelection(FeatureType type, bool isChecked)
        {
            IsSelectionChange = true;
            IsZipParsed = false;

            ZipFeatureType feature = ZipFeatureTypeList.Where(x => x.FeatureType == type).FirstOrDefault();
            if (feature != null)
            {
                feature.IsChecked = isChecked;
                if (type == FeatureType.WebApi)
                {
                    if (isChecked)
                        ZipDotNetCollection.Add(feature.ZipName);
                    else
                        ZipDotNetCollection.Remove(feature.ZipName);
                }
                else
                {
                    if (isChecked)
                        ZipAngularCollection.Add(feature.ZipName);
                    else
                        ZipAngularCollection.Remove(feature.ZipName);
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
                    && (IsWebApiSelected || IsCrudSelected || IsOptionSelected || IsTeamSelected);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed
                    && IsZipParsed
                    && !string.IsNullOrWhiteSpace(CRUDNamePlurial)
                    && !string.IsNullOrWhiteSpace(dtoDisplayItemSelected);
            }
        }
        #endregion
    }

    public class ZipFeatureType
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The CRUD type.
        /// </summary>
        public FeatureType FeatureType { get; }

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
        public ZipFeatureType(FeatureType type, string name, string path)
        {
            this.FeatureType = type;
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

        public bool IsPartialFile { get; }

        /// <summary>
        /// Temporary working directory full path.
        /// </summary>
        public string ExtractDirPath { get; }

        /// <summary>
        /// List of ExtractBlocks.
        /// </summary>
        public List<ExtractBlock> ExtractBlocks { get; set; }

        /// <summary>
        /// List of Options to delete.
        /// </summary>
        //public List<string> OptionToDelete { get; set; }

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
    public enum FeatureType
    {
        WebApi,
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
        Display,
        Parent,
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
