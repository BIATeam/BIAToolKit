namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    using System;
    using System.Collections.Generic;
    using System.Text;

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
                biaDtoFieldAttributeProperties ??= GenerateBiaDtoFieldAttributeProperties();
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
            if (MappingType == "bool")
            {
                return $"CSVBool(x.{MappingName}),";
            }

            if(
                MappingType == "int" 
                || MappingType == "double"
                || MappingType == "decimal"
                || MappingType == "float"
                || MappingType == "uint"
                || MappingType == "long"
                || MappingType == "ulong"
                || MappingType == "short"
                || MappingType == "ushort"
                )
            {
                return $"CSVNumber(x.{MappingName}),";
            }

            if (!string.IsNullOrWhiteSpace(MappingDateType))
            {
                return MappingDateType switch
                {
                    "Datetime" => $"CSVDateTime(x.{MappingName}),",
                    "Date" => $"CSVDate(x.{MappingName}),",
                    "Time" => $"CSVTime(x.{MappingName}),",
                    _ => throw new InvalidOperationException($"Unable to get CSV method for mapping date type {MappingDateType}")
                };
            }

            if(MappingType == "string")
            {
                return $"CSVString(x.{MappingName}),";
            }

            return $"CSVString(x.{MappingName}.ToString()),";
        }
    }
}