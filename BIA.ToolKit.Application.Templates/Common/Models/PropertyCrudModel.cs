namespace BIA.ToolKit.Application.Templates.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class PropertyCrudModel : IPropertyCrudModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<KeyValuePair<string, string>> BiaFieldAttributes { get; set; } = new List<KeyValuePair<string, string>>();

        public bool IsRequired => !IsNullable || BiaFieldAttributes.Any(x => x.Key == "IsRequired" && x.Value == "true");
        public bool IsOption => Type.StartsWith("OptionDto") || Type.StartsWith("ICollection<OptionDto>");
        public bool IsCollection => Type.StartsWith("ICollection");
        public bool IsNullable => Type.EndsWith("?");
        public bool IsDecimal => Type.StartsWith("decimal", StringComparison.InvariantCultureIgnoreCase);
        public string OptionItemType => BiaFieldAttributes.SingleOrDefault(x => x.Key == "ItemType").Value;
        public bool IsParentIdentifier => BiaFieldAttributes.Any(x => x.Key == "IsParent" && x.Value == "true");

        private string angularType;
        public string AngularType
        {
            get
            {
                if (angularType == null)
                    angularType = GetAngularType();

                return angularType;
            }
        }

        private string angularPropType;
        public string AngularPropType
        {
            get
            {
                if (angularPropType == null)
                    angularPropType = GetAngularPropType();

                return angularPropType;
            }
        }

        public bool HasAngularPropType => !string.IsNullOrWhiteSpace(AngularPropType);

        private string angularValidators;

        public string AngularValidators
        {
            get
            {
                if (angularValidators == null)
                    angularValidators = GetAngularValidators();

                return angularValidators;
            }
        }

        public bool HasAngularValidators => !string.IsNullOrWhiteSpace(AngularValidators);


        private string GetAngularType()
        {
            var baseType = Type;
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

            var collectionDefinition = IsCollection ? "[]" : string.Empty;
            var nullableDefinition = IsNullable ? " | null" : string.Empty;

            return baseType + collectionDefinition + nullableDefinition;
        }

        private string GetAngularPropType()
        {
            var angularType = GetAngularType().Split('|').First().Trim();
            var biaFieldType = BiaFieldAttributes.SingleOrDefault(x => x.Key == "Type").Value;
            switch (angularType)
            {
                case "string":
                    return "String";
                case "boolean":
                    return "Boolean";
                case "number":
                    return "Number";
                case "Date":
                    switch (biaFieldType)
                    {
                        case "datetime":
                            return "DateTime";
                        case "date":
                            return "Date";
                        case "time":
                            return "Time";
                        default:
                            return string.Empty;
                    }
                case "OptionDto":
                    return "OneToMany";
                case "OptionDto[]":
                    return "ManyToMany";
                default:
                    return string.Empty;
            }
        }

        private string GetAngularValidators()
        {
            var validators = new List<string>();

            if (IsRequired)
                validators.Add("Validators.required");

            return string.Join(", ", validators);
        }
    }
}
