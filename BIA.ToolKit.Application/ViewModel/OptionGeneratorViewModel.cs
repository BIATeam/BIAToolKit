namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class OptionGeneratorViewModel : ObservableObject
    {
        /// <summary>  
        /// Constructor.
        /// </summary>
        public OptionGeneratorViewModel()
        {
            ZipFeatureTypeList = new();
            Entities = new();
            EntityDisplayItems = new();
        }

        #region CurrentProject
        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
                BiaFronts.Clear();
                if(currentProject != null)
                {
                    foreach(var biaFront in currentProject.BIAFronts)
                    {
                        BiaFronts.Add(biaFront);
                    }
                    BiaFront = BiaFronts.FirstOrDefault();
                }
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

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                RaisePropertyChanged(nameof(BiaFront));
            }
        }

        private ObservableCollection<string> _biaFronts = new();
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                RaisePropertyChanged(nameof(BiaFronts));
            }
        }
        #endregion

        #region Entity
        private EntityInfo entity;
        public EntityInfo Entity
        {
            get => entity;
            set
            {
                if (entity != value)
                {
                    entity = value;
                    RaisePropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private ObservableCollection<EntityInfo> entities;
        public ObservableCollection<EntityInfo> Entities
        {
            get => entities;
            set
            {
                if (entities != value)
                {
                    entities = value;
                    RaisePropertyChanged(nameof(Entities));
                }
            }
        }

        private ObservableCollection<string> entityDisplayItems;
        public ObservableCollection<string> EntityDisplayItems
        {
            get => entityDisplayItems;
            set
            {
                if (entityDisplayItems != value)
                {
                    entityDisplayItems = value;
                    RaisePropertyChanged(nameof(EntityDisplayItems));
                }
            }
        }

        private bool isEntityParsed = false;
        public bool IsEntityParsed
        {
            get => isEntityParsed;
            set
            {
                if (isEntityParsed != value)
                {
                    isEntityParsed = value;
                    RaisePropertyChanged(nameof(IsEntityParsed));
                    RaisePropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private string entityDisplayItemSelected;
        public string EntityDisplayItemSelected
        {
            get => entityDisplayItemSelected;
            set
            {
                if (entityDisplayItemSelected != value)
                {
                    entityDisplayItemSelected = value;
                    RaisePropertyChanged(nameof(EntityDisplayItemSelected));
                    RaisePropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private bool isGenerated = false;
        public bool IsGenerated
        {
            get => isGenerated;
            set
            {
                if (isGenerated != value)
                {
                    isGenerated = value;
                    RaisePropertyChanged(nameof(IsGenerated));
                }
            }
        }
        #endregion

        #region Entity Name

        private string entityNamePlural;
        public string EntityNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    RaisePropertyChanged(nameof(EntityNamePlural));
                }
            }
        }
        #endregion

        #region Domain

        private string domain;
        public string Domain
        {
            get { return domain; }
            set 
            {
                if (domain != value)
                {
                    domain = value;
                    RaisePropertyChanged(nameof(Domain));
                    RaisePropertyChanged(nameof(IsButtonGenerateOptionEnable));
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
        #endregion

        #region Button
        public bool IsButtonGenerateOptionEnable
        {
            get
            {
                return IsEntityParsed
                    && !string.IsNullOrWhiteSpace(EntityNamePlural)
                    && !string.IsNullOrWhiteSpace(entityDisplayItemSelected)
                    && !string.IsNullOrEmpty(Domain);
            }
        }
        #endregion
    }
}
