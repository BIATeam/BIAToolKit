// <copyright file="MyCountryOptionAppService.cs" company="TheBIADevCompany">
//     Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.AircraftMaintenanceCompany
{
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Domain.Dto.Option;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.Service;
    using TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Entities;
    using TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Mappers;

    /// <summary>
    /// The application service used for MyCountry option.
    /// </summary>
    public class MyCountryOptionAppService : OptionAppServiceBase<OptionDto, MyCountry, int, MyCountryOptionMapper>, IMyCountryOptionAppService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyCountryOptionAppService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        public MyCountryOptionAppService(ITGenericRepository<MyCountry, int> repository)
            : base(repository)
        {
        }
    }
}