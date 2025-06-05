namespace BIA.ToolKit.Application.Services.FileGenerator.ModelProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Application.Helper;

    internal class FileGeneratorModelProviderFactory
    {
        private readonly List<IFileGeneratorModelProvider> modelProviders = [];

        public FileGeneratorModelProviderFactory(IConsoleWriter consoleWriter)
        {
            modelProviders.Add(new FileGeneratorModelProvider_4_0_0(consoleWriter));
            modelProviders.Add(new FileGeneratorModelProvider_5_0_0(consoleWriter));
        }

        public IFileGeneratorModelProvider GetModelProvider(Version version)
        {
            return modelProviders.FirstOrDefault(x => x.CompatibleBiaFrameworkVersions.Any(y => y.Matches(version)));
        }
    }
}
