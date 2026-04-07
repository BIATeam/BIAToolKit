namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Templates._4_0_0.Models;
    using BIA.ToolKit.Common;

    internal class FileGeneratorModelProvider_4_0_0(IConsoleWriter consoleWriter) : FileGeneratorModelProviderBase<EntityDtoModel<PropertyDtoModel>, EntityCrudModel<PropertyCrudModel>, EntityOptionModel, PropertyDtoModel, PropertyCrudModel>(consoleWriter)
    {
        public override List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("4.*"),
        ];

        public override object GetCrudTemplateModel(FileGeneratorCrudContext crudContext)
        {
            throw new NotImplementedException();
        }

        public override object GetOptionTemplateModel(FileGeneratorOptionContext optionContext)
        {
            throw new NotImplementedException();
        }
    }
}
