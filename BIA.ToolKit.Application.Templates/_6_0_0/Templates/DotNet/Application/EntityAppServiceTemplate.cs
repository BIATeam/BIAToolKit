// <copyright file="MaintenanceTeamAppService.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.MaintenanceCompanies
{
    using System.Linq.Expressions;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Common.Exceptions;
    using BIA.Net.Core.Domain.Authentication;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.Service;
    using BIA.Net.Core.Domain.Specification;
    using BIA.Net.Core.Domain.User.Specifications;
    using TheBIADevCompany.BIADemo.Application.User;
    using TheBIADevCompany.BIADemo.Crosscutting.Common.Enum;
    using TheBIADevCompany.BIADemo.Domain.Dto.MaintenanceCompanies;
    using TheBIADevCompany.BIADemo.Domain.Dto.User;
    using TheBIADevCompany.BIADemo.Domain.MaintenanceCompanies.Entities;
    using TheBIADevCompany.BIADemo.Domain.MaintenanceCompanies.Mappers;
    using TheBIADevCompany.BIADemo.Domain.RepoContract;
    using TheBIADevCompany.BIADemo.Domain.User;

    /// <summary>
    /// The application service used for maintenanceTeam.
    /// </summary>
    public class MaintenanceTeamAppService : CrudAppServiceBase<MaintenanceTeamDto, MaintenanceTeam, int, PagingFilterFormatDto, MaintenanceTeamMapper>, IMaintenanceTeamAppService
    {
        /// <summary>
        /// The current AncestorTeamId.
        /// </summary>
        private readonly int currentAncestorTeamId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceTeamAppService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="principal">The claims principal.</param>
        public MaintenanceTeamAppService(
            ITGenericRepository<MaintenanceTeam, int> repository,
            IPrincipal principal)
            : base(repository)
        {
            this.FiltersContext.Add(
                AccessMode.Read,
                TeamAppService.ReadSpecification<MaintenanceTeam>(TeamTypeId.MaintenanceTeam, principal, TeamConfig.Config));

            this.FiltersContext.Add(
                AccessMode.Update,
                TeamAppService.UpdateSpecification<MaintenanceTeam, UserDataDto>(TeamTypeId.MaintenanceTeam, principal));
            var userData = (principal as BiaClaimsPrincipal).GetUserData<UserDataDto>();
            this.currentAncestorTeamId = userData != null ? userData.GetCurrentTeamId((int)TeamTypeId.Site) : 0;
        }

        /// <inheritdoc/>
        public override async Task<MaintenanceTeamDto> AddAsync(MaintenanceTeamDto dto, string mapperMode = null)
        {
            if (dto.SiteId != this.currentAncestorTeamId)
            {
                throw new ForbiddenException("Can only add MaintenanceTeam on current parent Team.");
            }

            return await base.AddAsync(dto, mapperMode);
        }

        /// <inheritdoc/>
#pragma warning disable S1006 // Method overrides should not change parameter defaults
        public override async Task<(IEnumerable<MaintenanceTeamDto> Results, int Total)> GetRangeAsync(PagingFilterFormatDto filters = null, int id = default, Specification<MaintenanceTeam> specification = null, Expression<Func<MaintenanceTeam, bool>> filter = null, string accessMode = "Read", string queryMode = "ReadList", string mapperMode = null, bool isReadOnlyMode = false)
#pragma warning restore S1006 // Method overrides should not change parameter defaults
        {
            specification ??= TeamAdvancedFilterSpecification<MaintenanceTeam>.Filter(filters);
            return await base.GetRangeAsync(filters, id, specification, filter, accessMode, queryMode, mapperMode, isReadOnlyMode);
        }
    }
}