namespace BIA.ToolKit.Test.Templates.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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
                @namespace: "TheBIADevCompany.BIADemo.Domain.Plane.Entities",
                name: "Engine",
                baseType: "VersionedTable",
                primaryKey: null,
                arguments: null,
                baseList: ["IEntityFixable<int>"]);

            var domainName = "Plane";

            var properties = new List<PropertyInfo>
            {
                new("int", "Id", []),
                new("string", "Reference", []),
                new("string", "Manufacturer", []),
                new("DateTime", "NextMaintenanceDate", []),
                new("DateTime?", "LastMaintenanceDate", []),
                new("DateTime", "DeliveryDate", []),
                new("DateTime?", "ExchangeDate", []),
                new("TimeSpan", "SyncTime", []),
                new("TimeSpan?", "IgnitionTime", []),
                new("int?", "Power", []),
                new("int", "NoiseLevel", []),
                new("double", "FlightHours", []),
                new("double?", "AverageFlightHours", []),
                new("float", "FuelConsumption", []),
                new("float?", "AverageFuelConsumption", []),
                new("decimal", "OriginalPrice", []),
                new("decimal?", "EstimatedPrice", []),
                new("Plane", "Plane", []),
                new("int", "PlaneId", []),
                new("bool", "IsToBeMaintained", []),
                new("bool?", "IsHybrid", []),
                new("Part", "PrincipalPart", []),
                new("int?", "PrincipalPartId", []),
                new("ICollection<Part>", "InstalledParts", []),
                new("ICollection<EnginePart>", "InstalledEngineParts", []),
                new("bool", "IsFixed", []),
                new("DateTime?", "FixedDate", []),
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
                HasAncestorTeam = true,
                AncestorTeamName = "Site",
                UseHubForClient = true,
                HasCustomRepository = true,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
            };

            await fixture.RunTestGenerateCrudAllFilesEqualsAsync(crudContext);
        }
    }
}
