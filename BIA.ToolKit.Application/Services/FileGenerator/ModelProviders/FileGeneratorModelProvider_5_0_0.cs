namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Templates._5_0_0.Models;
    using BIA.ToolKit.Common;

    internal class FileGeneratorModelProvider_5_0_0(IConsoleWriter consoleWriter) : FileGeneratorModelProviderBase<EntityDtoModel<PropertyDtoModel>, EntityCrudModel<PropertyCrudModel>, EntityOptionModel, PropertyDtoModel, PropertyCrudModel>(consoleWriter)
    {
        public override List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("5.*"),
        ];
    }
}
