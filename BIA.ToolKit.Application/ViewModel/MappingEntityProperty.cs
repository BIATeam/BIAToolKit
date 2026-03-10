namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using System.Collections.Generic;

    public class MappingEntityProperty : ObservableObject
    {
        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }

        private string mappingName = null;
        public string MappingName
        {
            // Inteligent getter to simplify unitary test
            get { if (mappingName != null) { return mappingName; } else { return EntityCompositeName; } }
            set { mappingName = value; RaisePropertyChanged(nameof(MappingName)); }
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

        private bool isRequired;
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                RaisePropertyChanged(nameof(IsRequired));
            }
        }

        public List<string> OptionIdProperties { get; set; } = new();
        public List<string> OptionDisplayProperties { get; set; } = new();
        public List<string> OptionEntityIdProperties { get; set; } = new();

        private string optionDisplayProperty;
        public string OptionDisplayProperty
        {
            get => optionDisplayProperty;
            set
            {
                optionDisplayProperty = value;
                RaisePropertyChanged(nameof(OptionDisplayProperty));
            }
        }

        private string optionIdProperty;
        public string OptionIdProperty
        {
            get => optionIdProperty;
            set
            {
                optionIdProperty = value;
                RaisePropertyChanged(nameof(OptionIdProperty));
            }
        }

        public bool IsVisibleOptionPropertiesComboBox => IsOption || IsOptionCollection;
        public string OptionEntityIdPropertyComposite { get; private set; }

        private string optionEntityIdProperty;
        public string OptionEntityIdProperty 
        {
            get => optionEntityIdProperty;
            set
            {
                optionEntityIdProperty = value;
                OptionEntityIdPropertyComposite = IsCompositeName ?
                    string.Join(".", EntityCompositeName.Split('.').SkipLast(1)) + $".{value}" :
                    value;
                RaisePropertyChanged(nameof(OptionEntityIdProperty));
            }
        }
        
        public string OptionRelationType { get; set; }
        public string OptionRelationFirstIdProperty { get; set; }
        public string OptionRelationSecondIdProperty { get; set; }
        public string OptionRelationPropertyComposite { get; set; }

        private string mappingDateType;
        public string MappingDateType
        {
            get => mappingDateType;
            set
            {
                mappingDateType = value;
                RaisePropertyChanged(nameof(MappingDateType));
            }
        }

        public List<string> MappingDateTypes { get; set; } = new();
        public bool IsVisibleDateTypesComboxBox => MappingDateTypes.Count > 0;

        public bool IsValid => ComputeValidity();
        private bool IsCompositeName => EntityCompositeName.Contains('.');

        public bool HasMappingNameError => !string.IsNullOrWhiteSpace(MappingNameError);
        private string mappingNameError;
        public string MappingNameError
        {
            get => mappingNameError;
            set 
            { 
                mappingNameError = value; 
                RaisePropertyChanged(nameof(MappingNameError));
                RaisePropertyChanged(nameof(HasMappingNameError));
            }
        }

        private bool isParent;
        public bool IsParent
        {
            get => isParent;
            set 
            { 
                isParent = value; 
                RaisePropertyChanged(nameof(IsParent));
            }
        }

        public bool IsVisibleIsParentCheckbox => 
            EntityCompositeName.EndsWith("Id") 
            && !EntityCompositeName.Equals("Id")
            ;


        private bool ComputeValidity()
        {
            // Common validity
            var isMappingNameValid = !string.IsNullOrWhiteSpace(MappingName) && !HasMappingNameError;
            // Options validity
            var isOptionIdPropertyValid = !string.IsNullOrWhiteSpace(OptionIdProperty);
            var isOptionDisplayPropertyValid = !string.IsNullOrWhiteSpace(OptionDisplayProperty);

            if(IsOption)
            {
                var isOptionEntityIdPropertyValid = !string.IsNullOrWhiteSpace(OptionEntityIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionEntityIdPropertyValid;
            }

            if (IsOptionCollection)
            {
                var isOptionRelationTypeValid = !string.IsNullOrWhiteSpace(OptionRelationType);
                var isOptionRelationPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationPropertyComposite);
                var isOptionRelationFirstIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationFirstIdProperty);
                var isOptionRelationSecondIdPropertyValid = !string.IsNullOrWhiteSpace(OptionRelationSecondIdProperty);
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionRelationTypeValid && isOptionRelationPropertyValid
                    && isOptionRelationFirstIdPropertyValid && isOptionRelationSecondIdPropertyValid;
            }

            return isMappingNameValid;
        }
    }
}
