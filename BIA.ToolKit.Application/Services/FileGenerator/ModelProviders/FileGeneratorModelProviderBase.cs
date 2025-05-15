namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;
    using BIA.ToolKit.Common;

    internal abstract class FileGeneratorModelProviderBase<TEntityDtoModel, TEntityCrudModel, TEntityOptionModel, TPropertyDtoModel, TPropertyCrudModel> : IFileGeneratorModelProvider
        where TPropertyDtoModel : class, IPropertyDtoModel, new()
        where TEntityDtoModel : class, IEntityDtoModel<TPropertyDtoModel>, new()
        where TPropertyCrudModel : class, IPropertyCrudModel, new()
        where TEntityCrudModel : class, IEntityCrudModel<TPropertyCrudModel>, new()
        where TEntityOptionModel : class, IEntityOptionModel, new()
    {
        public abstract List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions { get; }

        protected static TEntityCrudModel CreateCrudTemplateModel() => new();

        protected static TEntityDtoModel CreateDtoTemplateModel() => new();

        protected static TEntityOptionModel CreateOptionTemplateModel() => new();

        public abstract object GetCrudTemplateModel(FileGeneratorCrudContext crudContext);

        public abstract object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext);

        public abstract object GetOptionTemplateModel(FileGeneratorOptionContext optionContext);
    }
}
