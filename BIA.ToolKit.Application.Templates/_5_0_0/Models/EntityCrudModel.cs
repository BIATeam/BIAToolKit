namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : Common.Models.EntityModel, IEntityCrudModel<TPropertyCrudModel>
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        protected readonly List<string> excludedPropertiesForBiaFieldConfigColumns = new List<string>
        {
            "Id",
            "IsFixed"
        };

        public bool IsTeam { get; set; }
        public bool HasAncestorTeam { get; set; }
        public string AncestorTeamName { get; set; }
        public string DisplayItemName { get; set; }
        public List<string> OptionItems { get; set; }
        public bool HasOptions => OptionItems?.Count > 0;
        public bool HasParent { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
        public List<TPropertyCrudModel> Properties { get; set; } = new List<TPropertyCrudModel>();
        public string AngularParentRelativePath { get; set; }
        public int AngularDeepLevel { get; set; }
        public string AngularDeepRelativePath
        {
            get
            {
                var pathBuilder = new StringBuilder();
                for (int i = 0; i < AngularDeepLevel; i++)
                {
                    pathBuilder.Append("../");
                }
                return pathBuilder.ToString();
            }
        }
        public IEnumerable<TPropertyCrudModel> BiaFieldConfigProperties => Properties.Where(p => !excludedPropertiesForBiaFieldConfigColumns.Contains(p.Name) && !p.IsParentIdentifier);
        public bool UseHubForClient {  get; set; }
        public bool HasCustomRepository {  get; set; }
    }
}
