namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractDisplayBlock : ExtractBlock
    {
        public string ExtractLine { get; set; }

        public string ExtractItem { get; set; }

        public ExtractDisplayBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) :
            base(dataUpdateType, name, lines)
        { }
    }
}
