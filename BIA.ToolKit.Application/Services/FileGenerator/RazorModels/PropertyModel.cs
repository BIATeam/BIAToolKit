namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    using System.Collections.Generic;
    using System.Text;

    public class PropertyModel
    {
        public string MappingName { get; set; }
        public string EntityCompositeName { get; set; }
        public string MappingType { get; set; }
        public bool IsOption { get; set; }
        public string OptionType { get; set; }
        public bool IsRequired { get; set; }
        public string OptionDisplayProperty { get; set; }
        public string OptionIdProperty { get; set; }
        public string OptionEntityIdPropertyComposite { get; set; }
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

            if (IsOption)
            {
                attributeProperties.Add($"ItemType = \"{OptionType}\"");
            }

            return string.Join(", ", attributeProperties);
        }
    }
}