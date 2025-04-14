namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EntityCrudModel : EntityModel
    {
        public bool IsTeam { get; set; }
        public bool HasAncestorTeam { get; set; }
        public string AncestorTeamName { get; set; }
        public string DisplayItemName { get; set; }
        public List<string> OptionItems { get; set; }
        public bool HasParent { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
    }
}
