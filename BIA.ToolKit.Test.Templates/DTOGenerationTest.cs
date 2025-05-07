namespace BIA.ToolKit.Test.Templates
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
    using Newtonsoft.Json;

    public class DTOGenerationTest : IClassFixture<FileGeneratorTestFixture>
    {
        private readonly FileGeneratorTestFixture fixture;

        public DTOGenerationTest(FileGeneratorTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task GenerateDTO_PlaneType()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Plane.Entities",
                name: "PlaneType",
                baseType: "VersionedTable",
                primaryKey: null,
                arguments: null,
                baseList: ["IEntity<int>"]);

            var domainName = "Plane";

            var mappingProperties = new List<MappingEntityProperty>
            {
                new()
                {
                    EntityCompositeName = "Id",
                    MappingType = "int",
                    MappingName = "Id",
                },
                new()
                {
                    EntityCompositeName = "Title",
                    MappingType = "string",
                    MappingName = "Title",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "CertificationDate",
                    MappingType = "DateTime?",
                    MappingName = "CertificationDate",
                    MappingDateType = "datetime"
                },
            };

            var dtoContext = new FileGeneratorDtoContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = [.. mappingProperties],
                GenerateBack = true
            };

            await fixture.FileGeneratorService.GenerateDtoAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }

        [Fact]
        public async Task GenerateDTO_Plane()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Plane.Entities",
                name: "Plane",
                baseType: "VersionedTable",
                primaryKey: null,
                arguments: null,
                baseList: ["IEntityArchivable<int>"]);

            var domainName = "Plane";

            var mappingProperties = new List<MappingEntityProperty>
            {
                new()
                {
                    EntityCompositeName = "SiteId",
                    MappingType = "int",
                    IsRequired = true,
                    IsParent = true,
                },
                new()
                {
                    EntityCompositeName = "Id",
                    MappingType = "int",
                },
                new()
                {
                    EntityCompositeName = "Msn",
                    MappingType = "string",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "Manufacturer",
                    MappingType = "string",
                },
                new()
                {
                    EntityCompositeName = "IsActive",
                    MappingType = "bool",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "IsMaintenance",
                    MappingType = "bool?",
                },
                new()
                {
                    EntityCompositeName = "FirstFlightDate",
                    MappingType = "DateTime",
                    IsRequired = true,
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "LastFlightDate",
                    MappingType = "DateTime?",
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "DeliveryDate",
                    MappingType = "DateTime?",
                    MappingDateType = "date",
                },
                new()
                {
                    EntityCompositeName = "NextMaintenanceDate",
                    MappingType = "DateTime",
                    IsRequired = true,
                    MappingDateType = "date",
                },
                new()
                {
                    EntityCompositeName = "SyncTime",
                    EntityType = "TimeSpan?",
                    MappingType = "string",
                    MappingDateType = "time",
                },
                new()
                {
                    EntityCompositeName = "SyncFlightDataTime",
                    EntityType = "TimeSpan",
                    MappingType = "string",
                    IsRequired = true,
                    MappingDateType = "time",
                },
                new()
                {
                    EntityCompositeName = "Capacity",
                    MappingType = "int",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "MotorsCount",
                    MappingType = "int?",
                },
                new()
                {
                   EntityCompositeName = "TotalFlightHours",
                   MappingType = "double",
                   IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "Probability",
                    MappingType = "double?",
                },
                new()
                {
                    EntityCompositeName = "FuelCapacity",
                    MappingType = "float",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "FuelLevel",
                    MappingType = "float?",
                },
                new()
                {
                    EntityCompositeName = "OriginalPrice",
                    MappingType = "decimal",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "EstimatedPrice",
                    MappingType = "decimal?",
                },
                new()
                {
                    EntityCompositeName = "PlaneType",
                    MappingType = "OptionDto",
                    OptionType = "PlaneType",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Title",
                    OptionEntityIdProperty = "PlaneTypeId",
                },
                new()
                {
                    EntityCompositeName = "SimilarTypes",
                    MappingType = "ICollection<OptionDto>",
                    OptionType = "PlaneType",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Title",
                    OptionRelationPropertyComposite = "SimilarPlaneType",
                    OptionRelationType = "PlanePlaneType",
                    OptionRelationFirstIdProperty = "PlaneId",
                    OptionRelationSecondIdProperty = "PlaneTypeId",
                },
                new()
                {
                    EntityCompositeName = "CurrentAirport",
                    MappingType = "OptionDto",
                    IsRequired = true,
                    OptionType = "Airport",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionEntityIdProperty = "CurrentAirportId",
                },
                new()
                {
                    EntityCompositeName = "ConnectingAirports",
                    MappingType = "ICollection<OptionDto>",
                    IsRequired = true,
                    OptionType = "Airport",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionRelationPropertyComposite = "ConnectingPlaneAirports",
                    OptionRelationType = "PlaneAirport",
                    OptionRelationFirstIdProperty = "PlaneId",
                    OptionRelationSecondIdProperty = "AirportId",
                },
            };

            var dtoContext = new FileGeneratorDtoContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = [.. mappingProperties],
                GenerateBack = true,
                AncestorTeamName = "Site",
            };

            await fixture.FileGeneratorService.GenerateDtoAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }

    }
}
