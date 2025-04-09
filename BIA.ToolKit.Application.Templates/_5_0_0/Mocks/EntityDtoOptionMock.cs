namespace BIA.ToolKit.Application.Templates._5_0_0.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates._5_0_0.Models;

    public class EntityOptionMock : EntityOptionModel
    {
        public EntityOptionMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "AircraftMaintenanceCompany";
            EntityName = "Country";
            BaseKeyType = "int";
            EntityNamePlural = "Countries";
            OptionDisplayName = "Name";
        }
    }
}
