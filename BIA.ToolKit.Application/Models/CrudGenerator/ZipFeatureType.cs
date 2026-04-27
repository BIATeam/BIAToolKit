namespace BIA.ToolKit.Application.Models.CrudGenerator
{
    using System.Collections.Generic;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Constructor.
    /// </summary>
    public class ZipFeatureType(FeatureType type, GenerationType generation, string zipName, string zipPath, string feature, List<FeatureParent> parents, bool needParent, List<FeatureAdaptPath> adaptPaths, string domain)
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The Feature type.
        /// </summary>
        public FeatureType FeatureType { get; } = type;

        /// <summary>
        /// The Generation type.
        /// </summary>
        public GenerationType GenerationType { get; } = generation;

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; } = zipName;

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; } = zipPath;

        /// <summary>
        /// Name of the feature associated to the ZIP
        /// </summary>
        public string Feature { get; } = feature;

        public List<FeatureData> FeatureDataList { get; set; }

        /// <summary>
        /// Parents of the feature
        /// </summary>
        public List<FeatureParent> Parents { get; set; } = parents;

        /// <summary>
        /// Indicates if the feature needs a parent
        /// </summary>
        public bool NeedParent { get; set; } = needParent;

        /// <summary>
        /// The adapt paths to apply
        /// </summary>
        public List<FeatureAdaptPath> AdaptPaths { get; set; } = adaptPaths;

        /// <summary>
        /// The feature domain
        /// </summary>
        public string Domain { get; set; } = domain;
    }
}
