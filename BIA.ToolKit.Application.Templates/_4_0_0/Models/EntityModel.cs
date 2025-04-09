using System;
using System.Collections.Generic;
using System.Linq;

namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    public class EntityModel
    {
        public const string OptionDto = "OptionDto";
        public const string CollectionOptionDto = "ICollection<" + OptionDto + ">";

        private readonly List<string> excludedDtoPropertiesToGenerate = new List<string>
        {
            "Id",
            "IsFixed"
        };

        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string EntityNameArticle { get; set; }
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public IEnumerable<PropertyModel> DtoPropertiesToGenerate => Properties.Where(p => !excludedDtoPropertiesToGenerate.Contains(p.MappingName));
        public bool HasCollectionOptions => Properties.Any(p => p.MappingType.Equals(CollectionOptionDto));
        public bool HasOptions => HasCollectionOptions || Properties.Any(p => p.MappingType.Equals(OptionDto));
        public string BaseKeyType { get; set; }
        public bool IsTeamType { get; set; }
    }
}
