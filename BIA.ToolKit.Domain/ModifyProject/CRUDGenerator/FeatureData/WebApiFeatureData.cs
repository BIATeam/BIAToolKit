namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData
{
    using BIA.ToolKit.Domain.CRUDGenerator;
    using BIA.ToolKit.Domain.DtoGenerator;
    using System.Collections.Generic;

    public class WebApiFeatureData : FeatureData
    {
        /// <summary>
        /// File type.
        /// </summary>
        public WebApiFileType? FileType { get; }

        /// <summary>
        /// List of Options to delete.
        /// </summary>
        public ClassDefinition ClassFileDefinition { get; set; }

        public string Namespace { get; set; }

        public List<PropertyInfo> PropertiesInfos { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WebApiFeatureData(string fileName, string filePath, string tmpDir, WebApiFileType? type) : base(fileName, filePath, tmpDir)
        {
            FileType = type;
        }
    }
}
