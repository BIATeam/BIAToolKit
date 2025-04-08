namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Helper;

    internal class FileGeneratorVersionFactory
    {
        private readonly List<IFileGeneratorVersion> biaFrameworkFileGenerators = new();

        public FileGeneratorVersionFactory(IConsoleWriter consoleWriter)
        {
            biaFrameworkFileGenerators.Add(new FileGeneratorVersion_4_0_0(consoleWriter));
        }

        public IFileGeneratorVersion GetBiaFrameworkFileGenerator(Version version)
        {
            return biaFrameworkFileGenerators.FirstOrDefault(x => x.CompatibleBiaFrameworkVersions.Any(y => y.Matches(version)));
        }
    }
}
