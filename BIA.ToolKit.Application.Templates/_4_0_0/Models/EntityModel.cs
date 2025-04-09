using System;
using System.Collections.Generic;
using System.Linq;

namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    public class EntityModel
    {
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
        public string DomainName { get; set; }
        public string EntityName { get; set; }
        public string EntityNameArticle { get; set; }
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public bool IsTeamType { get; set; }
    }
}
