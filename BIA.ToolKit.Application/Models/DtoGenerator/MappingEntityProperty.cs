namespace BIA.ToolKit.Application.Models.DtoGenerator
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Common;
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class MappingEntityProperty : ObservableObject
    {
        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }

        private string mappingName = null;
        public string MappingName
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingName != null) { return mappingName; } else { return EntityCompositeName; } }
            set { mappingName = value; OnPropertyChanged(nameof(MappingName)); }
        }

        private string mappingType = null;
        public string MappingType
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingType != null) { return mappingType; } else { return EntityType; } }
            set { mappingType = value; }
        }

        public string ParentEntityType { get; set; }
        public bool IsOption => MappingType.Equals(Constants.BiaClassName.OptionDto);
        public bool IsOptionCollection => MappingType.Equals(Constants.BiaClassName.CollectionOptionDto);
        public string OptionType { get; set; }

        [ObservableProperty]
        private bool isRequired;

        public List<string> OptionIdProperties { get; set; } = [];
        public List<string> OptionDisplayProperties { get; set; } = [];
        public List<string> OptionEntityIdProperties { get; set; } = [];

        [ObservableProperty]
        private string optionDisplayProperty;

        [ObservableProperty]
        private string optionIdProperty;

        public bool IsVisibleOptionPropertiesComboBox => IsOption || IsOptionCollection;
        public string OptionEntityIdPropertyComposite { get; private set; }

        [ObservableProperty]
        private string optionEntityIdProperty;

        partial void OnOptionEntityIdPropertyChanged(string value)
        {
            OptionEntityIdPropertyComposite = IsCompositeName ?
                string.Join(".", EntityCompositeName.Split('.').SkipLast(1)) + $".{value}" :
                value;
        }

        public string OptionRelationType { get; set; }
        public string OptionRelationFirstIdProperty { get; set; }
        public string OptionRelationSecondIdProperty { get; set; }
        public string OptionRelationPropertyComposite { get; set; }

        [ObservableProperty]
        private string mappingDateType;

        public List<string> MappingDateTypes { get; set; } = [];
        public bool IsVisibleDateTypesComboxBox => MappingDateTypes.Count > 0;

        public bool IsValid => ComputeValidity();
        private bool IsCompositeName => EntityCompositeName.Contains('.');

        public bool HasMappingNameError => !string.IsNullOrWhiteSpace(MappingNameError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMappingNameError))]
        private string mappingNameError;

        [ObservableProperty]
        private bool isParent;

        [ObservableProperty]
        private bool asLocalDateTime;

        public bool IsVisibleIsParentCheckbox =>
            EntityCompositeName.EndsWith("Id")
            && !EntityCompositeName.Equals("Id")
            ;


        private bool ComputeValidity()
        {
            // Common validity
            bool isMappingNameValid = !string.IsNullOrWhiteSpace(MappingName) && !HasMappingNameError;
            // Options validity
            bool isOptionIdPropertyValid = !string.IsNullOrWhiteSpace(OptionIdProperty);
            bool isOptionDisplayPropertyValid = !string.IsNullOrWhiteSpace(OptionDisplayProperty);

            if (IsOption)
            {
                bool isOptionEntityIdPropertyValid = !string.IsNullOrWhiteSpace(OptionEntityIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionEntityIdPropertyValid;
            }

            if (IsOptionCollection)
            {
                bool isOptionRelationTypeValid = !string.IsNullOrWhiteSpace(OptionRelationType);
                bool isOptionRelationPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationPropertyComposite);
                bool isOptionRelationFirstIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationFirstIdProperty);
                bool isOptionRelationSecondIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationSecondIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionRelationTypeValid && isOptionRelationPropertyValid
                    && isOptionRelationFirstIdPropertyValid && isOptionRelationSecondIdPropertyValid;
            }

            return isMappingNameValid;
        }
    }
}
