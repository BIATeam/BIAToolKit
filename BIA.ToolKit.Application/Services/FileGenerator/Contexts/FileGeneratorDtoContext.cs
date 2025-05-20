namespace BIA.ToolKit.Application.Services.FileGenerator.Contexts
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
        public bool IsArchivable { get; set; }
        public bool IsFixable { get; set; }
        public bool IsVersioned { get; set; }
    }
}
