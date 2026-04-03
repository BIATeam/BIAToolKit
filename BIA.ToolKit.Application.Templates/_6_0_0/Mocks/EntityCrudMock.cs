namespace BIA.ToolKit.Application.Templates._6_0_0.Mocks
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates._6_0_0.Models;

    public class EntityCrudMock : EntityCrudModel<PropertyCrudModel>
    {
        public EntityCrudMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "Fleet";
            EntityName = "Plane";
            BaseKeyType = "int";
            EntityNamePlural = "Planes";
            IsTeam = false;
            HasAncestorTeam = true;
            AncestorTeamName = "Site";
            DisplayItemName = "Msn";
            OptionItems = ["Engine", "PlaneType"];
            HasParent = false;
            DisplayHistorical = true;
            UseDomainUrl = true;
            //ParentName = "AircraftMaintenanceCompany";
            //ParentNamePlural = "AircraftMaintenanceCompanies";
            //AngularParentRelativePath = "aircraft-maintenance-companies";
            //AngularDeepLevel = 2;
            Properties =
            [
                 new PropertyCrudModel
                {
                    Name = "SiteId",
                    Type = "int",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "true")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "Msn",
                    Type = "string",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "true")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "IsActive",
                    Type = "bool",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "true")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "FirstFlightDate",
                    Type = "DateTime",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "true"),
                        new KeyValuePair<string, string>("Type", "date")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "MotorsCount",
                    Type = "int?",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "false")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "SomeDecimal",
                    Type = "decimal",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("IsRequired", "false")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "Engines",
                    Type = "ICollection<OptionDto>",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("ItemType", "Engine")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "PlaneType",
                    Type = "OptionDto",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("ItemType", "PlaneType")
                    ]
                },
                new PropertyCrudModel
                {
                    Name = "SimilarTypes",
                    Type = "ICollection<OptionDto>?",
                    BiaFieldAttributes =
                    [
                        new KeyValuePair<string, string>("ItemType", "PlaneType")
                    ]
                }
            ];
        }
    }
}
