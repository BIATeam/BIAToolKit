namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractPartialBlock : ExtractBlock
    {
        public string Index { get; }

        public ExtractPartialBlock(CRUDDataUpdateType dataUpdateType, string name, string index, List<string> lines) :
            base(dataUpdateType, name, lines)
        {
            Index = index;
        }
    }
}
