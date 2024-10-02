namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    public class PropertyModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOption { get; set; }
        public string OptionType { get; set; }
        public bool IsRequired { get; set; }
        public string OptionDisplayProperty { get; set; }
    }
}