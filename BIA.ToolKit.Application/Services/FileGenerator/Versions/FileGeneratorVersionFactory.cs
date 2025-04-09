namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Helper;

    internal class FileGeneratorVersionFactory
    {
        private readonly List<IFileGeneratorVersion> fileGeneratorsVersion = [];

        public FileGeneratorVersionFactory(IConsoleWriter consoleWriter)
        {
            fileGeneratorsVersion.Add(new FileGeneratorVersion_4_0_0(consoleWriter));
            fileGeneratorsVersion.Add(new FileGeneratorVersion_5_0_0(consoleWriter));
        }

        public IFileGeneratorVersion GetFileGeneratorVersion(Version version)
        {
            return fileGeneratorsVersion.FirstOrDefault(x => x.CompatibleBiaFrameworkVersions.Any(y => y.Matches(version)));
        }
    }
}
