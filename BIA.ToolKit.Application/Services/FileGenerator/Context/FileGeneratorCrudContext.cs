namespace BIA.ToolKit.Application.Services.FileGenerator.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;

    public sealed class FileGeneratorCrudContext : FileGeneratorContext
    {
        public string DisplayItemName { get; set; }
        public List<string> OptionItems { get; set; } = [];
        public List<PropertyInfo> Properties { get; set; } = [];
    }
}
