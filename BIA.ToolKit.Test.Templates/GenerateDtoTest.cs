namespace BIA.ToolKit.Test.Templates
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;

    public class GenerateDtoTest(FileGeneratorTestFixture fixture) : IClassFixture<FileGeneratorTestFixture>
    {
        /// <summary>
        /// Generates the dto and mapper for PlaneType.
        /// It is a sample for a DTO used for Option and for CRUD
        /// </summary>
        [Fact]
        public async Task GenerateDto_PlaneType()
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
        /// Generates the dto and mapper for Plane.
        /// It is a sample for a DTO used for CRUD with all type of data
        /// </summary>
        [Fact]
        public async Task GenerateDto_Plane()
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
        /// Generates the dto and mapper AircraftMaintenanceCompany.
        /// It is a sample for a DTO used for Team
        /// </summary>
        [Fact]
        public async Task GenerateDto_AircraftMaintenanceCompany()
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


        /// <summary>
        /// Generates the dto and mapper for MaintenanceTeam.
        /// It is a sample for a DTO used for Child Team
        /// </summary>
        [Fact]
        public async Task GenerateDto_MaintenanceTeam()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Entities",
                name: "MaintenanceTeam",
                baseType: "Team",
                primaryKey: null,
                arguments: null,
                baseList: []);

            var domainName = "AircraftMaintenanceCompany";
            var ancestorTeamName = "AircraftMaintenanceCompany";

            var mappingProperties = new List<MappingEntityProperty>
            {
                new()
                {
                    EntityCompositeName = "AircraftMaintenanceCompanyId",
                    EntityType = "int",
                    IsRequired = true,
                    IsParent = true,
                },
                new()
                {
                    EntityCompositeName = "Code",
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
                    EntityCompositeName = "IsApproved",
                    EntityType = "bool?",
                },
                new()
                {
                    EntityCompositeName = "FirstOperation",
                    EntityType = "DateTime",
                    IsRequired = true,
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "LastOperation",
                    EntityType = "DateTime?",
                    MappingDateType = "datetime",
                },
                new()
                {
                    EntityCompositeName = "ApprovedDate",
                    EntityType = "DateTime?",
                    MappingDateType = "date",
                },
                new()
                {
                    EntityCompositeName = "NextOperation",
                    EntityType = "DateTime",
                    IsRequired = true,
                    MappingDateType = "date",
                },
                new()
                {
                    EntityCompositeName = "MaxTravelDuration",
                    EntityType = "TimeSpan?",
                    MappingType = "string",
                    MappingDateType = "time",
                },
                new()
                {
                    EntityCompositeName = "MaxOperationDuration",
                    EntityType = "TimeSpan",
                    MappingType = "string",
                    IsRequired = true,
                    MappingDateType = "time",
                },
                new()
                {
                    EntityCompositeName = "OperationCount",
                    EntityType = "int",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "IncidentCount",
                    EntityType = "int?",
                },
                new()
                {
                   EntityCompositeName = "TotalOperationDuration",
                   EntityType = "double",
                   IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "AverageOperationDuration",
                    EntityType = "double?",
                },
                new()
                {
                    EntityCompositeName = "TotalTravelDuration",
                    EntityType = "float",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "AverageTravelDuration",
                    EntityType = "float?",
                },
                new()
                {
                    EntityCompositeName = "TotalOperationCost",
                    EntityType = "decimal",
                    IsRequired = true,
                },
                new()
                {
                    EntityCompositeName = "AverageOperationCost",
                    EntityType = "decimal?",
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
                    EntityCompositeName = "OperationAirports",
                    EntityType = "ICollection<OptionDto>",
                    IsRequired = true,
                    OptionType = "Airport",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionRelationPropertyComposite = "OperationMaintenanceTeamAirports",
                    OptionRelationType = "MaintenanceTeamAirport",
                    OptionRelationFirstIdProperty = "MaintenanceTeamId",
                    OptionRelationSecondIdProperty = "AirportId",
                },
                new()
                {
                    EntityCompositeName = "CurrentCountry",
                    EntityType = "OptionDto",
                    OptionType = "Country",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionEntityIdProperty = "CurrentCountryId",
                },
                new()
                {
                    EntityCompositeName = "OperationCountries",
                    EntityType = "ICollection<OptionDto>",
                    OptionType = "Country",
                    OptionIdProperty = "Id",
                    OptionDisplayProperty = "Name",
                    OptionRelationPropertyComposite = "OperationMaintenanceTeamCountries",
                    OptionRelationType = "MaintenanceTeamCountry",
                    OptionRelationFirstIdProperty = "MaintenanceTeamId",
                    OptionRelationSecondIdProperty = "CountryId",
                },
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
                AncestorTeamName = ancestorTeamName,
            };

            await fixture.FileGeneratorService.GenerateDtoAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }

    }
}
