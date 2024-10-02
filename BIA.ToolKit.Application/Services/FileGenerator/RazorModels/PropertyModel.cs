namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    using System.Collections.Generic;
    using System.Text;

    public class PropertyModel
    {
        public string Name { get; set; }
        public string CompositeName { get; set; }
        public string Type { get; set; }
        public bool IsOption { get; set; }
        public string OptionType { get; set; }
        public bool IsRequired { get; set; }
        public string OptionDisplayProperty { get; set; }

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
                $"Required = {IsRequired}"
            };

            if (IsOption)
            {
                attributeProperties.Add($"ItemType = {OptionType}");
            }

            return string.Join(", ", attributeProperties);
        }
    }
}