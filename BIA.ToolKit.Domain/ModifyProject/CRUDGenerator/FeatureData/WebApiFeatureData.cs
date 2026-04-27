namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData
{
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using System.Collections.Generic;

    /// <summary>
    /// Constructor.
    /// </summary>
    public class WebApiFeatureData(string fileName, string filePath, string tmpDir, WebApiFileType? type) : FeatureData(fileName, filePath, tmpDir)
    {
        /// <summary>
        /// File type.
        /// </summary>
        public WebApiFileType? FileType { get; } = type;

        /// <summary>
        /// List of Options to delete.
        /// </summary>
        public ClassDefinition ClassFileDefinition { get; set; }

        public string Namespace { get; set; }

        public List<PropertyInfo> PropertiesInfos { get; set; }
    }
}
