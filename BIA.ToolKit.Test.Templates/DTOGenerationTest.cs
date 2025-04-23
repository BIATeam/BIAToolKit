namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
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
                    EntityCompositeName = "Family",
                    MappingType = "DateTime?",
                    MappingName = "Family",
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

            foreach(var dotNetTemplate in fixture.FileGeneratorService.CurrentFeature.DotNetTemplates)
            {
                var (referencePath, generatedPath) = fixture.GetDotNetFilesPath(dotNetTemplate.OutputPath, dtoContext);
                Assert.True(File.Exists(generatedPath));
                CustomAssert.FilesEquals(referencePath, generatedPath);
            }

            foreach (var angularTemplate in fixture.FileGeneratorService.CurrentFeature.AngularTemplates)
            {
                var (referencePath, generatedPath) = fixture.GetAngularFilesPath(angularTemplate.OutputPath, dtoContext);
                Assert.True(File.Exists(generatedPath));
                CustomAssert.FilesEquals(referencePath, generatedPath);
            }
        }
    }
}
