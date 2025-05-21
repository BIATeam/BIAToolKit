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
                arguments: [RoslynHelper.CreateAttributeArgument("BiaDtoClass", [("AncestorTeam", "Site")])],
                baseList: ["BaseDto<int>", "IFixableDto"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("string", "Reference", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("string?", "Manufacturer", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("DateTime", "NextMaintenanceDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "datetime"),("Required", true)])]),
                new("DateTime?", "LastMaintenanceDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "datetime"), ("Required", false)])]),
                new("DateTime", "DeliveryDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "date"),("Required", true)])]),
                new("DateTime?", "ExchangeDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "date"),("Required", false)])]),
                new("string", "SyncTime", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "time"),("Required", true)])]),
                new("string?", "IgnitionTime", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Type", "time"),("Required", false)])]),
                new("int?", "Power", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("int", "NoiseLevel", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("double", "FlightHours", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("double?", "AverageFlightHours", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("float", "FuelConsumption", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("float?", "AverageFuelConsumption", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("decimal", "OriginalPrice", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("decimal?", "EstimatedPrice", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("bool", "IsToBeMaintained", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("bool?", "IsHybrid", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("int", "PlaneId", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("IsParent", true),("Required", false)])]),
                new("OptionDto?", "PrincipalPart", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("ItemType", "Part")])]),
                new("ICollection<OptionDto>", "InstalledParts", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("ItemType", "Part")])])
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
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }

        /// <summary>
        /// Generates the CRUD's files for Engine.
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
                arguments: [RoslynHelper.CreateAttributeArgument("BiaDtoClass", [("AncestorTeam", "Site")])],
                baseList: ["BaseDto<int>", "IArchivableDto"]);

            var domainName = "Fleet";

            var properties = new List<PropertyInfo>
            {
                new("int", "SiteId", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true),("IsParent", true)])]),
                new("string", "Msn", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("string?", "Manufacturer", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("bool", "IsActive", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("bool?", "IsMaintenance", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("DateTime", "FirstFlightDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true),("Type", "datetime")])]),
                new("DateTime?", "LastFlightDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false),("Type", "datetime")])]),
                new("DateTime?", "DeliveryDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false),("Type", "date")])]),
                new("DateTime", "NextMaintenanceDate", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true),("Type", "date")])]),
                new("string", "SyncTime", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true),("Type", "time")])]),
                new("string?", "SyncFlightDataTime", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false),("Type", "time")])]),
                new("int", "Capacity", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("int?", "MotorsCount", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("double", "TotalFlightHours", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("double?", "Probability", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("float", "FuelCapacity", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("float?", "FuelLevel", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("decimal", "OriginalPrice", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true)])]),
                new("decimal?", "EstimatedPrice", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false)])]),
                new("OptionDto", "PlaneType", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false),("ItemType", "PlaneType")])]),
                new("ICollection<OptionDto>", "SimilarTypes", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", false), ("ItemType", "PlaneType")])]),
                new("OptionDto", "CurrentAirport", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true),("ItemType", "Airport")])]),
                new("ICollection<OptionDto>", "ConnectingAirports", [RoslynHelper.CreateAttributeArgument("BiaDtoField", [("Required", true), ("ItemType", "Airport")])])
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
                IsFixable = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }
    }
}
