namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractPropertiesBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) : ExtractBlock(dataUpdateType, name, lines)
    {
        public List<CRUDPropertyType> PropertiesList { get; set; }
    }
}
