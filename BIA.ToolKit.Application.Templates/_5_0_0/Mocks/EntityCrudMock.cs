﻿namespace BIA.ToolKit.Application.Templates._5_0_0.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates._5_0_0.Models;

    public class EntityCrudMock : EntityCrudModel
    {
        public EntityCrudMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "Fleet";
            EntityName = "Plane";
            BaseKeyType = "int";
            EntityNamePlural = "Planes";
            IsTeam = true;
            HasAncestorTeam = true;
            AncestorTeamName = "Site";
            DisplayItemName = "Name";
            OptionItems = new List<string> { "Engine", "Airport", "PlaneType" };
            HasParent = true;
            ParentName = "Site";
            ParentNamePlural = "Sites";
        }
    }
}
