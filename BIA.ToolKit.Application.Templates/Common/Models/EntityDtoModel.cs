namespace BIA.ToolKit.Application.Templates.Common.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityDtoModel<TPropertyDtoModel> : EntityModel, IEntityDtoModel<TPropertyDtoModel>
        where TPropertyDtoModel : class, IPropertyDtoModel
    {
        public const string OptionDto = "OptionDto";
        public const string CollectionOptionDto = "ICollection<" + OptionDto + ">";

        protected readonly List<string> excludedPropertiesToGenerate = new List<string>
        {
            "Id",
            "IsFixed"
        };

        public List<TPropertyDtoModel> Properties { get; set; } = new List<TPropertyDtoModel>();
        public IEnumerable<TPropertyDtoModel> PropertiesToGenerate => Properties.Where(p => !excludedPropertiesToGenerate.Contains(p.MappingName));
        public bool HasCollectionOptions => Properties.Any(p => p.MappingType.Equals(CollectionOptionDto));
        public bool HasTimeSpanProperty => Properties.Any(p => p.EntityType.Equals("TimeSpan") || p.EntityType.Equals("TimeSpan?"));


        public bool HasOptions => HasCollectionOptions || Properties.Any(p => p.MappingType.Equals(OptionDto));
        public string AncestorTeam { get; set; }
        public bool HasAncestorTeam => !string.IsNullOrWhiteSpace(AncestorTeam);
    }
}
