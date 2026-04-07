namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractDisplayBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines) : ExtractBlock(dataUpdateType, name, lines)
    {
        public string ExtractLine { get; set; }

        public string ExtractItem { get; set; }
    }
}
