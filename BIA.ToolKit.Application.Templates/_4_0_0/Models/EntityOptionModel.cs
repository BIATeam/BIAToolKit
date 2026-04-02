namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

#pragma warning disable CS9266 // Les propriétés with setter utilisent throw NotImplementedException() au getter par design

    public class EntityOptionModel : Common.Models.EntityModel, IEntityOptionModel
    {
        public string OptionDisplayName { get => throw new System.NotImplementedException(); set; }

        public virtual bool UseHubForClient { get => throw new System.NotImplementedException(); set; }
    }
#pragma warning restore CS9266
}
