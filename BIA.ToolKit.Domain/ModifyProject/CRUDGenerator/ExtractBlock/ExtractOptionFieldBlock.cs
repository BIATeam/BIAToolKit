namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractOptionFieldBlock(CRUDDataUpdateType dataUpdateType, string name, string propertyField, List<string> lines) : ExtractBlock(dataUpdateType, name, lines)
    {
        public string PropertyField { get; } = propertyField;
    }
}
