// BIADemo only
// <copyright file="SiteDomainModelBuilder.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Infrastructure.Data.ModelBuilders
{
    using Microsoft.EntityFrameworkCore;
    using TheBIADevCompany.BIADemo.Domain.SiteDomain.Entities;

    /// <summary>
    /// Class used to update the model builder for site domain domain.
    /// </summary>
    public static class SiteDomainModelBuilder
    {
        /// <summary>
        /// Create the model for sites.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        public static void CreateSiteModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Site>().ToTable("Sites");
        }
    }
}