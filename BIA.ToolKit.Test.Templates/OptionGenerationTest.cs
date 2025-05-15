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

    public class OptionGenerationTest : IClassFixture<FileGeneratorTestFixture>
    {
        private readonly FileGeneratorTestFixture fixture;

        public OptionGenerationTest(FileGeneratorTestFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Generates the dto and mapper for PlaneType.
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

            var dtoContext = new FileGeneratorOptionContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                GenerateBack = true
            };

            await fixture.FileGeneratorService.GenerateOptionAsync(dtoContext);

            fixture.AssertFilesEquals(dtoContext, fixture.FileGeneratorService.CurrentFeature);
        }
    }
}
