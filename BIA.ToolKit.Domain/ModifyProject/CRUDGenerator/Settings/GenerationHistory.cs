namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings
{
    using System;
    using System.Collections.Generic;

    public abstract class GenerationHistory
    {
        public DateTime Date { get; set; }
        public string EntityNameSingular { get; set; }
        public string EntityNamePlural { get; set; }
        public List<Generation> Generation { get; }

        protected GenerationHistory()
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
}
