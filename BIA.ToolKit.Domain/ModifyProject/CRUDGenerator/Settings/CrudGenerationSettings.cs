namespace BIA.ToolKit.Application.Settings
{
    using System.Collections.Generic;

    public class CrudGenerationSettings
    {
        public string Feature { get; set; }
        public string Type { get; set; }
        public string FeatureName { get; set; }
        public string FeatureNamePlural { get; set; }
        public string ZipName { get; set; }
        public Contains Contains { get; }
        public List<string> Children { get; }
        public List<string> Options { get; }
        public List<string> Partial { get; }
        public List<FeatureParent> Parents { get; }

        public CrudGenerationSettings()
        {
            Children = new();
            Options = new();
            Partial = new();
            Parents = new();
        }
    }

    public class Contains
    {
        public List<string> Include { get; set; }
        public List<string> Exclude { get; set; }
    }

    public class FeatureParent
    {
        public string Name { get; set; }
        public string NamePlural { get; set; }
        public string DomainName { get; set; }
        public bool IsPrincipal { get; set; }
        public List<FeatureAdaptPath> AdaptPaths { get; set; } = new();
    }

    public class FeatureAdaptPath
    {
        public string RootPath { get; set; }
        public int InitialDeepLevel { get; set; }
        public string DeepLevelIdentifier { get; set; }
        public List<FeatureMoveFiles> MoveFiles { get; set; } = new();
        public List<FeatureReplaceInFiles> ReplaceInFiles { get; set; } = new();
    }

    public class FeatureMoveFiles
    {
        public string FromRelativePath { get; set; }
        public string ToRelativePathNoParent { get; set; }
        public string ToRelativePathWithParent { get; set; }
    }

    public class FeatureReplaceInFiles
    {
        public string RegexMatch { get; set; }
        public string Pattern { get; set; }
        public string NoParentValue { get; set; }
        public string WithParentValue { get; set; }
        public string WithParentAddByDeeperLevel { get; set; }
    }
}
