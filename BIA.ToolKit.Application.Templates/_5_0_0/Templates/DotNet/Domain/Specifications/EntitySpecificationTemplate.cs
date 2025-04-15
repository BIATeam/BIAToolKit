// <copyright file="PlaneSpecification.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Fleet.Specifications
{
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Specification;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;

    /// <summary>
    /// The specifications of the member entity.
    /// </summary>
    public static class PlaneSpecification
    {
        /// <summary>
        /// Search member using the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The specification.</returns>
        public static Specification<Plane> SearchGetAll(PagingFilterFormatDto filter)
        {
            Specification<Plane> specification = new TrueSpecification<Plane>();

            if (filter.ParentIds != null && filter.ParentIds.Length > 0)
            {
                specification &= new DirectSpecification<Plane>(s =>
                    s.SiteId == int.Parse(filter.ParentIds[0]));
            }

            return specification;
        }
    }
}
