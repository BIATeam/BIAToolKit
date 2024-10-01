namespace BIA.ToolKit.Application.Services.FileGenerator.RazorModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DtoModel : IModel
    {
        public string CompanyName { get; set; }
        public string ProjectName { get; set; }
    }
}
