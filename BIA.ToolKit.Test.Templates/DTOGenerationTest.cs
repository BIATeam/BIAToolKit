namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;

    public class DTOGenerationTest : IClassFixture<FileGeneratorTestFixture>
    {
        private readonly FileGeneratorTestFixture fixture;

        public DTOGenerationTest(FileGeneratorTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task GenerateDTO_Part()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Plane.Entities",
                name: "Part",
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
                    EntityCompositeName = "SN",
                    MappingType = "string",
                    MappingName = "SN",
                },
                new()
                {
                    EntityCompositeName = "Family",
                    MappingType = "string",
                    MappingName = "Family",
                },
                new()
                {
                    EntityCompositeName = "Price",
                    MappingType = "decimal",
                    MappingName = "Price",
                },
            };

            await fixture.FileGeneratorService.GenerateDto(entityInfo, domainName, mappingProperties);

            foreach(var dotNetTemplate in fixture.FileGeneratorService.CurrentFeature.DotNetTemplates)
            {
                var (referencePath, generatedPath) = fixture.GetDotNetFilesPath(dotNetTemplate.OutputPath, domainName, entityInfo.Name);
                Assert.True(File.Exists(generatedPath));
                Assert.Equal(File.ReadAllText(referencePath), File.ReadAllText(generatedPath));
            }

            foreach (var angularTemplate in fixture.FileGeneratorService.CurrentFeature.AngularTemplates)
            {
                var (referencePath, generatedPath) = fixture.GetAngularFilesPath(angularTemplate.OutputPath, entityInfo.Name);
                Assert.True(File.Exists(generatedPath));
                Assert.Equal(File.ReadAllText(referencePath), File.ReadAllText(generatedPath));
            }
        }
    }
}
