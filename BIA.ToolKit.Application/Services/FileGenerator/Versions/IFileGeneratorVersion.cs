namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Services.FileGenerator.Context;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal interface IFileGeneratorVersion
    {
        List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions { get; }
        object GetDtoTemplateModel(FileGeneratorDtoContext dtoContext);
        object GetOptionTemplateModel(FileGeneratorOptionContext optionContext);
        object GetCrudTemplateModel(FileGeneratorCrudContext crudContext);
    }
}
