namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractPropertiesBlock : ExtractBlock
    {
        public List<CRUDPropertyType> PropertiesList { get; set; }

        public ExtractPropertiesBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) :
            base(dataUpdateType, name, lines)
        { }
    }
}
