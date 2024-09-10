﻿namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock
{
    using System.Collections.Generic;

    public class ExtractPartialBlock : ExtractBlock
    {
        public string Index { get; }

        public List<ExtractPartialBlock> NestedBlocks { get; }

        public ExtractPartialBlock ParentBlock { get; private set; }

        public ExtractPartialBlock(CRUDDataUpdateType dataUpdateType, string name, string index) :
            base(dataUpdateType, name, new())
        {
            Index = index;
            NestedBlocks = new();
        }

        public void AddLine(string line)
        {
            BlockLines.Add(line);
            AddLineToParentRecursive(line, ParentBlock);
        }

        private static void AddLineToParentRecursive(string line, ExtractPartialBlock parent)
        {
            if (parent == null)
                return;

            parent.BlockLines.Add(line);
            AddLineToParentRecursive(line, parent.ParentBlock);
        }

        public void AddNestedBlock(ExtractPartialBlock nestedBlock)
        {
            nestedBlock.ParentBlock = this;
            NestedBlocks.Add(nestedBlock);
        }

        public ExtractPartialBlock GetLastNestedBlock()
        {
            return GetLastNestedBlockRecursive(this);
        }

        private static ExtractPartialBlock GetLastNestedBlockRecursive(ExtractPartialBlock extractPartialBlock)
        {
            if (!extractPartialBlock.NestedBlocks.Any())
                return extractPartialBlock;

            return GetLastNestedBlockRecursive(extractPartialBlock.NestedBlocks.Last());
        }
    }
}
