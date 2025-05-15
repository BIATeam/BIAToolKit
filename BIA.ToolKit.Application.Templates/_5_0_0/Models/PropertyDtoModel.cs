namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    public class PropertyDtoModel : _4_0_0.Models.PropertyDtoModel
    {
        public new string GenerateGetSetComment(string entityName)
        {
            string nonNullMappingType = NonNullMappingType;
            if (nonNullMappingType == "bool")
            {
                return "Gets or sets a value indicating whether the " + entityName + " " + MappingName.ToLiteral();
            }
            if (IsOptionCollection)
            {
                return "Gets or sets the list of " + MappingName.ToLiteral();
            }
            return "Gets or sets the " + MappingName.ToLiteral();
        }
    }
}
