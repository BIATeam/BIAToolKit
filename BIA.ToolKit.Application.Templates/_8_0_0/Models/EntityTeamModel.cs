namespace BIA.ToolKit.Application.Templates._8_0_0.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityTeamModel : IEntityTeamModel
    {
        public string BaseKeyType { get; set; }
        public string CompanyName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string EntityNameArticle { get; set; }
        public string EntityNamePlural { get; set; }
        public bool IsTeamType { get; set; }
        public string ProjectName { get; set; }
    }
}
