namespace BIA.ToolKit.Application.Templates.Common.Interfaces
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public interface IEntityCrudModel<TPropertyCrudModel> : IEntityModel
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        string AncestorTeamName { get; set; }
        int AngularDeepLevel { get; set; }
        string AngularDeepRelativePath { get; }
        string AngularParentRelativePath { get; set; }
        IEnumerable<TPropertyCrudModel> BiaFieldConfigProperties { get; }
        string DisplayItemName { get; set; }
        bool HasAncestorTeam { get; set; }
        bool HasOptions { get; }
        bool HasParent { get; set; }
        bool IsTeam { get; set; }
        List<string> OptionItems { get; set; }
        string ParentName { get; set; }
        string ParentNamePlural { get; set; }
        List<TPropertyCrudModel> Properties { get; set; }
    }
}