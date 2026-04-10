namespace BIA.ToolKit.Application.Templates._8_0_0.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates._8_0_0.Models;

    public class EntityTeamMock : EntityTeamModel
    {
        public EntityTeamMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "Fleet";
            EntityName = "Site";
            BaseKeyType = "int";
            EntityNamePlural = "Sites";
            IsTeamType = true;
        }
    }
}
