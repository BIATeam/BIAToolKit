namespace BIA.ToolKit.Application.Settings
{
    using System;
    using System.Collections.Generic;

    public class CRUDGeneration
    {
        public List<CRUDGenerationHistory> CRUDGenerationHistory { get; }

        public CRUDGeneration()
        {
            CRUDGenerationHistory = new();
        }
    }

    public class CRUDGenerationHistory
    {
        public DateTime Date { get; set; }
        public string EntityNameSingular { get; set; }
        public string EntityNamePlural { get; set; }
        public string DisplayItem { get; set; }
        public List<string> OptionItems { get; set; }
        public Mapping Mapping { get; set; }
        public List<Generation> Generation { get; }
        public string Feature { get; set; }

        public CRUDGenerationHistory()
        {
            Generation = new();
        }
    }

    public class Generation
    {
        public string GenerationType { get; set; }
        public string FeatureType { get; set; }
        public string Folder { get; set; }
        public string Template { get; set; }
        public string Type { get; set; }
    }

    public class Mapping
    {
        public string Dto { get; set; }
        public string Type { get; set; }
    }
}
