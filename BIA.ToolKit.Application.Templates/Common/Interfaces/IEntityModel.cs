namespace BIA.ToolKit.Application.Templates.Common.Interfaces
{
    public interface IEntityModel
    {
        string BaseKeyType { get; set; }
        string CompanyName { get; set; }
        string DomainName { get; set; }
        string EntityName { get; set; }
        string EntityNameArticle { get; set; }
        string EntityNamePlural { get; set; }
        bool IsTeamType { get; set; }
        string ProjectName { get; set; }
    }
}