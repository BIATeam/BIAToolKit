namespace BIA.ToolKit.Application.Templates.Common.Interfaces
{
    using System.Collections.Generic;

    public interface IPropertyCrudModel
    {
        string AngularPropType { get; }
        string AngularType { get; }
        string AngularValidators { get; }
        List<KeyValuePair<string, string>> BiaFieldAttributes { get; set; }
        bool HasAngularPropType { get; }
        bool HasAngularValidators { get; }
        bool IsCollection { get; }
        bool IsDecimal { get; }
        bool IsNullable { get; }
        bool IsOption { get; }
        bool IsParentIdentifier { get; }
        bool IsRequired { get; }
        string Name { get; set; }
        string OptionItemType { get; }
        string Type { get; set; }
    }
}