namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates._4_0_0.Models;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorModelProvider_4_0_0(IConsoleWriter consoleWriter) : IFileGeneratorModelProvider
    {
        protected readonly IConsoleWriter consoleWriter = consoleWriter;

        public List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("4.*"),
        ];

        protected virtual EntityDtoModel CreateDtoTemplateModel()
        {
            return new EntityDtoModel();
        }

        public object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext)
        {
            var model = CreateDtoTemplateModel();

            model.CompanyName = dtoContext.CompanyName;
            model.ProjectName = dtoContext.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(dtoContext.EntityName);
            model.DomainName = dtoContext.DomainName;
            model.EntityName = dtoContext.EntityName;
            model.BaseKeyType = dtoContext.BaseKeyType;
            model.AncestorTeam = dtoContext.AncestorTeamName;
            model.Properties = dtoContext.Properties.Select(x => new PropertyDtoModel()
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
                IsParent = x.IsParent,
            }).ToList();
            model.IsTeamType = dtoContext.IsTeam;

            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            return model;
        }

        public object GetOptionTemplateModel(FileGeneratorOptionContext optionContext)
        {
            throw new NotImplementedException();
        }

        public object GetCrudTemplateModel(FileGeneratorCrudContext crudContext)
        {
            throw new NotImplementedException();
        }
    }
}
