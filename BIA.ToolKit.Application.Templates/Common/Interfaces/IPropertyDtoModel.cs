namespace BIA.ToolKit.Application.Templates.Common.Interfaces
{
    public interface IPropertyDtoModel
    {
        string BiaDtoFieldAttributeProperties { get; }
        string EntityCompositeName { get; set; }
        string EntityType { get; set; }
        bool IsOption { get; set; }
        bool IsOptionCollection { get; set; }
        bool IsParent { get; set; }
        bool IsRequired { get; set; }
        string MappingDateType { get; set; }
        string MappingName { get; set; }
        string MappingType { get; set; }
        string MappingTypeInDto { get; }
        string NonNullEntityType { get; }
        string NonNullMappingType { get; }
        string OptionDisplayProperty { get; set; }
        string OptionEntityIdPropertyComposite { get; set; }
        string OptionIdProperty { get; set; }
        string OptionRelationFirstIdProperty { get; set; }
        string OptionRelationPropertyComposite { get; set; }
        string OptionRelationSecondIdProperty { get; set; }
        string OptionRelationType { get; set; }
        string OptionType { get; set; }

        string GenerateGetSetComment(string entityName);
        string GenerateMapperCSV();
    }
}