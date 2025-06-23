﻿namespace BIA.ToolKit.Test.Templates.Tests
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
        public async Task GeneratePlaneType_BIADemoConfiguration_AllFilesEquals()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Fleet.Entities",
                name: "PlaneType",
                baseType: null,
                baseKeyType: "int",
                arguments: null,
                baseList: ["BaseEntityVersioned<int>"]);

            var domainName = "Fleet";

            var optionContext = new FileGeneratorOptionContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
                DisplayName = "Title"
            };

            await fixture.RunTestGenerateOptionAllFilesEqualsAsync(optionContext);
        }

        /// <summary>
        /// Generates the option's files for Country.
        /// It is a sample for an Option used for DTO and for CRUD
        /// </summary>
        [Fact]
        public async Task GenerateCountry_BIADemoConfiguration_AllFilesEquals()
        {
            var entityInfo = new EntityInfo(
                path: string.Empty,
                @namespace: "TheBIADevCompany.BIADemo.Domain.Fleet.Entities",
                name: "Country",
                baseType: null,
                baseKeyType: "int",
                arguments: null,
                baseList: ["BaseEntityVersioned<int>"]);

            var domainName = "Maintenance";

            var optionContext = new FileGeneratorOptionContext
            {
                CompanyName = fixture.TestProject.CompanyName,
                ProjectName = fixture.TestProject.Name,
                DomainName = domainName,
                EntityName = entityInfo.Name,
                EntityNamePlural = entityInfo.NamePluralized,
                BaseKeyType = entityInfo.BaseKeyType,
                GenerateBack = true,
                GenerateFront = true,
                AngularFront = "Angular",
                DisplayName = "Name"
            };

            await fixture.RunTestGenerateOptionAllFilesEqualsAsync(optionContext);
        }
    }
}
