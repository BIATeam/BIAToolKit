namespace BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DtoGenerationHistory
    {
        public List<DtoGeneration> Generations { get; set; } = new();
    }

    public class DtoGeneration
    {
        public DateTime DateTime { get; set; }
        public string EntityName { get; set; }
        public string EntityNamespace { get; set; }
        public string Domain { get; set; }
        public List<DtoGenerationPropertyMapping> PropertyMappings { get; set; } = new();
    }

    public class DtoGenerationPropertyMapping
    {
        public string EntityPropertyCompositeName { get; set; }
        public string MappingName { get; set; }
        public bool IsRequired { get; set; }
        public string DateType { get; set; }
        public string OptionMappingEntityIdProperty { get; set; }
        public string OptionMappingIdProperty { get; set; }
        public string OptionMappingDisplayProperty { get; set; }
    }
}
