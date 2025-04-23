// <copyright file="MaintenanceTeamSpecification.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.MaintenanceCompanies.Specifications
{
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Specification;
    using TheBIADevCompany.BIADemo.Domain.MaintenanceCompanies.Entities;

    /// <summary>
    /// The specifications of the member entity.
    /// </summary>
    public static class MaintenanceTeamSpecification
    {
        /// <summary>
        /// Search member using the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The specification.</returns>
        public static Specification<MaintenanceTeam> SearchGetAll(PagingFilterFormatDto filter)
        {
            Specification<MaintenanceTeam> specification = new TrueSpecification<MaintenanceTeam>();

            if (filter.ParentIds != null && filter.ParentIds.Length > 0)
            {
                specification &= new DirectSpecification<MaintenanceTeam>(s =>
                    s.AircraftMaintenanceCompanyId == int.Parse(filter.ParentIds[0]));
            }

            return specification;
        }
    }
}
