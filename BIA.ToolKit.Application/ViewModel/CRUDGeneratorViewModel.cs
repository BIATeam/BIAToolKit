namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using System.Collections.Generic;
    using System.IO;

    public class CRUDGeneratorViewModel : ObservableObject
    {
        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel()
        {
            ZipFilesContent = new();
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

        #region DtoRootFilePath
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

        #region ZipRootFilePath
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

        public List<ClassDefinition> ZipFilesContent { get; private set; }

        public string GetEntityFileName()
        {
            if (string.IsNullOrWhiteSpace(dtoRootFilePath))
            {
                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(dtoRootFilePath);
            if (fileName.ToLower().EndsWith("dto"))
            {
                return fileName.Substring(0, fileName.Length - 3);
            }
            else
            {
                return fileName;
            }
        }
    }
}
