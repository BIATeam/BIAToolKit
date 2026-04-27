// <copyright file="Site.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Site.Entities
{
    using Audit.EntityFramework;
    using BIA.Net.Core.Common.Attributes;
    using BIA.Net.Core.Common.Enum;
    using BIA.Net.Core.Domain.User.Entities;

    /// <summary>
    /// The site entity.
    /// </summary>
    public class Site : BaseEntityTeam
    {
        /// <summary>
        /// Add row version timestamp in table Site.
        /// </summary>
        [BiaRowVersionProperty(DbProvider.SqlServer)]
        [AuditIgnore]
        public byte[] RowVersionSite { get; set; }

        /// <summary>
        /// Add row version for Postgre in table Site.
        /// </summary>
        [BiaRowVersionProperty(DbProvider.PostGreSql)]
        [AuditIgnore]
        public uint RowVersionXminSite { get; set; }
    }
}