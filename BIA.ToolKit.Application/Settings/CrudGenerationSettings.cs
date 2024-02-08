namespace BIA.ToolKit.Application.Settings
{
    using System.Collections.Generic;

    public class CrudGenerationSettings
    {
        public string Feature { get; set; }
        public string Type { get; set; }
        public string FeatureName { get; set; }
        public string FeatureNamePlurial { get; set; }
        public string ZipName { get; set; }
        public Contains Contains { get; }
        public List<string> Children { get; }
        public List<string> Options { get; }
        public List<string> Partial { get; }

        public CrudGenerationSettings()
        {
            Children = new();
            Options = new();
            Partial = new();
        }
    }

    public class Contains
    {
        public List<string> Include { get; set; }
        public List<string> Exclude { get; set; }
    }
}
