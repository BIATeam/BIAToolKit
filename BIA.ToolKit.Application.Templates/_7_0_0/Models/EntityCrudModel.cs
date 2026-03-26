namespace BIA.ToolKit.Application.Templates._7_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : _6_0_0.Models.EntityCrudModel<TPropertyCrudModel>
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        public override bool HasListAndItemModels { get; set; } = false;
        public override List<TPropertyCrudModel> ListProperties { get; set; } = new List<TPropertyCrudModel>();
    }
}
