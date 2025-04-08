namespace BIA.ToolKit.Application.Services.FileGenerator.Versions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Common;

    internal abstract class FileGeneratorVersionBase
    {
        protected abstract string TemplateFolderPath { get; }
    }
}
