namespace BIA.ToolKit.Application.Templates._7_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityDtoModel<TPropertyDtoModel> : _6_0_0.Models.EntityDtoModel<TPropertyDtoModel>
        where TPropertyDtoModel : class, IPropertyDtoModel
    {
        public override bool HasListAndItemModels { get; set; } = false;
        public override List<TPropertyDtoModel> ListProperties { get; set; } = new List<TPropertyDtoModel>();
    }
}
