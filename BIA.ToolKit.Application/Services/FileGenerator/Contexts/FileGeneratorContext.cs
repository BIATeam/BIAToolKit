namespace BIA.ToolKit.Application.Services.FileGenerator.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Application.Templates;

    public abstract class FileGeneratorContext()
    {
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string EntityNamePlural { get; set; }
        public string BaseKeyType { get; set; }
        public bool IsTeam { get; set; }
        public bool HasAncestorTeam { get; set; }
        public string AncestorTeamName { get; set; }
        public bool HasParent { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
        public string AngularFront { get; set; }
        public bool GenerateBack { get; set; }
        public bool GenerateFront { get; set; }
        public int AngularDeepLevel { get; private set; }
        public string AngularParentFolderRelativePath { get; private set; }
        public string AngularParentChildrenFolderRelativePath { get; private set; }

        public void ComputeAngularParentLocation(string projectFolder)
        {
            var parentRelativePathSearchRootFolder = Path.Combine(projectFolder, AngularFront, @"src\app\features\");
            var parentRelativePath = Directory.EnumerateDirectories(parentRelativePathSearchRootFolder, ParentNamePlural.ToKebabCase(), SearchOption.AllDirectories).SingleOrDefault();
            AngularParentFolderRelativePath = !string.IsNullOrWhiteSpace(parentRelativePath) ? parentRelativePath.Replace(parentRelativePathSearchRootFolder, string.Empty) : string.Empty;
            AngularParentChildrenFolderRelativePath = !string.IsNullOrWhiteSpace(AngularParentFolderRelativePath) ? Path.Combine(AngularParentFolderRelativePath, "children") : string.Empty;
            AngularDeepLevel = AngularParentChildrenFolderRelativePath.Split('\\').Count();
        }
    }
}
