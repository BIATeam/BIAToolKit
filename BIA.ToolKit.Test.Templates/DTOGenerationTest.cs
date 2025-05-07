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

        /// <summary>
        /// Generates the type of the dto plane.
        /// It is a sample for a DTO used for Option and for CRUD
        /// </summary>
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
                    EntityType = "int",
                    MappingName = "Id",
                },
                new()
                {
                    EntityCompositeName = "Title",
                    EntityType = "string",
                    MappingName = "Title",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "CertificationDate",
                    EntityType = "DateTime?",
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

        /// <summary>
        /// Generates the dto plane.
        /// It is a sample for a DTO used for CRUD with all type of data
        /// </summary>
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
            var ancestorTeamName = "Site";

            var mappingProperties = new List<MappingEntityProperty>
            {
                new()
                {
                    EntityCompositeName = "SiteId",
                    EntityType = "int",
                    IsRequired = true,
                    IsParent = true,
                },
                new()
                {
                    EntityCompositeName = "Id",
                    EntityType = "int",
                },
                new()
                {
                    EntityCompositeName = "Msn",
                    EntityType = "string",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "Manufacturer",
                    EntityType = "string",
                },
                new()
                {
                    EntityCompositeName = "IsActive",
                    EntityType = "bool",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "IsMaintenance",
                    EntityType = "bool?",
                },
                new()
                {
                    EntityCompositeName = "FirstFlightDate",
                    EntityType = "DateTime",
                    IsRequired = true,
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "LastFlightDate",
                    EntityType = "DateTime?",
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "DeliveryDate",
                    EntityType = "DateTime?",
                    MappingDateType = "date",
                },
                new()
                {
                    EntityCompositeName = "NextMaintenanceDate",
                    EntityType = "DateTime",
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
                    EntityType = "int",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "MotorsCount",
                    EntityType = "int?",
                },
                new()
                {
                   EntityCompositeName = "TotalFlightHours",
                   EntityType = "double",
                   IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "Probability",
                    EntityType = "double?",
                },
                new()
                {
                    EntityCompositeName = "FuelCapacity",
                    EntityType = "float",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "FuelLevel",
                    EntityType = "float?",
                },
                new()
                {
                    EntityCompositeName = "OriginalPrice",
                    EntityType = "decimal",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "EstimatedPrice",
                    EntityType = "decimal?",
                },
                new()
                {
                    EntityCompositeName = "PlaneType",
                    EntityType = "OptionDto",
                    OptionType = "PlaneType",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Title",
                    OptionEntityIdProperty = "PlaneTypeId",
                },
                new()
                {
                    EntityCompositeName = "SimilarTypes",
                    EntityType = "ICollection<OptionDto>",
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
                    EntityType = "OptionDto",
                    IsRequired = true,
                    OptionType = "Airport",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionEntityIdProperty = "CurrentAirportId",
                },
                new()
                {
                    EntityCompositeName = "ConnectingAirports",
                    EntityType = "ICollection<OptionDto>",
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
                AncestorTeamName = ancestorTeamName,
            };

            await fixture.FileGeneratorService.GenerateDtoAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }

        /// <summary>
        /// Generates the dto plane.
        /// It is a sample for a DTO used for Team
        /// </summary>
        [Fact]
        public async Task GenerateDTO_AircraftMaintenanceCompany()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Entities",
                name: "AircraftMaintenanceCompany",
                baseType: "Team",
                primaryKey: null,
                arguments: null,
                baseList: []);

            var domainName = "AircraftMaintenanceCompany";

            var mappingProperties = new List<MappingEntityProperty>
            {
            };

            var dtoContext = new FileGeneratorDtoContext
            {
                IsTeam = true,
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = [.. mappingProperties],
                GenerateBack = true,
            };

            await fixture.FileGeneratorService.GenerateDtoAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }
    }
}
