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

    [Collection("FileGeneratorTestCollection")]
    public class GenerateOptionTest(FileGeneratorTestFixture fixture)
    {
        /// <summary>
        /// Generates the option's files for PlaneType.
        /// It is a sample for an Option used for DTO and for CRUD
        /// </summary>
        [Fact]
        public async Task GenerateOption_PlaneType()
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

            var optionContext = new FileGeneratorOptionContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                GenerateBack = true,
                DisplayName = "PlaneType"
            };

            await fixture.TestGenerateOptionAsync(optionContext);
        }
    }
}
