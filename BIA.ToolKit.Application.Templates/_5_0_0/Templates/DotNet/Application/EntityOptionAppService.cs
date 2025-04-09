// <copyright file="PlaneOptionAppService.cs" company="TheBIADevCompany">
//     Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Application.Fleet
{
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Domain.Dto.Option;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.Service;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;
    using TheBIADevCompany.BIADemo.Fleet.Mappers;

    /// <summary>
    /// The application service used for plane option.
    /// </summary>
    public class PlaneOptionAppService : OptionAppServiceBase<OptionDto, Plane, int, PlaneOptionMapper>, IPlaneOptionAppService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaneOptionAppService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        public PlaneOptionAppService(ITGenericRepository<Plane, int> repository)
            : base(repository)
        {
        }
    }
}