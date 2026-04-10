namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;
    using BIA.ToolKit.Common;

    internal abstract class FileGeneratorModelProviderBase<TEntityDtoModel, TEntityCrudModel, TEntityOptionModel, TEntityTeamModel, TPropertyDtoModel, TPropertyCrudModel>(IConsoleWriter consoleWriter) : IFileGeneratorModelProvider
        where TPropertyDtoModel : class, IPropertyDtoModel, new()
        where TEntityDtoModel : class, IEntityDtoModel<TPropertyDtoModel>, new()
        where TPropertyCrudModel : class, IPropertyCrudModel, new()
        where TEntityCrudModel : class, IEntityCrudModel<TPropertyCrudModel>, new()
        where TEntityOptionModel : class, IEntityOptionModel, new()
        where TEntityTeamModel : class, IEntityTeamModel, new()
    {
        protected readonly IConsoleWriter consoleWriter = consoleWriter;

        public abstract List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions { get; }

        protected static TEntityCrudModel CreateCrudTemplateModel() => new();

        protected static TEntityDtoModel CreateDtoTemplateModel() => new();

        protected static TEntityOptionModel CreateOptionTemplateModel() => new();

        protected static TEntityTeamModel CreateTeamTemplateModel() => new();

        public virtual object GetCrudTemplateModel(FileGeneratorCrudContext crudContext)
        {
            TEntityCrudModel model = CreateCrudTemplateModel();

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
            model.IsVersioned = crudContext.IsVersioned;
            model.IsTeam = crudContext.IsTeam;
            model.HasParent = crudContext.HasParent;
            model.ParentName = crudContext.ParentName;
            model.ParentNamePlural = crudContext.ParentNamePlural;
            model.AngularParentRelativePath = crudContext.AngularParentFolderRelativePath;
            model.AngularDeepLevel = crudContext.AngularDeepLevel;
            model.OptionItems = crudContext.OptionItems;
            model.UseHubForClient = crudContext.UseHubForClient;
            model.HasCustomRepository = crudContext.HasCustomRepository;
            model.HasReadOnlyMode = crudContext.HasReadOnlyMode;
            model.HasFixableParent = crudContext.HasFixableParent;
            model.IsFixable = crudContext.IsFixable;
            model.IsArchivable = crudContext.IsArchivable;
            model.HasAdvancedFilter = crudContext.HasAdvancedFilter;
            model.CanImport = crudContext.CanImport;
            model.TeamRoleId = crudContext.TeamRoleId;
            model.TeamTypeId = crudContext.TeamTypeId;
            model.FormReadOnlyMode = crudContext.FormReadOnlyMode;
            model.DisplayHistorical = crudContext.DisplayHistorical;
            model.UseDomainUrl = crudContext.UseDomainUrl;
            model.HasListAndItemModels = crudContext.HasListAndItemModels;

            model.Properties = [.. crudContext.Properties.Select(x => new TPropertyCrudModel
            {
                Name = x.Name,
                Type = x.Type,
                BiaFieldAttributes = x.Annotations
            })];

            model.ListProperties = [.. crudContext.ListProperties.Select(x => new TPropertyCrudModel
            {
                Name = x.Name,
                Type = x.Type,
                BiaFieldAttributes = x.Annotations
            })];

            return model;
        }

        public virtual object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext)
        {
            TEntityDtoModel model = CreateDtoTemplateModel();

            model.CompanyName = dtoContext.CompanyName;
            model.ProjectName = dtoContext.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(dtoContext.EntityName);
            model.DomainName = dtoContext.DomainName;
            model.EntityName = dtoContext.EntityName;
            model.BaseKeyType = dtoContext.BaseKeyType;
            model.AncestorTeam = dtoContext.AncestorTeamName;
            model.Properties = [.. dtoContext.Properties.Select(x => new TPropertyDtoModel()
            {
                MappingName = x.MappingName,
                EntityCompositeName = x.EntityCompositeName,
                EntityType = x.EntityType,
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
                AsLocalDateTime = x.AsLocalDateTime
            })];
            model.IsTeamType = dtoContext.IsTeam;
            model.IsArchivable = dtoContext.IsArchivable;
            model.IsFixable = dtoContext.IsFixable;
            model.IsVersioned = dtoContext.IsVersioned;
            model.HasAudit = dtoContext.HasAudit;
            model.HasListAndItemModels = dtoContext.HasListAndItemModels;
            model.ListProperties = [.. dtoContext.ListProperties.Select(x => new TPropertyDtoModel()
            {
                MappingName = x.MappingName,
                EntityCompositeName = x.EntityCompositeName,
                EntityType = x.EntityType,
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
                AsLocalDateTime = x.AsLocalDateTime
            })];

            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            return model;
        }

        public virtual object GetOptionTemplateModel(FileGeneratorOptionContext optionContext)
        {
            TEntityOptionModel model = CreateOptionTemplateModel();

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
            model.UseHubForClient = optionContext.UseHubForClient;

            return model;
        }

        public virtual object GetTeamTemplateModel(FileGeneratorTeamContext teamContext)
        {
            TEntityTeamModel model = CreateTeamTemplateModel();
            model.CompanyName = teamContext.CompanyName;
            model.ProjectName = teamContext.ProjectName;
            model.EntityNameArticle = Common.ComputeNameArticle(teamContext.EntityName);
            model.EntityName = teamContext.EntityName;
            model.EntityNamePlural = teamContext.EntityNamePlural;
            model.BaseKeyType = teamContext.BaseKeyType;
            if (string.IsNullOrWhiteSpace(model.BaseKeyType))
            {
                consoleWriter.AddMessageLine($"WARNING: Unable to retrieve entity's base key type, you'll must replace the template '{Common.TemplateValue_BaseKeyType}' by corresponding type value after generation.", "orange");
                model.BaseKeyType = Common.TemplateValue_BaseKeyType;
            }

            model.DomainName = teamContext.DomainName;
            model.IsTeamType = true;

            return model;
        }
    }
}
