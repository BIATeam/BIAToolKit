// <copyright file="IMaintenanceTeamAppService.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.MaintenanceCompanies
{
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Domain.Dto.Base;
    using TheBIADevCompany.BIADemo.Domain.Dto.MaintenanceCompanies;
    using TheBIADevCompany.BIADemo.Domain.MaintenanceCompanies.Entities;

    /// <summary>
    /// The interface defining the application service for maintenanceTeam.
    /// </summary>
    public interface IMaintenanceTeamAppService : ICrudAppServiceBase<MaintenanceTeamDto, MaintenanceTeam, int, PagingFilterFormatDto>
    {
    }
}