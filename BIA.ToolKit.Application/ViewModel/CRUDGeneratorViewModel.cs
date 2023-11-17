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
                    DtoProperties = dtoEntity.Properties;
                }
            }
        }

        private List<PropertyInfo> dtoProperties;
        public List<PropertyInfo> DtoProperties
        {
            get { return dtoProperties; }
            private set
            {
                if (dtoProperties != value)
                {
                    dtoProperties = value;
                    RaisePropertyChanged(nameof(DtoProperties));
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

        #region ZipFile
        Dictionary<string, string> zipDotNetFiles;
        public Dictionary<string, string> ZipDotNetFiles
        {
            get { return zipDotNetFiles; }
            set
            {
                if (zipDotNetFiles != value)
                {
                    zipDotNetFiles = value;
                    RaisePropertyChanged("ZipDotNetFiles");
                }
            }
        }

        Dictionary<string, string> zipAngularFiles;
        public Dictionary<string, string> ZipAngularFiles
        {
            get { return zipAngularFiles; }
            set
            {
                if (zipAngularFiles != value)
                {
                    zipAngularFiles = value;
                    RaisePropertyChanged("ZipAngularFiles");
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


        private bool generateCrudFeature = false;
        public bool GenerateCrudFeature
        {
            get { return generateCrudFeature; }
            set
            {
                generateCrudFeature = value;
                RaisePropertyChanged(nameof(GenerateCrudFeature));
            }
        }

        private bool generateCrudTeam = false;
        public bool GenerateCrudTeam
        {
            get { return generateCrudTeam; }
            set
            {
                generateCrudTeam = value;
                RaisePropertyChanged(nameof(GenerateCrudTeam));
            }
        }

        private bool generateCrudOption = false;
        public bool GenerateCrudOption
        {
            get { return generateCrudOption; }
            set
            {
                generateCrudOption = value;
                RaisePropertyChanged(nameof(GenerateCrudOption));
            }
        }


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
                    && (ZipDotNetSelected.Count > 0)
                    && (ZipAngularSelected.Count > 0);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return isDtoParsed && isZipParsed;
            }
        }
        #endregion




        public List<ClassDefinition> DotNetZipFilesContent { get; }
    }
}
