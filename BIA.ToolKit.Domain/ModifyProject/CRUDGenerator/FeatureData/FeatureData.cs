namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData
{
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock;
    using System.Collections.Generic;

    public class FeatureData
    {
        /// <summary>
        /// File name (only).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File full path on Temporary working directory.
        /// </summary>
        public string FilePath { get; }

        public bool IsPartialFile { get; } = false;

        public bool IsPropertyFile { get; set; } = false;

        /// <summary>
        /// Temporary working directory full path.
        /// </summary>
        public string ExtractDirPath { get; }

        /// <summary>
        /// List of ExtractBlocks.
        /// </summary>
        public List<ExtractBlock> ExtractBlocks { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FeatureData(string fileName, string filePath, string tmpDir)
        {
            FileName = fileName;
            FilePath = filePath;
            ExtractDirPath = tmpDir;
            IsPartialFile = fileName.EndsWith(Constants.PartialFileSuffix);
        }
    }


}
