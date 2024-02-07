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
            FeatureTypeDataList = new();
            ZipFilesContent = new();
            ZipDotNetSelected = new();
            ZipAngularSelected = new();
        }

        #region CurrentProject
        private Project currentProject;
        public Project CurrentProject
        {
            get { return currentProject; }
            set
            {
                currentProject = value;
                RaisePropertyChanged(nameof(IsButtonParseZipEnable));
            }
        }

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get { return isProjectChosen; }
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
            get { return dtoEntity; }
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
            get { return dtoFiles; }
            set
            {
                if (dtoFiles != value)
                {
                    dtoFiles = value;
                    RaisePropertyChanged(nameof(DtoFiles));
                }
            }
        }

        private string dtoSelected;
        public string DtoSelected
        {
            get { return dtoSelected; }
            set
            {
                if (dtoSelected != value)
                {
                    dtoSelected = value;
                    RaisePropertyChanged(nameof(IsButtonParseDtoEnable));
                }
            }
        }

        private bool isDtoParsed = false;
        public bool IsDtoParsed
        {
            get { return isDtoParsed; }
            set
            {
                isDtoParsed = value;
                RaisePropertyChanged(nameof(IsButtonParseZipEnable));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }
        #endregion

        #region CRUD Name
        private string entityNameSingular;
        public string CRUDNameSingular
        {
            get { return entityNameSingular; }
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
            get { return entityNamePlurial; }
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
        private bool isWebApiSelected;
        public bool IsWebApiSelected
        {
            get { return isWebApiSelected; }
            set
            {
                if (isWebApiSelected != value)
                {
                    isWebApiSelected = value;
                    RaisePropertyChanged(nameof(IsWebApiSelected));

                    FeatureTypeData feature = FeatureTypeDataList.Where(x => x.Type == FeatureType.WebApi).FirstOrDefault();
                    UpdateFeatureSelection(feature, value);
                }
            }
        }

        private bool isCrudSelected;
        public bool IsCrudSelected
        {
            get { return isCrudSelected; }
            set
            {
                if (isCrudSelected != value)
                {
                    isCrudSelected = value;
                    RaisePropertyChanged(nameof(IsCrudSelected));

                    FeatureTypeData feature = FeatureTypeDataList.Where(x => x.Type == FeatureType.CRUD).FirstOrDefault();
                    UpdateFeatureSelection(feature, value);
                }
            }
        }

        private bool isOptionSelected;
        public bool IsOptionSelected
        {
            get { return isOptionSelected; }
            set
            {
                if (isOptionSelected != value)
                {
                    isOptionSelected = value;
                    RaisePropertyChanged(nameof(IsOptionSelected));

                    FeatureTypeData feature = FeatureTypeDataList.Where(x => x.Type == FeatureType.Option).FirstOrDefault();
                    UpdateFeatureSelection(feature, value);
                }
            }
        }

        private bool isTeamSelected;
        public bool IsTeamSelected
        {
            get { return isTeamSelected; }
            set
            {
                if (isTeamSelected != value)
                {
                    isTeamSelected = value;
                    RaisePropertyChanged(nameof(IsTeamSelected));

                    FeatureTypeData feature = FeatureTypeDataList.Where(x => x.Type == FeatureType.Team).FirstOrDefault();
                    UpdateFeatureSelection(feature, value);
                }
            }
        }

        private void UpdateFeatureSelection(FeatureTypeData feature, bool isChecked)
        {
            if (feature != null)
            {
                feature.IsChecked = isChecked;
                IsZipParsed = false;
                IsCheckedAction = true;

                if (feature.IsChecked)
                {
                    if (feature.Type == FeatureType.WebApi)
                        ZipDotNetSelected.Add(feature.ZipName);
                    else
                        ZipAngularSelected.Add(feature.ZipName);
                }
                else
                {
                    if (feature.Type == FeatureType.WebApi)
                        ZipDotNetSelected.Remove(feature.ZipName);
                    else
                        ZipAngularSelected.Remove(feature.ZipName);
                }
            }
        }
        #endregion

        #region ZipFile
        private List<FeatureTypeData> featureTypeDataList;
        public List<FeatureTypeData> FeatureTypeDataList
        {
            get { return featureTypeDataList; }
            set
            {
                if (featureTypeDataList != value)
                {
                    featureTypeDataList = value;
                }
            }
        }

        private ObservableCollection<string> zipDotNetSelected;
        public ObservableCollection<string> ZipDotNetSelected
        {
            get { return zipDotNetSelected; }
            set
            {
                if (zipDotNetSelected != value)
                {
                    zipDotNetSelected = value;
                    RaisePropertyChanged(nameof(ZipDotNetSelected));
                }
            }
        }

        private ObservableCollection<string> zipAngularSelected;
        public ObservableCollection<string> ZipAngularSelected
        {
            get { return zipAngularSelected; }
            set
            {
                if (zipAngularSelected != value)
                {
                    zipAngularSelected = value;
                    RaisePropertyChanged(nameof(ZipAngularSelected));
                }
            }
        }

        private bool isZipParsed = false;
        public bool IsZipParsed
        {
            get { return isZipParsed; }
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
                return isDtoParsed
                    && (ZipDotNetSelected.Count > 0 || ZipAngularSelected.Count > 0);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return isDtoParsed
                    && isZipParsed
                    && !string.IsNullOrWhiteSpace(CRUDNamePlurial);
            }
        }

        public bool IsCheckedAction
        {
            set
            {
                RaisePropertyChanged(nameof(IsButtonParseZipEnable));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }
        #endregion

        public List<ZipFilesContent> ZipFilesContent { get; }
    }

    public class FeatureTypeData
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The CRUD type.
        /// </summary>
        public FeatureType Type { get; }

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; }

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FeatureTypeData(FeatureType type, string name, string path)
        {
            this.Type = type;
            this.ZipName = name;
            this.ZipPath = path;
        }
    }


    public class ZipFilesContent
    {
        public FeatureType Type { get; }

        public List<FeatureData> FeatureDataList { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipFilesContent(FeatureType type)
        {
            this.Type = type;
            this.FeatureDataList = new();
        }
    }

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public WebApiFeatureData(string fileName, string filePath, string tmpDir, WebApiFileType? type) : base(fileName, filePath, tmpDir)
        {
            this.FileType = type;
        }
    }

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
        public Dictionary<string, List<string>> PropertiesList { get; }

        public ExtractPropertiesBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) : base(dataUpdateType, name, lines)
        {
            PropertiesList = new();
        }
    }

    public class ExtractBlockBlock : ExtractBlock
    {
        public string Type { get; set; }

        public ExtractBlockBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) : base(dataUpdateType, name, lines)
        {
        }
    }

    public class ExtractPartialBlock : ExtractBlock
    {
        public string Index { get; }

        public ExtractPartialBlock(CRUDDataUpdateType dataUpdateType, string name, string index, List<string> lines) : base(dataUpdateType, name, lines)
        {
            this.Index = index;
        }
    }

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
}
