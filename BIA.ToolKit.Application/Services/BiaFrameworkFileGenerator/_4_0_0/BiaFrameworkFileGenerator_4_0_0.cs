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
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    public class BiaFrameworkFileGenerator_4_0_0 : IBiaFrameworkFileGenerator
    {
        private readonly BiaFrameworkFileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;

        public Version BiaFrameworkVersion => new("4.0.0");
        public string TemplatesNamespace => GetType().Namespace + ".Templates";

        public BiaFrameworkFileGenerator_4_0_0(BiaFrameworkFileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
        }

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = new DtoModel
            {
                CompanyName = project.CompanyName,
                ProjectName = project.Name,
                NameArticle = Common.ComputeNameArticle(entityInfo.Name),
                DomainName = domainName,
                DtoName = entityInfo.Name + "Dto",
                EntityName = entityInfo.Name,
                BaseKeyType = entityInfo.BaseKeyType,
                Properties = mappingEntityProperties.Select(x => new PropertyModel()
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
                IsTeamType = entityInfo.BaseType.Contains("Team")
            };

            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            consoleWriter.AddMessageLine($"Generating DTO...");
            var dtoContent = await fileGeneratorService.GenerateFromTemplate(Common.TemplateKey_Dto, model);
            var dtoDestPath = Path.Combine(
                project.Folder,
                Constants.FolderDotNet,
                string.Join(".", project.CompanyName, project.Name, "Domain", "Dto"),
                model.DomainName,
                $"{model.DtoName}.cs");
            await fileGeneratorService.GenerateFile(dtoContent, dtoDestPath);
            consoleWriter.AddMessageLine($"DTO successfully generated !", "green");

            consoleWriter.AddMessageLine($"Generating mapper...");
            var mapperContent = await fileGeneratorService.GenerateFromTemplate(Common.TemplateKey_Mapper, model);
            var mapperDestPath = Path.Combine(
                Path.GetDirectoryName(entityInfo.Path),
                $"{model.MapperName}.cs");
            await fileGeneratorService.GenerateFile(mapperContent, mapperDestPath);
            consoleWriter.AddMessageLine($"Mapper successfully generated !", "green");
        }
    }
}
