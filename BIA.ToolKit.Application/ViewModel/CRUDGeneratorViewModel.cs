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

        #region Entity Name
        private string entityNameSingular;
        public string EntityNameSingular
        {
            get { return entityNameSingular; }
            set
            {
                if (entityNameSingular != value)
                {
                    entityNameSingular = value;
                    RaisePropertyChanged(nameof(EntityNameSingular));
                }
            }
        }

        private string entityNamePlurial;
        public string EntityNamePlurial
        {
            get { return entityNamePlurial; }
            set
            {
                if (entityNamePlurial != value)
                {
                    entityNamePlurial = value;
                    RaisePropertyChanged(nameof(EntityNamePlurial));
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
                    && ZipAngularSelected.Count > 0
                    && !string.IsNullOrWhiteSpace(EntityNamePlurial);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return isDtoParsed && isZipParsed;
            }
        }

        public bool IsCheckedAction
        {
            set
            {
                RaisePropertyChanged(nameof(IsButtonParseZipEnable));
            }
        }
        #endregion

        public List<ClassDefinition> DotNetZipFilesContent { get; }
    }

    public class CRUDTypeData
    {
        public bool IsChecked { get; set; }

        public CRUDType Type { get; }

        public string DotNetZipName { get; }

        public string AngularZipName { get; }

        public string DotNetZipPath { get; }

        public string AngularZipPath { get; }

        public CRUDTypeData(CRUDType Type, string dotNetName, string dotNetPath, string angularName, string angularPath)
        {
            this.Type = Type;
            this.DotNetZipName = dotNetName;
            this.AngularZipName = angularName;
            this.DotNetZipPath = dotNetPath;
            this.AngularZipPath = angularPath;
        }
    }

    public enum CRUDType
    {
        Feature,
        Option,
        Team
    }
}
