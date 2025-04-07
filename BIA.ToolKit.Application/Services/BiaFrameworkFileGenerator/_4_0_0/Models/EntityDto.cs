using System.Collections.Generic;
using System.Linq;
using BIA.ToolKit.Common;

namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator._4_0_0.Models
{
    public class EntityDto
    {
        private readonly List<string> excludedPropertiesToGenerate = new()
        {
            "Id",
            "IsFixed"
        };
        /// <summary>
        /// Properties already included into BaseDto class
        /// </summary>
        public List<string> ExcludedPropertiesToGenerate { get => excludedPropertiesToGenerate; }

        public string EntityNamespace { get; set; }
        public string MapperName { get; set; }
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string NameArticle { get; set; }
        public string DtoName { get; set; }
        public List<PropertyModel> Properties { get; set; } = [];
        public IEnumerable<PropertyModel> FilteredProperties => Properties.Where(p => !ExcludedPropertiesToGenerate.Contains(p.MappingName));
        public bool HasCollectionOptions => Properties.Any(p => p.MappingType.Equals(Constants.BiaClassName.CollectionOptionDto));
        public bool HasOptions => HasCollectionOptions || Properties.Any(p => p.MappingType.Equals(Constants.BiaClassName.OptionDto));
        public string BaseKeyType { get; set; }
        public bool IsTeamType { get; set; }
    }
}
