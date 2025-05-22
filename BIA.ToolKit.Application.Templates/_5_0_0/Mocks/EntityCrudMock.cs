namespace BIA.ToolKit.Application.Templates._5_0_0.Mocks
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates._5_0_0.Models;

    public class EntityCrudMock : EntityCrudModel<PropertyCrudModel>
    {
        public EntityCrudMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "MaintenanceCompanies";
            EntityName = "MaintenanceTeam";
            BaseKeyType = "int";
            EntityNamePlural = "MaintenanceTeams";
            IsTeam = true;
            HasAncestorTeam = true;
            AncestorTeamName = "Site";
            DisplayItemName = "Name";
            OptionItems = new List<string> { "Engine", "PlaneType" };
            HasParent = true;
            ParentName = "AircraftMaintenanceCompany";
            ParentNamePlural = "AircraftMaintenanceCompanies";
            AngularParentRelativePath = "aircraft-maintenance-companies";
            AngularDeepLevel = 2;
            Properties = new List<PropertyCrudModel>
            {
                 new PropertyCrudModel
                {
                    Name = "AircraftMaintenanceCompanyId",
                    Type = "int",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "true")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "Msn",
                    Type = "string",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "true")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "IsActive",
                    Type = "bool",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "true")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "FirstFlightDate",
                    Type = "DateTime",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "true"),
                        new KeyValuePair<string, string>("Type", "date")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "MotorsCount",
                    Type = "int?",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "false")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "SomeDecimal",
                    Type = "decimal",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("IsRequired", "false")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "Engines",
                    Type = "ICollection<OptionDto>",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ItemType", "Engine")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "PlaneType",
                    Type = "OptionDto",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ItemType", "PlaneType")
                    }
                },
                new PropertyCrudModel
                {
                    Name = "SimilarTypes",
                    Type = "ICollection<OptionDto>?",
                    BiaFieldAttributes = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ItemType", "PlaneType")
                    }
                }
            };
        }
    }
}
