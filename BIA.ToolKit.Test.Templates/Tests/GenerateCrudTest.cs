namespace BIA.ToolKit.Test.Templates.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Domain.DtoGenerator;

    [Collection("FileGeneratorTestCollection")]
    public class GenerateCrudTest(FileGeneratorTestFixture fixture)
    {
        /// <summary>
        /// Generates the CRUD's files for Engine.
        /// </summary>
        [Fact]
        public async Task GenerateEngine_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(5))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Engine",
                baseType: null,
                baseKeyType: "int",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDtoVersionedFixable<int>"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("string", "Reference", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("string?", "Manufacturer", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("DateTime", "NextMaintenanceDate", [RoslynHelper.CreateAttributeArgument("Type", "datetime"), RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("DateTime?", "LastMaintenanceDate", [RoslynHelper.CreateAttributeArgument("Type", "datetime"), RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("DateTime", "DeliveryDate", [RoslynHelper.CreateAttributeArgument("Type", "date"), RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("DateTime?", "ExchangeDate", [RoslynHelper.CreateAttributeArgument("Type", "date"), RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("string", "SyncTime", [RoslynHelper.CreateAttributeArgument("Type", "time"), RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("string?", "IgnitionTime", [RoslynHelper.CreateAttributeArgument("Type", "time"), RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("int?", "Power", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("int", "NoiseLevel", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("double", "FlightHours", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("double?", "AverageFlightHours", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("float", "FuelConsumption", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("float?", "AverageFuelConsumption", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("decimal", "OriginalPrice", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("decimal?", "EstimatedPrice", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("bool", "IsToBeMaintained", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("bool?", "IsHybrid", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("int", "PlaneId", [RoslynHelper.CreateAttributeArgument("IsParent", true), RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("OptionDto", "PrincipalPart", [RoslynHelper.CreateAttributeArgument("ItemType", "Part")]),
                new("ICollection<OptionDto>", "InstalledParts", [RoslynHelper.CreateAttributeArgument("ItemType", "Part"), RoslynHelper.CreateAttributeArgument("Required", true)])
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                HasParent = true,
                Properties = properties,
                IsVersioned = true,
                IsFixable = true,
                ParentName = "Plane",
                ParentNamePlural = "Planes",
                DisplayItemName = "Reference",
                OptionItems = ["Part"],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = true,
                HasCustomRepository = true,
                HasFixableParent = true,
                CanImport = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }

        /// <summary>
        /// Generates the CRUD's files for Plane.
        /// </summary>
        [Fact]
        public async Task GeneratePlane_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(5))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Plane",
                baseType: null,
                baseKeyType: "int",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDtoVersionedFixableArchivable<int>"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("int", "SiteId", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("IsParent", true)]),
                new("string", "Msn", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("string?", "Manufacturer", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("bool", "IsActive", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("bool?", "IsMaintenance", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("DateTime", "FirstFlightDate", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "datetime")]),
                new("DateTime?", "LastFlightDate", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "datetime")]),
                new("DateTime?", "DeliveryDate", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "date")]),
                new("DateTime", "NextMaintenanceDate", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "date")]),
                new("string?", "SyncTime", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "time")]),
                new("string", "SyncFlightDataTime", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "time")]),
                new("int", "Capacity", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("int?", "MotorsCount", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("double", "TotalFlightHours", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("double?", "Probability", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("float", "FuelCapacity", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("float?", "FuelLevel", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("decimal", "OriginalPrice", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("decimal?", "EstimatedPrice", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("OptionDto", "PlaneType", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("ItemType", "PlaneType")]),
                new("ICollection<OptionDto>", "SimilarTypes", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("ItemType", "PlaneType")]),
                new("OptionDto", "CurrentAirport", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")]),
                new("ICollection<OptionDto>", "ConnectingAirports", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")])
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = properties,
                IsVersioned = true,
                DisplayItemName = "Msn",
                OptionItems = ["PlaneType", "Airport"],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = true,
                HasReadOnlyMode = true,
                IsFixable = true,
                FormReadOnlyMode = Enum.GetName(typeof(FormReadOnlyMode), FormReadOnlyMode.Off),
                CanImport = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }

        /// <summary>
        /// Generates the CRUD's files for Plane.
        /// </summary>
        [Fact]
        public async Task GeneratePilot_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(6))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Pilot",
                baseType: null,
                baseKeyType: "Guid",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDtoVersionedFixableArchivable<Guid>"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("int", "SiteId", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("IsParent", true)]),
                new("string", "IdentificationNumber", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("int?", "FlightHours", [RoslynHelper.CreateAttributeArgument("Required", false)]),
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = properties,
                IsVersioned = true,
                DisplayItemName = "IdentificationNumber",
                OptionItems = [],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = false,
                HasReadOnlyMode = true,
                IsFixable = true,
                FormReadOnlyMode = Enum.GetName(typeof(FormReadOnlyMode), FormReadOnlyMode.Off),
                CanImport = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }

        /// <summary>
        /// Generates the CRUD's files for Plane.
        /// </summary>
        [Fact]
        public async Task GenerateFlight_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(6))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Flight",
                baseType: null,
                baseKeyType: "string",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDtoVersionedFixableArchivable<string>"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("int", "SiteId", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("IsParent", true)]),
                new("OptionDto", "DepartureAirport", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")]),
                new("OptionDto", "ArrivalAirport", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")]),
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = properties,
                IsVersioned = true,
                DisplayItemName = "Id",
                OptionItems = ["Airport"],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = false,
                HasReadOnlyMode = true,
                IsFixable = true,
                FormReadOnlyMode = Enum.GetName(typeof(FormReadOnlyMode), FormReadOnlyMode.Off),
                CanImport = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }

        /// <summary>
        /// Generates the CRUD's files for Aircraft Maintenance Company.
        /// </summary>
        [Fact]
        public async Task GenerateAircraftMaintenanceCompany_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(5))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Maintenance",
                name: "AircraftMaintenanceCompany",
                baseType: "BaseDtoVersionedTeam",
                baseKeyType: null,
                arguments: [],
                baseList: ["BaseDtoVersionedTeam"]);

            var domainName = "Maintenance";

            var properties = new List<PropertyInfo>
            {
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = properties,
                IsVersioned = true,
                IsTeam = true,
                DisplayItemName = "Title",
                HasAdvancedFilter = true,
                TeamTypeId = 3,
                TeamRoleId = 3,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext, ["MaintenanceTeam"]);
        }

        /// <summary>
        /// Generates the CRUD's files for Maintenance Team.
        /// </summary>
        [Fact]
        public async Task GenerateMaintenanceTeam_BIADemoConfiguration_AllFilesEquals()
        {
            if (!fixture.IsTestProjectMinimalVersion(5))
                return;

            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Maintenance",
                name: "MaintenanceTeam",
                baseType: "BaseDtoVersionedTeamFixableArchivable",
                baseKeyType: null,
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "AircraftMaintenanceCompany")],
                baseList: ["BaseDtoVersionedTeamFixableArchivable"]);

            var domainName = "Maintenance";

            var properties = new List<PropertyInfo>
            {
                new("string", "Code", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("bool", "IsActive", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("bool?", "IsApproved", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("DateTime", "FirstOperation", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "datetime")]),
                new("DateTime?", "LastOperation", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "datetime")]),
                new("DateTime?", "ApprovedDate", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "date")]),
                new("DateTime", "NextOperation", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "date")]),
                new("string", "MaxTravelDuration", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "time")]),
                new("string", "MaxOperationDuration", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "time")]),
                new("int", "OperationCount", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("int?", "IncidentCount", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("double", "TotalOperationDuration", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("double?", "AverageOperationDuration", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("float", "TotalTravelDuration", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("float?", "AverageTravelDuration", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("decimal", "TotalOperationCost", [RoslynHelper.CreateAttributeArgument("Required", true)]),
                new("decimal?", "AverageOperationCost", [RoslynHelper.CreateAttributeArgument("Required", false)]),
                new("int", "AircraftMaintenanceCompanyId", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("IsParent", true)]),
                new("OptionDto", "CurrentAirport", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")]),
                new("ICollection<OptionDto>", "OperationAirports", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("ItemType", "Airport")]),
                new("OptionDto", "CurrentCountry", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("ItemType", "Country")]),
                new("ICollection<OptionDto>", "OperationCountries", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("ItemType", "Country")])
            };

            var crudContext = new FileGeneratorCrudContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = properties,
                IsVersioned = true,
                IsTeam = true,
                OptionItems = ["Airport", "Country"],
                HasParent = true,
                ParentName = "AircraftMaintenanceCompany",
                ParentNamePlural = "AircraftMaintenanceCompanies",
                HasAncestorTeam = true,
                AncestorTeamName = "AircraftMaintenanceCompany",
                IsFixable = true,
                DisplayItemName = "Title",
                HasAdvancedFilter = true,
                TeamTypeId = 4,
                TeamRoleId = 4,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }
    }
}
