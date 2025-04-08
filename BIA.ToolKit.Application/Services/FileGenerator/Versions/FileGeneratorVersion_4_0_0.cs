namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.TemplateGenerator._4_0_0.Models;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorVersion_4_0_0(
        FileGeneratorService fileGeneratorService, 
        IConsoleWriter consoleWriter) 
        : FileGeneratorVersionBase, IFileGeneratorVersion
    {
        public List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("4.*"),
        ];

        protected override string TemplateFolderPath => @"_4_0_0\Templates";

        public async Task GenerateDto(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var entityModel = new EntityModel
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
                IsTeamType = entityInfo.BaseType?.Contains("Team") ?? false
            };

            if (string.IsNullOrWhiteSpace(entityModel.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                entityModel.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            consoleWriter.AddMessageLine($"Generating DTO...");
            var dtoDestPath = Path.Combine(
                project.Folder,
                Constants.FolderDotNet,
                string.Join(".", project.CompanyName, project.Name, "Domain", "Dto"),
                entityModel.DomainName,
                $"{entityModel.DtoName}.cs");
            await fileGeneratorService.GenerateFromTemplateWithT4(@$"{TemplateFolderPath}\DotNet\DomainDto\EntityDtoTemplate.tt", entityModel, dtoDestPath);
            consoleWriter.AddMessageLine($"DTO successfully generated !", "green");

            consoleWriter.AddMessageLine($"Generating mapper...");
            var mapperDestPath = Path.Combine(
                Path.GetDirectoryName(entityInfo.Path),
                "..",
                "Mappers",
                $"{entityModel.MapperName}.cs");
            await fileGeneratorService.GenerateFromTemplateWithT4(@$"{TemplateFolderPath}\DotNet\Domain\Mappers\EntityMapper.tt", entityModel, mapperDestPath);
            consoleWriter.AddMessageLine($"Mapper successfully generated !", "green");
        }
    }
}
