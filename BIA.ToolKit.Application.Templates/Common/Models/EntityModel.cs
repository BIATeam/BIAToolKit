using BIA.ToolKit.Application.Templates.Common.Interfaces;

namespace BIA.ToolKit.Application.Templates.Common.Models
{
    public abstract class EntityModel : DotNetModel, IEntityModel
    {
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string EntityNameArticle { get; set; }
        public bool IsTeamType { get; set; }
        public string BaseKeyType { get; set; }
        public string EntityNamePlural { get; set; }
    }
}
