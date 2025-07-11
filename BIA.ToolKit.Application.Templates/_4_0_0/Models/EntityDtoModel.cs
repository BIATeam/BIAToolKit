﻿namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityDtoModel<TPropertyDtoModel> : Common.Models.EntityModel, IEntityDtoModel<TPropertyDtoModel>
            where TPropertyDtoModel : class, IPropertyDtoModel
    {
        public const string OptionDto = "OptionDto";
        public const string CollectionOptionDto = "ICollection<" + OptionDto + ">";

        protected virtual List<string> ExcludedPropertiesToGenerate => new List<string>
        {
            "Id",
            "IsFixed"
        };

        public List<TPropertyDtoModel> Properties { get; set; } = new List<TPropertyDtoModel>();
        public IEnumerable<TPropertyDtoModel> PropertiesToGenerate => Properties.Where(p => !ExcludedPropertiesToGenerate.Contains(p.MappingName));
        public bool HasCollectionOptions => Properties.Any(p => p.MappingType.Equals(CollectionOptionDto));
        public bool HasTimeSpanProperty => Properties.Any(p => p.EntityType.Equals("TimeSpan") || p.EntityType.Equals("TimeSpan?"));


        public bool HasOptions => HasCollectionOptions || Properties.Any(p => p.MappingType.Equals(OptionDto));
        public string AncestorTeam { get; set; }
        public bool HasAncestorTeam => !string.IsNullOrWhiteSpace(AncestorTeam);
        public bool IsArchivable { get; set; }
        public bool IsFixable { get; set; }
        public bool IsVersioned { get; set; }

        public virtual string GetClassInheritance()
        {
            var types = new List<string>();
            if(IsTeamType)
            {
                types.Add("TeamDto");
            }
            else
            {
                types.Add($"BaseDto<{BaseKeyType}>");
            }

            if (IsFixable)
            {
                types.Add("IFixableDto");
            }

            if (IsArchivable)
            {
                types.Add("IArchivableDto");
            }

            return string.Join(", ", types);
        }
    }
}
