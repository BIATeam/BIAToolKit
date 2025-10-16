namespace BIA.ToolKit.Application.Templates.Common.Interfaces
{
    using System.Collections.Generic;

    public interface IEntityDtoModel<TPropertyDtoModel> : IEntityModel
        where TPropertyDtoModel : class, IPropertyDtoModel
    {
        string AncestorTeam { get; set; }
        bool HasAncestorTeam { get; }
        bool HasCollectionOptions { get; }
        bool HasOptions { get; }
        bool HasTimeSpanProperty { get; }
        List<TPropertyDtoModel> Properties { get; set; }
        IEnumerable<TPropertyDtoModel> PropertiesToGenerate { get; }
        bool IsArchivable { get; set; }
        bool IsFixable { get; set; }
        bool IsVersioned { get; set; }
        string GetClassInheritance();
        bool HasAudit { get; set;}
    }
}