namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings
{
    using System;
    using System.Collections.Generic;

    public class OptionGeneration
    {
        public List<OptionGenerationHistory> OptionGenerationHistory { get; }

        public OptionGeneration()
        {
            OptionGenerationHistory = [];
        }
    }

    public class OptionGenerationHistory : GenerationHistory
    {
        public string DisplayItem { get; set; }
        public string Domain { get; set; }
        public EntityMapping Mapping { get; set; }
        public string BiaFront { get; set; }
        public bool UseHubClient { get; set; }

        /// <summary>
        /// Full namespace of the option entity class (e.g. "Acme.MyApp.Domain.Orders.Entities").
        /// Used by the RegenerateFeatures discovery service to locate the entity file without
        /// requiring project-level metadata (CompanyName / ProjectName).
        /// </summary>
        public string EntityNamespace { get; set; }
    }

    public class EntityMapping
    {
        public string Entity { get; set; }
        public string Type { get; set; }
    }
}
