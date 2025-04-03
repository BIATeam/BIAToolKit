namespace BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.BiaFrameworkFileGenerator._4_0_0;

    internal class BiaFrameworkFileGeneratorFactory
    {
        private readonly List<IBiaFrameworkFileGenerator> biaFrameworkFileGenerators = new();

        public BiaFrameworkFileGeneratorFactory(BiaFrameworkFileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
        {
            biaFrameworkFileGenerators.Add(new BiaFrameworkFileGenerator_4_0_0(fileGeneratorService, consoleWriter));
        }

        public IBiaFrameworkFileGenerator GetBiaFrameworkFileGenerator(Version version)
        {
            return biaFrameworkFileGenerators.FirstOrDefault(x => x.CompatibleBiaFrameworkVersions.Any(y => y.Matches(version)));
        }
    }
}
