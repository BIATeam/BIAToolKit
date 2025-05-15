namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityDtoModel<TPropertyDtoModel> : Common.Models.EntityDtoModel<TPropertyDtoModel>
            where TPropertyDtoModel : class, IPropertyDtoModel
    {
    }
}
