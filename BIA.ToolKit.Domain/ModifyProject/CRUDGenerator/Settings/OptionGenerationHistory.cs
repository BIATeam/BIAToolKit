namespace BIA.ToolKit.Application.Settings
{
    using System;
    using System.Collections.Generic;

    public class OptionGeneration
    {
        public List<OptionGenerationHistory> OptionGenerationHistory { get; }

        public OptionGeneration()
        {
            OptionGenerationHistory = new();
        }
    }

    public class OptionGenerationHistory
    {
        public DateTime Date { get; set; }
        public string EntityNameSingular { get; set; }
        public string EntityNamePlural { get; set; }
        public string DisplayItem { get; set; }
        public string Domain { get; set; }
        public List<Generation> Generation { get; }
        public EntityMapping Mapping { get; set; }


        public OptionGenerationHistory()
        {
            Generation = new();
        }
    }

    public class EntityMapping
    {
        public string Entity { get; set; }
        public string Type { get; set; }
    }
}
