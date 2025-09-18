namespace BIA.ToolKit.Application.Templates._5_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : Common.Models.EntityModel, IEntityCrudModel<TPropertyCrudModel>
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        protected List<string> excludedProperties;
        protected List<string> ExcludedProperties
        {
            get
            {
                if (excludedProperties == null)
                {
                    excludedProperties = new List<string>()
                    {
                        "Id",
                        "DtoState"
                    };

                    if (IsVersioned)
                    {
                        excludedProperties.Add("RowVersion");
                    }

                    if (IsArchivable)
                    {
                        excludedProperties.Add("IsArchived");
                        excludedProperties.Add("ArchivedDate");
                    }

                    if (IsFixable)
                    {
                        excludedProperties.Add("IsFixed");
                        excludedProperties.Add("FixedDate");
                    }

                    if (IsTeam)
                    {
                        excludedProperties.Add("Title");
                        excludedProperties.Add("TeamTypeId");
                        excludedProperties.Add("IsDefault");
                        excludedProperties.Add("Roles");
                        excludedProperties.Add("ParentTeamId");
                        excludedProperties.Add("ParentTeamTitle");
                        excludedProperties.Add("CanUpdate");
                        excludedProperties.Add("CanMemberListAccess");
                        excludedProperties.Add("Admins");
                    }
                }

                return excludedProperties;
            }
        }

        public bool IsTeam { get; set; }
        public bool HasAncestorTeam { get; set; }
        public string AncestorTeamName { get; set; }
        public string DisplayItemName { get; set; }
        public List<string> OptionItems { get; set; }
        public bool HasOptionItems => OptionItems != null && OptionItems.Any();
        public bool HasOptions => Properties.Any(p => p.IsOption);
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
        public IEnumerable<TPropertyCrudModel> PropertiesToGenerate => Properties.Where(p => !ExcludedProperties.Contains(p.Name));
        public IEnumerable<TPropertyCrudModel> BiaFieldConfigProperties => PropertiesToGenerate.Where(p => !p.IsParentIdentifier);
        public bool UseHubForClient {  get; set; }
        public bool HasCustomRepository {  get; set; }
        public bool HasReadOnlyMode { get; set; }
        public bool HasFixableParent { get; set; }
        public bool IsFixable { get; set; }
        public bool IsVersioned { get; set; }
        public bool IsArchivable { get; set; }

        public string HubForClientParentKey => GetHubForClientParentKey();
        public bool HasHubForClientParentKey => HasParent || HasAncestorTeam || IsTeam;

        public string GetHubForClientParentKey()
        {
            var parentKeyName = HasParent ? ParentName 
                : HasAncestorTeam ? AncestorTeamName 
                : "ParentTeam";
            return $"{parentKeyName}Id";
        }

        public bool HasAdvancedFilter { get; set; }
        public bool CanImport { get; set; }
        public int TeamTypeId { get; set; }
        public int TeamRoleId { get; set; }
        public string FormReadOnlyMode { get; set; }

        public virtual string AngularModelInterfaceInheritance
        {
            get
            {
                var interfaces = new List<string> { "BaseDto" };
                if (IsVersioned)
                    interfaces.Add("VersionedDto");
                if (IsTeam)
                    interfaces.Add("TeamDto");
                if (IsFixable)
                    interfaces.Add("FixableDto");
                if (IsArchivable)
                    interfaces.Add("ArchivableDto");
                return string.Join(", ", interfaces);
            }
        }
    }
}
