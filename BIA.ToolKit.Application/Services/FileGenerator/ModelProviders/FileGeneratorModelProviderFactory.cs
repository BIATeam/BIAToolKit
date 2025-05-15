namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Helper;
    using Templates.Common.Interfaces;
    using Models_4_0_0 = Templates._4_0_0.Models;
    using Models_5_0_0 = Templates._5_0_0.Models;

    internal class FileGeneratorModelProviderFactory
    {
        private readonly List<IFileGeneratorModelProvider> modelProviders = [];

        public FileGeneratorModelProviderFactory(IConsoleWriter consoleWriter)
        {
            modelProviders.Add(new FileGeneratorModelProvider_4_0_0<
                Models_4_0_0.EntityDtoModel, 
                Models_4_0_0.EntityCrudModel, 
                Models_4_0_0.EntityOptionModel, 
                Models_4_0_0.PropertyDtoModel, 
                Models_4_0_0.PropertyCrudModel>(consoleWriter));

            modelProviders.Add(new FileGeneratorModelProvider_5_0_0<
                Models_5_0_0.EntityDtoModel, 
                Models_5_0_0.EntityCrudModel, 
                Models_5_0_0.EntityOptionModel, 
                Models_5_0_0.PropertyDtoModel, 
                Models_5_0_0.PropertyCrudModel>(consoleWriter));
        }

        public IFileGeneratorModelProvider GetFileGeneratorVersion(Version version)
        {
            return modelProviders.FirstOrDefault(x => x.CompatibleBiaFrameworkVersions.Any(y => y.Matches(version)));
        }
    }
}
