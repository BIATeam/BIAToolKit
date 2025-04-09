namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EntityDtoModel : EntityModel
    {
        public const string OptionDto = "OptionDto";
        public const string CollectionOptionDto = "ICollection<" + OptionDto + ">";

        private readonly List<string> excludedDtoPropertiesToGenerate = new List<string>
        {
            "Id",
            "IsFixed"
        };

        public IEnumerable<PropertyModel> DtoPropertiesToGenerate => Properties.Where(p => !excludedDtoPropertiesToGenerate.Contains(p.MappingName));
        public bool HasCollectionOptions => Properties.Any(p => p.MappingType.Equals(CollectionOptionDto));
        public bool HasOptions => HasCollectionOptions || Properties.Any(p => p.MappingType.Equals(OptionDto));
    }
}
