namespace BIA.ToolKit.Application.Templates._6_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : _5_0_0.Models.EntityCrudModel<TPropertyCrudModel>
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        public override string AngularModelInterfaceInheritance
        {
            get
            {
                var interfaces = new List<string>
                {
                    BaseKeyType == "string" || BaseKeyType == "Guid" ? "BaseDto<string>" : "BaseDto"
                };
                if (IsVersioned)
                    interfaces.Add("VersionedDto");
                if (IsTeam)
                    interfaces.Add("TeamDto");
                if (IsFixable)
                    interfaces.Add("FixableDto");
                if (IsArchivable)
                    interfaces.Add("ArchivableDto");
                return string.Join(", ", interfaces);
            }
        }

        public override bool HasAudit { get; set; }
    }
}
