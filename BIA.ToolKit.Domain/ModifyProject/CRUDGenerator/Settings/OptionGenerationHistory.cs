namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings
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

    public class OptionGenerationHistory : GenerationHistory
    {
        public string DisplayItem { get; set; }
        public string Domain { get; set; }
        public EntityMapping Mapping { get; set; }
        public string BiaFront { get; set; }
    }

    public class EntityMapping
    {
        public string Entity { get; set; }
        public string Type { get; set; }
    }
}
