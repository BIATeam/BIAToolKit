namespace BIA.ToolKit.Application.Services.FileGenerator.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.ViewModel;

    public sealed class FileGeneratorDtoContext : FileGeneratorContext
    {
        public List<MappingEntityProperty> Properties { get; set; } = [];
    }
}
