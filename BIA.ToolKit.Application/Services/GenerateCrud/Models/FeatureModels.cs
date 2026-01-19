namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.ExtractBlock;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    public class ZipFeatureType
    {
        public ZipFeatureType(FeatureType type, GenerationType generation, string zipName, string zipPath, string feature, List<FeatureParent> parents, bool needParent, List<FeatureAdaptPath> adaptPaths, string domain)
        {
            FeatureType = type;
            GenerationType = generation;
            ZipName = zipName;
            ZipPath = zipPath;
            Feature = feature;
            Parents = parents;
            NeedParent = needParent;
            AdaptPaths = adaptPaths;
            Domain = domain;
        }

        public bool IsChecked { get; set; }

        public FeatureType FeatureType { get; }

        public GenerationType GenerationType { get; }

        public string ZipName { get; }

        public string ZipPath { get; }

        public string Feature { get; }

        public List<FeatureData> FeatureDataList { get; set; }

        public List<FeatureParent> Parents { get; set; }

        public bool NeedParent { get; set; }

        public List<FeatureAdaptPath> AdaptPaths { get; set; }

        public string Domain { get; set; }
    }

    public class WebApiNamespace
    {
        public WebApiNamespace(WebApiFileType fileType, string crudNamespace)
        {
            FileType = fileType;
            CrudNamespace = crudNamespace;
        }

        public WebApiFileType FileType { get; }

        public string CrudNamespace { get; }

        public string CrudNamespaceGenerated { get; set; }
    }

    public class CrudParent
    {
        public bool Exists { get; set; }
        public string Name { get; set; }
        public string NamePlural { get; set; }
        public string Domain { get; set; }
    }
}
