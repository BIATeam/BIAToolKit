namespace BIA.ToolKit.Application.Templates.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityOptionModel : EntityModel, IEntityOptionModel
    {
        public string OptionDisplayName { get; set; }
    }
}
