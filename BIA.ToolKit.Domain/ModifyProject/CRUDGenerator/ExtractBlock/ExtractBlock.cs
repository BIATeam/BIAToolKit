namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;

    public class ExtractBlock
    {
        public CRUDDataUpdateType DataUpdateType { get; }

        public string Name { get; }

        public List<string> BlockLines { get; }

        public ExtractBlock(CRUDDataUpdateType dataUpdateType, string name, List<string> lines)
        {
            DataUpdateType = dataUpdateType;
            Name = name;
            BlockLines = lines;
        }
    }
}
