namespace BIA.ToolKit.Application.Templates._6_0_0.Models
{
    public class PropertyDtoModel : _4_0_0.Models.PropertyDtoModel
    {
        public override string GenerateMapperCSV()
        {
            return base.GenerateMapperCSV().Replace("(x", "(dto");
        }
    }
}
