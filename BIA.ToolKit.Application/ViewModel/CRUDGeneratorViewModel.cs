namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System.Collections.Generic;

    public class CRUDGeneratorViewModel : ObservableObject
    {
        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel()
        {
            DotNetZipFilesContent = new();
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

        #region DtoEntity DtoProperties
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
                return !string.IsNullOrWhiteSpace(dtoRootFilePath);
            }
        }

        public bool IsButtonParseZipEnable
        {
            get
            {
                return !string.IsNullOrWhiteSpace(zipRootFilePath) && isDtoParsed &&
                    CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.Folder);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsButtonParseDtoEnable && isDtoParsed
                    && IsButtonParseZipEnable && isZipParsed;
            }
        }
        #endregion

        #region DtoFile
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
                }
            }
        }

        private string dtoRootFilePath;
        public string DtoRootFilePath
        {
            get { return dtoRootFilePath; }
            set
            {
                if (dtoRootFilePath != value)
                {
                    dtoRootFilePath = value;
                    RaisePropertyChanged(nameof(DtoRootFilePath));
                    RaisePropertyChanged(nameof(IsButtonParseDtoEnable));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }
        #endregion

        #region ZipFile
        Dictionary<string, string> zipFiles;
        public Dictionary<string, string> ZipFiles
        {
            get { return zipFiles; }
            set
            {
                if (zipFiles != value)
                {
                    zipFiles = value;
                    RaisePropertyChanged("ZipFiles");
                }
            }
        }

        private string zipSelected;
        public string ZipSelected
        {
            get { return zipSelected; }
            set
            {
                if (zipSelected != value)
                {
                    zipSelected = value;
                }
            }
        }

        private string zipRootFilePath;
        public string ZipRootFilePath
        {
            get { return zipRootFilePath; }
            set
            {
                if (zipRootFilePath != value)
                {
                    zipRootFilePath = value;
                    //zipContent?.ZipPath = value;
                    RaisePropertyChanged(nameof(ZipRootFilePath));
                    RaisePropertyChanged(nameof(IsButtonParseZipEnable));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }
        #endregion

        public List<ClassDefinition> DotNetZipFilesContent { get; }
    }
}
