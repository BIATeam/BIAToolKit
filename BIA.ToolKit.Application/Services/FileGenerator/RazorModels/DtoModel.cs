using System.Collections.Generic;

namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    public class DtoModel
    {
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string NameArticle { get; set; }
        public string DtoName { get; set; }
        public List<PropertyModel> Properties { get; set; } = new();
    }
}
