namespace BIA.ToolKit.Application.Templates._5_0_0.Mocks
{
    using BIA.ToolKit.Application.Templates._5_0_0.Models;

    public class EntityOptionMock : EntityOptionModel
    {
        public EntityOptionMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "AircraftMaintenanceCompany";
            EntityName = "MyCountry";
            BaseKeyType = "int";
            EntityNamePlural = "MyCountries";
            OptionDisplayName = "Name";
        }
    }
}
