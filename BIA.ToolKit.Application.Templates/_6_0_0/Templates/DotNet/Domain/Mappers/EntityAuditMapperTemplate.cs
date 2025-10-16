// <copyright file="PlaneAuditMapper.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Fleet.Mappers
{
    using BIA.Net.Core.Domain.Mapper;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;

    /// <summary>
    /// Audit mapper for <see cref="Plane"/>.
    /// </summary>
    public class PlaneAuditMapper : AuditMapper<Plane>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaneAuditMapper"/> class.
        /// </summary>
        public PlaneAuditMapper()
        {
        }
    }
}
