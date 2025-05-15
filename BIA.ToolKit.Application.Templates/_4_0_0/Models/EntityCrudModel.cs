namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : Common.Models.EntityCrudModel<TPropertyCrudModel>
         where TPropertyCrudModel : class, IPropertyCrudModel
    {
    }
}
