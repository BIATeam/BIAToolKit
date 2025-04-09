namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Templates._4_0_0.Models;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;

    internal class FileGeneratorVersion_5_0_0(IConsoleWriter consoleWriter) : FileGeneratorVersion_4_0_0(consoleWriter), IFileGeneratorVersion
    {
        public new List<BiaFrameworkVersion> CompatibleBiaFrameworkVersions =>
        [
            new("5.*"),
        ];
    }
}
