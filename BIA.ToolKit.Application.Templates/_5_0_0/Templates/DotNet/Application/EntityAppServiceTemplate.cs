// <copyright file="PlaneAppService.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.Plane
{
    using System.Security.Principal;
    using System.Threading.Tasks;
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Common.Exceptions;
    using BIA.Net.Core.Domain.Authentication;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.User;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.Service;
    using BIA.Net.Core.Domain.Specification;
    using TheBIADevCompany.BIADemo.Crosscutting.Common.Enum;
    using TheBIADevCompany.BIADemo.Domain.Dto.Plane;
    using TheBIADevCompany.BIADemo.Domain.Plane.Entities;
    using TheBIADevCompany.BIADemo.Domain.Plane.Mappers;
    using TheBIADevCompany.BIADemo.Domain.RepoContract;

    /// <summary>
    /// The application service used for plane.
    /// </summary>
    public class PlaneAppService : CrudAppServiceBase<PlaneDto, Plane, int, PagingFilterFormatDto, PlaneMapper>, IPlaneAppService
    {
        /// <summary>
        /// The current AncestorTeamId.
        /// </summary>
        private readonly int currentAncestorTeamId;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaneAppService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="principal">The claims principal.</param>
        public PlaneAppService(
            ITGenericRepository<Plane, int> repository,
            IPrincipal principal)
            : base(repository)
        {
            var userData = (principal as BiaClaimsPrincipal).GetUserData<UserDataDto>();
            this.currentAncestorTeamId = userData != null ? userData.GetCurrentTeamId((int)TeamTypeId.Site) : 0;

            // For child : set the TeamId of the Ancestor that contain a team Parent
            this.FiltersContext.Add(AccessMode.Read, new DirectSpecification<Plane>(x => x.SiteId == this.currentAncestorTeamId));

            this.FiltersContext.Add(
                AccessMode.Read,
                TeamAppService.ReadSpecification<Plane>(TeamTypeId.Plane, principal));

            this.FiltersContext.Add(
                AccessMode.Update,
                TeamAppService.UpdateSpecification<Plane>(TeamTypeId.Plane, principal));
        }

        /// <inheritdoc/>
#pragma warning disable S1006 // Method overrides should not change parameter defaults
        public override Task<(IEnumerable<PlaneDto> Results, int Total)> GetRangeAsync(PagingFilterFormatDto filters = null, int id = default, Specification<Plane> specification = null, Expression<Func<Plane, bool>> filter = null, string accessMode = "Read", string queryMode = "ReadList", string mapperMode = null, bool isReadOnlyMode = false)
#pragma warning restore S1006 // Method overrides should not change parameter defaults
        {
            specification ??= PlaneSpecification.SearchGetAll(filters);
            return base.GetRangeAsync(filters, id, specification, filter, accessMode, queryMode, mapperMode, isReadOnlyMode);
        }

        /// <inheritdoc/>
#pragma warning disable S1006 // Method overrides should not change parameter defaults
        public override Task<byte[]> GetCsvAsync(PagingFilterFormatDto filters = null, int id = default, Specification<Plane> specification = null, Expression<Func<Plane, bool>> filter = null, string accessMode = "Read", string queryMode = "ReadList", string mapperMode = null, bool isReadOnlyMode = false)
#pragma warning restore S1006 // Method overrides should not change parameter defaults
        {
            specification ??= PlaneSpecification.SearchGetAll(filters);
            return base.GetCsvAsync(filters, id, specification, filter, accessMode, queryMode, mapperMode, isReadOnlyMode);
        }

        /// <inheritdoc/>
        public override Task<PlaneDto> AddAsync(PlaneDto dto, string mapperMode = null)
        {
            dto.SiteId = this.currentAncestorTeamId;
            return base.AddAsync(dto, mapperMode);
        }
    }
}