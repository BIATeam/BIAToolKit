namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractOptionFieldBlock : ExtractBlock
    {
        public string PropertyField { get; }

        public ExtractOptionFieldBlock(CRUDDataUpdateType dataUpdateType, string name, string propertyField, List<string> lines) :
            base(dataUpdateType, name, lines)
        {
            PropertyField = propertyField;
        }
    }
}
