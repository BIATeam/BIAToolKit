namespace BIA.ToolKit.Test.Templates.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Newtonsoft.Json;

    [Collection("FileGeneratorTestCollection")]
    public class GenerateCrudTest(FileGeneratorTestFixture fixture)
    {
        /// <summary>
        /// Generates the CRUD's files for Engine.
        /// </summary>
        [Fact]
        public async Task GenerateEngine_BIADemoConfiguration_AllFilesEquals()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Engine",
                baseType: "BaseDto",
                baseKeyType: "int",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDto<int>", "IFixableDto"]);

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
                ParentName = "Plane",
                ParentNamePlural = "Planes",
                DisplayItemName = "Reference",
                OptionItems = ["Part"],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = true,
                HasCustomRepository = true,
                HasFixableParent = true,
                GenerateBack = false,
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
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Fleet",
                name: "Plane",
                baseType: "BaseDto",
                baseKeyType: "int",
                arguments: [RoslynHelper.CreateAttributeArgument("AncestorTeam", "Site")],
                baseList: ["BaseDto<int>", "IArchivableDto"]);

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
                new("string", "SyncTime", [RoslynHelper.CreateAttributeArgument("Required", true), RoslynHelper.CreateAttributeArgument("Type", "time")]),
                new("string?", "SyncFlightDataTime", [RoslynHelper.CreateAttributeArgument("Required", false), RoslynHelper.CreateAttributeArgument("Type", "time")]),
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
                DisplayItemName = "Msn",
                OptionItems = ["PlaneType", "Airport"],
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = true,
                HasReadOnlyMode = true,
                IsFixable = true,
                GenerateBack = false,
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
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Dto.Maintenance",
                name: "AircraftMaintenanceCompany",
                baseType: "TeamDto",
                baseKeyType: null,
                arguments: [],
                baseList: ["TeamDto"]);

            var domainName = "Maintenance";

            var properties = new List<PropertyInfo>
            {
                new("ICollection<OptionDto>", "Admins", [RoslynHelper.CreateAttributeArgument("Required", true)])
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
                IsTeam = true,
                DisplayItemName = "Title",
                HasAdvancedFilter = true,
                GenerateBack = false,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext, ["MaintenanceTeam"]);
        }
    }
}
