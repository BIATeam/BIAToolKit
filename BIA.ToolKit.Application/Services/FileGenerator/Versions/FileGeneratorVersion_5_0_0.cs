namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
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

        protected override Templates._4_0_0.Models.EntityDtoModel CreateDtoTemplateModel()
        {
            return new EntityDtoModel();
        }

        protected virtual EntityOptionModel CreateOptionTemplateModel()
        {
            return new EntityOptionModel();
        }

        protected virtual EntityCrudModel CreateCrudTemplateModel()
        {
            return new EntityCrudModel();
        }

        public new object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext)
        {
            var model = base.GetDtoTemplateModel(dtoContext) as EntityDtoModel;
            // Map additionnal properties of your model 
            return model;
        }

        public new object GetOptionTemplateModel(FileGeneratorOptionContext optionContext)
        {
            var model = CreateOptionTemplateModel();

            model.CompanyName = optionContext.CompanyName;
            model.ProjectName = optionContext.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(optionContext.EntityName);
            model.EntityName = optionContext.EntityName;
            model.EntityNamePlural = optionContext.EntityNamePlural;
            model.BaseKeyType = optionContext.BaseKeyType;
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = optionContext.DomainName;
            model.OptionDisplayName = optionContext.DisplayName;

            return model;
        }

        public new object GetCrudTemplateModel(FileGeneratorCrudContext crudContext)
        {
            var model = CreateCrudTemplateModel();

            model.CompanyName = crudContext.CompanyName;
            model.ProjectName = crudContext.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(crudContext.EntityName);
            model.EntityName = crudContext.EntityName;
            model.EntityNamePlural = crudContext.EntityNamePlural;
            model.BaseKeyType = crudContext.BaseKeyType;
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = crudContext.DomainName;
            model.DisplayItemName = crudContext.DisplayItemName;
            model.HasAncestorTeam = crudContext.HasAncestorTeam;
            model.AncestorTeamName = crudContext.AncestorTeamName;
            model.IsTeam = crudContext.IsTeam;
            model.HasParent = crudContext.HasParent;
            model.ParentName = crudContext.ParentName;
            model.ParentNamePlural = crudContext.ParentNamePlural;
            model.AngularParentRelativePath = crudContext.AngularParentFolderRelativePath;
            model.AngularDeepLevel = crudContext.AngularDeepLevel;
            model.OptionItems = crudContext.OptionItems;

            model.Properties = crudContext.Properties.Select(x => new PropertyCrudModel
            {
                Name = x.Name,
                Type = x.Type,
                BiaFieldAttributes = x.Annotations
            }).ToList();

            return model;
        }
    }
}
