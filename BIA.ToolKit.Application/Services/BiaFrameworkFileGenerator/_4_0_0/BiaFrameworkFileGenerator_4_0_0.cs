namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator._4_0_0
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator._4_0_0.Models;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator._4_0_0.Templates;
    using BIA.ToolKit.Application.TemplateGenerator._4_0_0.Models.DotNet.DomainDto;
    using BIA.ToolKit.Application.TemplateGenerator._4_0_0.Templates.DotNet.DomainDto;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    public class BiaFrameworkFileGenerator_4_0_0 : IBiaFrameworkFileGenerator
    {
        private readonly BiaFrameworkFileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;

        public List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions => new()
        {
            new("4.*"),
        };

        public BiaFrameworkFileGenerator_4_0_0(BiaFrameworkFileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var dtoModel = new EntityDtoModel
            {
                CompanyName = project.CompanyName,
                ProjectName = project.Name,
                NameArticle = Common.ComputeNameArticle(entityInfo.Name),
                DomainName = domainName,
                DtoName = entityInfo.Name + "Dto",
                EntityName = entityInfo.Name,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = mappingEntityProperties.Select(x => new TemplateGenerator._4_0_0.Models.DotNet.DomainDto.PropertyModel()
                {
                    MappingName = x.MappingName,
                    EntityCompositeName = x.EntityCompositeName,
                    MappingType = x.MappingType,
                    MappingDateType = x.MappingDateType,
                    IsOption = x.IsOption,
                    IsOptionCollection = x.IsOptionCollection,
                    OptionType = x.OptionType,
                    IsRequired = x.IsRequired,
                    OptionDisplayProperty = x.OptionDisplayProperty,
                    OptionIdProperty = x.OptionIdProperty,
                    OptionEntityIdPropertyComposite = x.OptionEntityIdPropertyComposite,
                    OptionRelationType = x.OptionRelationType,
                    OptionRelationPropertyComposite = x.OptionRelationPropertyComposite,
                    OptionRelationFirstIdProperty = x.OptionRelationFirstIdProperty,
                    OptionRelationSecondIdProperty = x.OptionRelationSecondIdProperty,
                }).ToList(),
                EntityNamespace = entityInfo.Namespace,
                MapperName = entityInfo.Name + "Mapper",
                IsTeamType = entityInfo.BaseType?.Contains("Team") ?? false
            };

            var mapperModel = new DtoModel
            {
                CompanyName = project.CompanyName,
                ProjectName = project.Name,
                NameArticle = Common.ComputeNameArticle(entityInfo.Name),
                DomainName = domainName,
                DtoName = entityInfo.Name + "Dto",
                EntityName = entityInfo.Name,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = mappingEntityProperties.Select(x => new Models.PropertyModel()
                {
                    MappingName = x.MappingName,
                    EntityCompositeName = x.EntityCompositeName,
                    MappingType = x.MappingType,
                    MappingDateType = x.MappingDateType,
                    IsOption = x.IsOption,
                    IsOptionCollection = x.IsOptionCollection,
                    OptionType = x.OptionType,
                    IsRequired = x.IsRequired,
                    OptionDisplayProperty = x.OptionDisplayProperty,
                    OptionIdProperty = x.OptionIdProperty,
                    OptionEntityIdPropertyComposite = x.OptionEntityIdPropertyComposite,
                    OptionRelationType = x.OptionRelationType,
                    OptionRelationPropertyComposite = x.OptionRelationPropertyComposite,
                    OptionRelationFirstIdProperty = x.OptionRelationFirstIdProperty,
                    OptionRelationSecondIdProperty = x.OptionRelationSecondIdProperty,
                }).ToList(),
                EntityNamespace = entityInfo.Namespace,
                MapperName = entityInfo.Name + "Mapper",
                IsTeamType = entityInfo.BaseType?.Contains("Team") ?? false
            };

            if (string.IsNullOrWhiteSpace(dtoModel.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                dtoModel.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            consoleWriter.AddMessageLine($"Generating DTO...");
            var dtoContent = new EntityDtoTemplate(dtoModel).TransformText();
            var dtoDestPath = Path.Combine(
                project.Folder,
                Constants.FolderDotNet,
                string.Join(".", project.CompanyName, project.Name, "Domain", "Dto"),
                dtoModel.DomainName,
                $"{dtoModel.DtoName}.cs");
            await fileGeneratorService.GenerateFile(dtoContent, dtoDestPath);
            consoleWriter.AddMessageLine($"DTO successfully generated !", "green");

            consoleWriter.AddMessageLine($"Generating mapper...");
            var mapperContent = await fileGeneratorService.GenerateFromTemplateWithRazor(typeof(MapperTemplate), mapperModel);
            var mapperDestPath = Path.Combine(
                Path.GetDirectoryName(entityInfo.Path),
                "..",
                "Mappers",
                $"{dtoModel.MapperName}.cs");
            await fileGeneratorService.GenerateFile(mapperContent, mapperDestPath);
            consoleWriter.AddMessageLine($"Mapper successfully generated !", "green");
        }
    }
}
