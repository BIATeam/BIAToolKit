﻿namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates.Common.Enum;
    using BIA.ToolKit.Application.Templates.Common.Interfaces;

    public class EntityCrudModel<TPropertyCrudModel> : Common.Models.EntityModel, IEntityCrudModel<TPropertyCrudModel>
        where TPropertyCrudModel : class, IPropertyCrudModel
    {
        public string AncestorTeamName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int AngularDeepLevel { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string AngularDeepRelativePath => throw new System.NotImplementedException();

        public string AngularParentRelativePath { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public IEnumerable<TPropertyCrudModel> BiaFieldConfigProperties => throw new System.NotImplementedException();

        public string DisplayItemName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool HasAncestorTeam { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public bool HasOptionItems => throw new System.NotImplementedException();
        public bool HasOptions => throw new System.NotImplementedException();

        public bool HasParent { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsTeam { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public List<string> OptionItems { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string ParentName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string ParentNamePlural { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public List<TPropertyCrudModel> Properties { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool UseHubForClient { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool HasCustomRepository { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool HasReadOnlyMode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool HasFixableParent { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsFixable { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string HubForClientParentKey { get => throw new System.NotImplementedException(); }
        public bool HasHubForClientParentKey { get => throw new System.NotImplementedException(); }
        public bool HasAdvancedFilter { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CanImport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int TeamTypeId { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int TeamRoleId { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string FormReadOnlyMode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsVersioned { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsArchivable { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
