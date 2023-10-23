namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.DtoGenerator;
    using System.Collections.Generic;

    public class CRUDGeneratorViewModel : ObservableObject
    {

        /// <summary>  
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel()
        {
            CRUDGenerator = new CRUDGenerator();
            DtoProperties = new();
        }

        public CRUDGenerator CRUDGenerator { get; set; }

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
                    RaisePropertyChanged(nameof(ZipRootFilePath));
                    RaisePropertyChanged(nameof(IsButtonParseZipEnable));
                }
            }
        }

        private List<PropertyInfo> dtoProperties;
        public List<PropertyInfo> DtoProperties
        {
            get { return dtoProperties; }
            set
            {
                if (dtoProperties != value)
                {
                    dtoProperties = value;
                    RaisePropertyChanged(nameof(DtoProperties));
                }
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
                return !string.IsNullOrWhiteSpace(zipRootFilePath);
            }
        }
    }
}
