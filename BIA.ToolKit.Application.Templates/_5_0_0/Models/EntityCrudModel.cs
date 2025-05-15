namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : _4_0_0.Models.EntityCrudModel<TPropertyCrudModel>
         where TPropertyCrudModel : class, IPropertyCrudModel
    {
    }
}
