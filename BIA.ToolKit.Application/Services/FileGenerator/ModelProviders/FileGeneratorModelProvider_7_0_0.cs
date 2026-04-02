namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Templates._7_0_0.Models;
    using BIA.ToolKit.Common;

    internal class FileGeneratorModelProvider_7_0_0(IConsoleWriter consoleWriter) : FileGeneratorModelProviderBase<EntityDtoModel<PropertyDtoModel>, EntityCrudModel<PropertyCrudModel>, EntityOptionModel, PropertyDtoModel, PropertyCrudModel>(consoleWriter)
    {
        public override List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("7.*"),
        ];
    }
}
