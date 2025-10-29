namespace BIA.ToolKit.Application.Services.FileGenerator.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.DtoGenerator;

    public sealed class FileGeneratorCrudContext : FileGeneratorContext
    {
        public string DisplayItemName { get; set; }
        public List<string> OptionItems { get; set; } = [];
        public List<PropertyInfo> Properties { get; set; } = [];
        public bool UseHubForClient { get; set; }
        public bool HasCustomRepository { get; set; }
        public bool HasReadOnlyMode { get; set; }
        public bool HasFixableParent { get; set; }
        public bool IsFixable { get; set; }
        public bool IsVersioned { get; set; }
        public bool IsArchivable { get; set; }
        public bool HasAdvancedFilter { get; set; }
        public bool CanImport { get; set; }
        public int TeamTypeId { get; set; }
        public int TeamRoleId { get; set; }
        public string FormReadOnlyMode { get; set; }
        public bool DisplayHistorical { get; set; }
        public bool UseDomainUrl { get; set; }
    }
}
