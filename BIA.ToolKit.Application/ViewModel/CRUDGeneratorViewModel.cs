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
            CRUDZipDataList = new();
            DotNetZipFilesContent = new();
            AngularZipContentFiles = new();
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
        private bool isEnable;
        public bool IsEnable
        {
            get { return isEnable; }
            set
            {
                isEnable = value;
                RaisePropertyChanged(nameof(IsEnable));
            }
        }

        private List<CRUDTypeData> crudZipDataList;
        public List<CRUDTypeData> CRUDZipDataList
        {
            get { return crudZipDataList; }
            set
            {
                if (crudZipDataList != value)
                {
                    crudZipDataList = value;
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
        #endregion

        #region Button
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

        public List<CRUDAngularData> AngularZipContentFiles { get; }
    }

    public class CRUDTypeData
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The CRUD type.
        /// </summary>
        public CRUDType Type { get; }

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
        public CRUDTypeData(CRUDType Type, string name, string path)
        {
            this.Type = Type;
            this.ZipName = name;
            this.ZipPath = path;
        }
    }

    public class CRUDAngularData
    {
        /// <summary>
        /// File name (only).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File destination path.
        /// </summary>
        public string FilePathDest { get; }

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
        public CRUDAngularData(string fileName, string filePath, string tmpDir)
        {
            this.FileName = fileName;
            this.FilePathDest = filePath;
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

    public enum CRUDType
    {
        Back,
        Feature,
        Option,
        Team
    }

    public enum CRUDDataUpdateType
    {
        Property,
        Block
    }
}
