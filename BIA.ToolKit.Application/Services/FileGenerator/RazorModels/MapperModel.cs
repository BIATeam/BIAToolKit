namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MapperModel : DtoModel
    {
        public string EntityNamespace { get; set; }
        public string MapperName { get; set; }
    }
}
