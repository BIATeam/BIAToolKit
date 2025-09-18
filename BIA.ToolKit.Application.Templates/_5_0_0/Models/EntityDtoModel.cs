namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityDtoModel<TPropertyDtoModel> : _4_0_0.Models.EntityDtoModel<TPropertyDtoModel>
        where TPropertyDtoModel : class, IPropertyDtoModel
    {
        protected override List<string> ExcludedPropertiesToGenerate
        {
            get
            {
                _ = base.ExcludedPropertiesToGenerate;

                if (IsTeamType)
                {
                    excludedPropertiesToGenerate.Add("UserDefaultTeams");
                }

                return excludedPropertiesToGenerate;
            }
        }

        public override string GetClassInheritance()
        {
            if (IsTeamType)
            {
                if (IsFixable && IsArchivable)
                    return "BaseDtoVersionedTeamFixableArchivable";
                if (IsFixable)
                    return "BaseDtoVersionedTeamFixable";
                return "BaseDtoVersionedTeam";
            }

            if (IsVersioned && IsFixable && IsArchivable)
                return $"BaseDtoVersionedFixableArchivable<{BaseKeyType}>";
            if (IsVersioned && IsFixable)
                return $"BaseDtoVersionedFixable<{BaseKeyType}>";
            if (IsVersioned)
                return $"BaseDtoVersioned<{BaseKeyType}>";

            return $"BaseDto<{BaseKeyType}>";
        }

        public TPropertyDtoModel ParentProperty => Properties.FirstOrDefault(p => p.IsParent);
        public bool HasParent => ParentProperty != null;
    }
}
