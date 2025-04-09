namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PropertyModel
    {
        public string MappingName { get; set; }
        public string EntityCompositeName { get; set; }
        public string MappingType { get; set; }
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
            string nonNullMappingType = MappingType.Replace("?", string.Empty);

            if (nonNullMappingType == "bool")
            {
                return $"CSVBool(x.{MappingName}),";
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
                return $"CSVNumber(x.{MappingName}),";
            }

            if (!string.IsNullOrWhiteSpace(MappingDateType))
            {
                switch (MappingDateType)
                {
                    case "Datetime":
                        return $"CSVDateTime(x.{MappingName}),";
                    case "Date":
                        return $"CSVDate(x.{MappingName}),";
                    case "Time":
                        return $"CSVTime(x.{MappingName}),";
                    default:
                        throw new InvalidOperationException($"Unable to get CSV method for mapping date type {MappingDateType}");
                }
            }

            if (nonNullMappingType == "string")
            {
                return $"CSVString(x.{MappingName}),";
            }

            return $"CSVString(x.{MappingName}.ToString()),";
        }
    }
}
