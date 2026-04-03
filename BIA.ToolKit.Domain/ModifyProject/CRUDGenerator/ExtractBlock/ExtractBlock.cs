namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;

    public class ExtractBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines)
    {
        public CRUDDataUpdateType DataUpdateType { get; } = dataUpdateType;

        public string Name { get; } = name;

        public List<string> BlockLines { get; } = lines;
    }
}
