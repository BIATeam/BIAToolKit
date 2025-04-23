// <copyright file="IMyCountryOptionAppService.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.AircraftMaintenanceCompany
{
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Domain.Dto.Option;

    /// <summary>
    /// The interface defining the application service for MyCountry option.
    /// </summary>
    public interface IMyCountryOptionAppService : IOptionAppServiceBase<OptionDto, int>
    {
    }
}