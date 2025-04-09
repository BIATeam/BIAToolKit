namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Templates._5_0_0.Models;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorVersion_5_0_0(IConsoleWriter consoleWriter) : FileGeneratorVersion_4_0_0(consoleWriter), IFileGeneratorVersion
    {
        public new List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("5.*"),
        ];

        protected override Templates._4_0_0.Models.EntityDtoModel CreateDtoEntityModel()
        {
            return new EntityDtoModel();
        }

        protected virtual EntityOptionModel CreateDtoOptionModel()
        {
            return new EntityOptionModel();
        }

        public new object GetDtoTemplateModel(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = base.GetDtoTemplateModel(project, entityInfo, domainName, mappingEntityProperties) as Templates._5_0_0.Models.EntityDtoModel;
            // Map additionnal properties of your model 
            return model;
        }

        public new object GetOptionTemplateModel(EntityInfo entityInfo, string domainName, string displayName)
        {
            var model = CreateDtoOptionModel();

            model.CompanyName = entityInfo.CompanyName;
            model.ProjectName = entityInfo.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(entityInfo.Name);
            model.EntityName = entityInfo.Name;
            model.EntityNamePlural = entityInfo.NamePluralized;
            model.BaseKeyType = entityInfo.BaseKeyType;
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value inside the DTO and mapper after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = domainName;
            model.OptionDisplayName = displayName;

            return model;
        }
    }
}
