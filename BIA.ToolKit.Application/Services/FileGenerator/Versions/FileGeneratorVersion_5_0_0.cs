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

        protected virtual EntityOptionModel CreateOptionModel()
        {
            return new EntityOptionModel();
        }

        protected virtual EntityCrudModel CreateCrudModel()
        {
            return new EntityCrudModel();
        }

        public new object GetDtoTemplateModel(Project project, EntityInfo entityInfo, string domainName, IEnumerable<MappingEntityProperty> mappingEntityProperties)
        {
            var model = base.GetDtoTemplateModel(project, entityInfo, domainName, mappingEntityProperties) as EntityDtoModel;
            // Map additionnal properties of your model 
            return model;
        }

        public new object GetOptionTemplateModel(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayName)
        {
            var model = CreateOptionModel();

            model.CompanyName = entityInfo.CompanyName;
            model.ProjectName = entityInfo.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(entityInfo.Name);
            model.EntityName = entityInfo.Name;
            model.EntityNamePlural = entityNamePlural;
            model.BaseKeyType = entityInfo.BaseKeyType;
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = domainName;
            model.OptionDisplayName = displayName;

            return model;
        }

        public new object GetCrudTemplateModel(EntityInfo entityInfo, string entityNamePlural, string domainName, string displayItemName, bool isTeam = false, List<string> optionItems = null, bool hasParent = false, string parentName = null, string parentNamePlural = null)
        {
            var model = CreateCrudModel();

            model.CompanyName = entityInfo.CompanyName;
            model.ProjectName = entityInfo.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(entityInfo.Name);
            model.EntityName = entityInfo.Name.Replace("dto", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            model.EntityNamePlural = entityNamePlural;
            model.BaseKeyType = entityInfo.BaseKeyType ?? entityInfo.PrimaryKey ?? (entityInfo.BaseType == "TeamDto" ? "int" : null);
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = domainName;
            model.DisplayItemName = displayItemName;
            model.HasAncestorTeam = entityInfo.HasAncestorTeam;
            model.AncestorTeamName = entityInfo.AncestorTeamName;
            model.IsTeam = isTeam;
            model.HasParent = hasParent;
            model.ParentName = parentName;
            model.ParentNamePlural = parentNamePlural;
            model.OptionItems = optionItems ?? [];

            return model;
        }
    }
}
