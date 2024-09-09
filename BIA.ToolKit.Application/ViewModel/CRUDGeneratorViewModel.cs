namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using System;
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
            OptionItems = new();
            ZipFeatureTypeList = new();
            Features = new();
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
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
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

        private ObservableCollection<OptionItem> optionItems;
        public ObservableCollection<OptionItem> OptionItems
        {
            get => optionItems;
            set
            {
                if (optionItems != value)
                {
                    optionItems = value;
                    RaisePropertyChanged(nameof(OptionItems));
                }
            }
        }

        private bool isDtoGenerated = false;
        public bool IsDtoGenerated
        {
            get => isDtoGenerated;
            set
            {
                if (isDtoGenerated != value)
                {
                    isDtoGenerated = value;
                    RaisePropertyChanged(nameof(IsDtoGenerated));
                }
            }
        }

        private ObservableCollection<string> features;
        public ObservableCollection<string> Features
        {
            get => features;
            set
            {
                if (features != value)
                {
                    features = value;
                    RaisePropertyChanged(nameof(Features));
                }
            }
        }

        private string featureSelected;
        public string FeatureSelected
        {
            get => featureSelected;
            set
            {
                if (featureSelected != value)
                {
                    featureSelected = value;
                    RaisePropertyChanged(nameof(FeatureSelected));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                    UpdateFeatureSelection();
                }
            }
        }

        private void UpdateFeatureSelection()
        {
            ZipFeatureTypeList.ForEach(x => x.IsChecked = false);
            if (string.IsNullOrEmpty(featureSelected))
                return;

            foreach (var zipFeatureType in ZipFeatureTypeList.Where(x => x.Feature == FeatureSelected))
            {
                if (zipFeatureType.GenerationType == GenerationType.WebApi && isWebApiSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
                if (zipFeatureType.GenerationType == GenerationType.Front && isFrontSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
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

        private string entityNamePlural;
        public string CRUDNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    RaisePropertyChanged(nameof(CRUDNamePlural));
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
                    UpdateFeatureSelection();
                }
            }
        }

        private bool isFrontSelected;
        public bool IsFrontSelected
        {
            get => isFrontSelected;
            set
            {
                if (isFrontSelected != value)
                {
                    isFrontSelected = value;
                    RaisePropertyChanged(nameof(IsFrontSelected));
                    UpdateFeatureSelection();
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
        public bool IsOptionItemEnable
        {
            get
            {
                return !string.IsNullOrEmpty(featureSelected) && ZipFeatureTypeList.Any(x => x.Feature == featureSelected && x.FeatureType == FeatureType.Option);
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed && IsZipParsed
                    && !string.IsNullOrWhiteSpace(CRUDNameSingular)
                    && !string.IsNullOrWhiteSpace(CRUDNamePlural)
                    && !string.IsNullOrWhiteSpace(dtoDisplayItemSelected)
                    && (IsWebApiSelected || isFrontSelected) && !string.IsNullOrEmpty(featureSelected);
            }
        }
        #endregion
    }

    public class OptionItem
    {
        public bool Check { get; set; }
        public string OptionName { get; set; }

        public OptionItem(string name, bool check = false)
        {
            this.Check = check;
            this.OptionName = name;
        }
    }

    public class ZipFeatureType
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The Feature type.
        /// </summary>
        public FeatureType FeatureType { get; }

        /// <summary>
        /// The Generation type.
        /// </summary>
        public GenerationType GenerationType { get; }

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; }

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; }

        /// <summary>
        /// Name of the feature associated to the ZIP
        /// </summary>
        public string Feature { get; }

        public List<FeatureData> FeatureDataList { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ZipFeatureType(FeatureType type, GenerationType generation, string zipName, string zipPath, string feature)
        {
            this.FeatureType = type;
            this.GenerationType = generation;
            this.ZipName = zipName;
            this.ZipPath = zipPath;
            this.Feature = feature;
        }
    }

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
}
