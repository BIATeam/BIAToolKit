namespace BIA.ToolKit.Application.Templates.Common.Models
{
    using System;
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class PropertyDtoModel : IPropertyDtoModel
    {
        public string EntityCompositeName { get; set; }
        public string EntityType { get; set; }
        public string MappingType { get; set; }
        public string MappingName { get; set; }

        public string NonNullEntityType
        {
            get
            {
                return EntityType.Replace("?", string.Empty);
            }
        }

        public string NonNullMappingType
        {
            get
            {
                return MappingType.Replace("?", string.Empty);
            }
        }
        public string MappingTypeInDto
        {
            get
            {
                if (NonNullEntityType == "TimeSpan") { return "string"; }
                return MappingType;
            }
        }

        public string MappingDateType { get; set; }
        public bool IsOption { get; set; }
        public string OptionType { get; set; }
        public bool IsRequired { get; set; }
        public string OptionDisplayProperty { get; set; }
        public string OptionIdProperty { get; set; }
        public string OptionEntityIdPropertyComposite { get; set; }
        public string OptionRelationType { get; set; }
        public string OptionRelationPropertyComposite { get; set; }
        public string OptionRelationFirstIdProperty { get; set; }
        public string OptionRelationSecondIdProperty { get; set; }
        public bool IsOptionCollection { get; set; }
        public bool IsParent { get; set; }

        private string biaDtoFieldAttributeProperties;
        public string BiaDtoFieldAttributeProperties
        {
            get
            {
                biaDtoFieldAttributeProperties = biaDtoFieldAttributeProperties ?? GenerateBiaDtoFieldAttributeProperties();
                return biaDtoFieldAttributeProperties;
            }
        }

        private string GenerateBiaDtoFieldAttributeProperties()
        {
            var attributeProperties = new List<string>
            {
                $"Required = {IsRequired.ToString().ToLower()}"
            };

            if (IsParent)
            {
                attributeProperties.Add($"IsParent = true");
            }

            if (IsOption || IsOptionCollection)
            {
                attributeProperties.Add($"ItemType = \"{OptionType}\"");
            }

            if (!string.IsNullOrWhiteSpace(MappingDateType))
            {
                attributeProperties.Add($"Type = \"{MappingDateType.ToLower()}\"");
            }

            return string.Join(", ", attributeProperties);
        }

        public string GenerateMapperCSV()
        {
            string nonNullMappingType = NonNullMappingType;

            if (nonNullMappingType == "bool")
            {
                return $"CSVBool(x.{MappingName})";
            }

            if (
                nonNullMappingType == "int"
                || nonNullMappingType == "double"
                || nonNullMappingType == "decimal"
                || nonNullMappingType == "float"
                || nonNullMappingType == "uint"
                || nonNullMappingType == "long"
                || nonNullMappingType == "ulong"
                || nonNullMappingType == "short"
                || nonNullMappingType == "ushort"
                )
            {
                return $"CSVNumber(x.{MappingName})";
            }

            if (!string.IsNullOrWhiteSpace(MappingDateType))
            {
                switch (MappingDateType)
                {
                    case "datetime":
                        return $"CSVDateTime(x.{MappingName})";
                    case "date":
                        return $"CSVDate(x.{MappingName})";
                    case "time":
                        return $"CSVTime(x.{MappingName})";
                    default:
                        throw new InvalidOperationException($"Unable to get CSV method for mapping date type {MappingDateType}");
                }
            }

            if (nonNullMappingType == "string")
            {
                return $"CSVString(x.{MappingName})";
            }

            if (IsOption)
            {
                return $"CSVString(x.{MappingName}?.Display)";
            }

            if (IsOptionCollection)
            {
                return $"CSVList(x.{MappingName})";
            }

            return $"CSVString(x.{MappingName}.ToString())";
        }

        public string GenerateGetSetComment(string entityName)
        {
            throw new NotImplementedException();
        }
    }
}
