namespace BIA.ToolKit.Application.Templates.Common.Models
{
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityOptionModel : EntityModel, IEntityOptionModel
    {
        public string OptionDisplayName { get; set; }
    }
}
