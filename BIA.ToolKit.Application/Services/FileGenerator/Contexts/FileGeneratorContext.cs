namespace BIA.ToolKit.Application.Services.FileGenerator.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
    }
}
