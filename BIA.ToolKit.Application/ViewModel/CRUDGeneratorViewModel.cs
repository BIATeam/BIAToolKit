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
        CRUDTypeData crudDataFeature;
        public CRUDTypeData CRUDDataFeature
        {
            get { return crudDataFeature; }
            set
            {
                if (crudDataFeature != value)
                {
                    crudDataFeature = value;
                }
            }
        }

        CRUDTypeData crudDataOption;
        public CRUDTypeData CRUDDataOption
        {
            get { return crudDataOption; }
            set
            {
                if (crudDataOption != value)
                {
                    crudDataOption = value;
                }
            }
        }

        CRUDTypeData crudDataTeam;
        public CRUDTypeData CRUDDataTeam
        {
            get { return crudDataTeam; }
            set
            {
                if (crudDataTeam != value)
                {
                    crudDataTeam = value;
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
                    && ZipDotNetSelected.Count > 0
                    && ZipAngularSelected.Count > 0;
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
        /// DotNet zip file name.
        /// </summary>
        public string DotNetZipName { get; }

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string AngularZipName { get; }

        /// <summary>
        /// DotNet zip file path.
        /// </summary>
        public string DotNetZipPath { get; }

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string AngularZipPath { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CRUDTypeData(CRUDType Type, string dotNetName, string dotNetPath, string angularName, string angularPath)
        {
            this.Type = Type;
            this.DotNetZipName = dotNetName;
            this.AngularZipName = angularName;
            this.DotNetZipPath = dotNetPath;
            this.AngularZipPath = angularPath;
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
        public string Type { get; }

        public string Name { get; }

        public List<string> BlockLines { get; }

        public ExtractBlocks(string type, string name, List<string> lines)
        {
            this.Type = type;
            this.Name = name;
            this.BlockLines = lines;
        }
    }

    public enum CRUDType
    {
        Feature,
        Option,
        Team
    }
}
