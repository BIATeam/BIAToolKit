namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CRUDGeneratorViewModel : ObservableObject
    {
        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel()
        {
            FeatureTypeDataList = new();
            DotNetZipFilesContent = new();
            AngularZipFilesContent = new();
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
                    RaisePropertyChanged("DtoFiles");
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

        public List<ClassDefinition> DotNetZipFilesContent { get; }

        public List<AngularFeatureData> AngularZipFilesContent { get; }
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

    public class AngularFeatureData
    {
        public FeatureType Type { get; }

        /// <summary>
        /// File name (only).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File full path on Temporary working directory.
        /// </summary>
        public string FileFullPath { get; }

        /// <summary>
        /// Temporary working directory full path.
        /// </summary>
        public string TempDirPath { get; }

        /// <summary>
        /// List of ExtractBlocks.
        /// </summary>
        public List<ExtractBlocks> ExtractBlocks { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AngularFeatureData(FeatureType type, string fileName, string filePath, string tmpDir)
        {
            this.Type = type;
            this.FileName = fileName;
            this.FileFullPath = filePath;
            this.TempDirPath = tmpDir;
        }
    }

    public class ExtractBlocks
    {
        public CRUDDataUpdateType DataUpdateType { get; }

        public string Type { get; }

        public string Name { get; }

        public List<string> BlockLines { get; }

        public ExtractBlocks(CRUDDataUpdateType dataUpdateType, string type, string name, List<string> lines)
        {
            this.DataUpdateType = dataUpdateType;
            this.Type = type;
            this.Name = name;
            this.BlockLines = lines;
        }
    }

    public enum FeatureType
    {
        Back,
        CRUD,
        Option,
        Team
    }

    public enum CRUDDataUpdateType
    {
        Property,
        Block
    }
}
