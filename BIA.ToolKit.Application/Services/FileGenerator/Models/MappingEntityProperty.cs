namespace BIA.ToolKit.Application.Services.FileGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Common;
    using CommunityToolkit.Mvvm.ComponentModel;

    public class MappingEntityProperty : ObservableObject
    {
        protected void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }

        private string mappingName = null;
        public string MappingName
        {
            // Intelligent getter to simplify unitary test
            get => mappingName ?? EntityCompositeName;
            set
            {
                mappingName = value;
                RaisePropertyChanged(nameof(MappingName));
            }
        }

        private string mappingType = null;
        public string MappingType
        {
            // Intelligent getter to simplify unitary test
            get => mappingType ?? EntityType;
            set => mappingType = value;
        }

        public string ParentEntityType { get; set; }
        public bool IsOption => MappingType.Equals(Constants.BiaClassName.OptionDto, StringComparison.OrdinalIgnoreCase);
        public bool IsOptionCollection => MappingType.Equals(Constants.BiaClassName.CollectionOptionDto, StringComparison.OrdinalIgnoreCase);
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
                OptionEntityIdPropertyComposite = IsCompositeName ? string.Join('.', EntityCompositeName.Split('.').SkipLast(1)) + $".{value}" : value;
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
        private bool IsCompositeName => EntityCompositeName?.Contains('.') == true;

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

        public bool IsVisibleIsParentCheckbox => EntityCompositeName?.EndsWith("Id", StringComparison.OrdinalIgnoreCase) == true && !string.Equals(EntityCompositeName, "Id", StringComparison.OrdinalIgnoreCase);

        private bool ComputeValidity()
        {
            var isMappingNameValid = !string.IsNullOrWhiteSpace(MappingName) && !HasMappingNameError;
            var isOptionIdPropertyValid = !string.IsNullOrWhiteSpace(OptionIdProperty);
            var isOptionDisplayPropertyValid = !string.IsNullOrWhiteSpace(OptionDisplayProperty);

            if (IsOption)
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
                return isMappingNameValid && isOptionIdPropertyValid && isOptionDisplayPropertyValid && isOptionRelationTypeValid && isOptionRelationPropertyValid &&
                    isOptionRelationFirstIdPropertyValid && isOptionRelationSecondIdPropertyValid;
            }

            return isMappingNameValid;
        }
    }
}
