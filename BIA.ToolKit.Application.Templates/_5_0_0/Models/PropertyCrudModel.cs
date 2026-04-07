namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class PropertyCrudModel : IPropertyCrudModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<KeyValuePair<string, string>> BiaFieldAttributes { get; set; } = [];

        public bool IsRequired => !IsNullable;
        public bool IsOption => Type.StartsWith("OptionDto") || Type.StartsWith("ICollection<OptionDto>");
        public bool IsCollection => Type.StartsWith("ICollection");
        public bool IsNullable => Type.EndsWith("?") || !BiaFieldAttributes.Any(x => x.Key == "Required" && x.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase));
        public bool IsDecimal => Type.StartsWith("decimal", StringComparison.InvariantCultureIgnoreCase);
        public string OptionItemType => BiaFieldAttributes.SingleOrDefault(x => x.Key == "ItemType").Value;
        public bool IsParentIdentifier => BiaFieldAttributes.Any(x => x.Key == "IsParent" && x.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase));
        public bool AsLocalDateTime => BiaFieldAttributes.Any(x => x.Key == "AsLocalDateTime" && x.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase));

        private string angularType;
        public string AngularType
        {
            get
            {
                angularType ??= GetAngularType();

                return angularType;
            }
        }

        private string angularPropType;
        public string AngularPropType
        {
            get
            {
                angularPropType ??= GetAngularPropType();

                return angularPropType;
            }
        }

        public bool HasAngularPropType => !string.IsNullOrWhiteSpace(AngularPropType);

        private string angularValidators;

        public string AngularValidators
        {
            get
            {
                angularValidators ??= GetAngularValidators();

                return angularValidators;
            }
        }

        public bool HasAngularValidators => !string.IsNullOrWhiteSpace(AngularValidators);


        private string GetAngularType()
        {
            string baseType = Type;
            switch (baseType.Replace("?", string.Empty).ToLower())
            {
                case "string":
                    baseType = "string";
                    break;
                case "bool":
                    baseType = "boolean";
                    break;
                case "int":
                case "long":
                case "short":
                case "uint":
                case "ulong":
                case "ushort":
                case "decimal":
                case "double":
                case "float":
                    baseType = "number";
                    break;
                case "datetime":
                    baseType = "Date";
                    break;
                default:
                    break;
            }

            if (IsCollection)
                baseType = baseType
                    .Replace("ICollection", string.Empty)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty);

            if (IsNullable)
                baseType = baseType.Replace("?", string.Empty);

            string collectionDefinition = IsCollection ? "[]" : string.Empty;
            string nullableDefinition = IsNullable ? " | null" : string.Empty;

            return baseType + collectionDefinition + nullableDefinition;
        }

        private string GetAngularPropType()
        {
            string angularType = GetAngularType().Split('|').First().Trim();
            string biaFieldType = BiaFieldAttributes.SingleOrDefault(x => x.Key == "Type").Value;
            return angularType switch
            {
                "string" => biaFieldType switch
                {
                    "time" => "TimeSecOnly",
                    _ => "String",
                },
                "boolean" => "Boolean",
                "number" => "Number",
                "Date" => biaFieldType switch
                {
                    "datetime" => "DateTime",
                    "date" => "Date",
                    "time" => "Time",
                    _ => string.Empty,
                },
                "OptionDto" => "OneToMany",
                "OptionDto[]" => "ManyToMany",
                _ => string.Empty,
            };
        }

        private string GetAngularValidators()
        {
            var validators = new List<string>();

            return string.Join(", ", validators);
        }
    }
}
