namespace BIA.ToolKit.Application.Services.FileGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates;

    public class GenerationReport
    {
        public List<Manifest.Feature.Template> TemplatesIgnored { get; private set; } = [];
        public bool HasFailed { get; set; }
    }
}
