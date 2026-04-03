namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Common;

    internal interface IFileGeneratorModelProvider
    {
        List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions { get; }
        object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext);
        object GetOptionTemplateModel(FileGeneratorOptionContext optionContext);
        object GetCrudTemplateModel(FileGeneratorCrudContext crudContext);
    }
}
