namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData
{
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock;
    using System.Collections.Generic;

    /// <summary>
    /// Constructor.
    /// </summary>
    public class FeatureData(string fileName, string filePath, string tmpDir)
    {
        /// <summary>
        /// File name (only).
        /// </summary>
        public string FileName { get; } = fileName;

        /// <summary>
        /// File full path on Temporary working directory.
        /// </summary>
        public string FilePath { get; } = filePath;

        public bool IsPartialFile { get; } = fileName.EndsWith(Constants.PartialFileSuffix);

        public bool IsPropertyFile { get; set; } = false;

        /// <summary>
        /// Temporary working directory full path.
        /// </summary>
        public string ExtractDirPath { get; } = tmpDir;

        /// <summary>
        /// List of ExtractBlocks.
        /// </summary>
        public List<ExtractBlock> ExtractBlocks { get; set; }
    }


}
