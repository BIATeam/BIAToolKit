namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Templates._4_0_0.Models;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorVersion_4_0_0(IConsoleWriter consoleWriter) : IFileGeneratorVersion
    {
        public List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("4.*"),
        ];

        public object GetDtoTemplateModel(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = new EntityModel
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

            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            return model;
        }
    }
}
