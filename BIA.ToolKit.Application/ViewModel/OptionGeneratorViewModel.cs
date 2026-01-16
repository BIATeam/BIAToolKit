namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using CommunityToolkit.Mvvm.ComponentModel;
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
                OnPropertyChanged(nameof(IsProjectChosen));
            }
        }

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                OnPropertyChanged(nameof(BiaFront));
            }
        }

        private ObservableCollection<string> _biaFronts = new();
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                OnPropertyChanged(nameof(BiaFronts));
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
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
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
                    OnPropertyChanged(nameof(Entities));
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
                    OnPropertyChanged(nameof(EntityDisplayItems));
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
                    OnPropertyChanged(nameof(IsEntityParsed));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
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
                    OnPropertyChanged(nameof(EntityDisplayItemSelected));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
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
                    OnPropertyChanged(nameof(IsGenerated));
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
                    OnPropertyChanged(nameof(EntityNamePlural));
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
                    OnPropertyChanged(nameof(Domain));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
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
