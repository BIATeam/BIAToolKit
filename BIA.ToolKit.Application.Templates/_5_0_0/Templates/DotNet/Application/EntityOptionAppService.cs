// <copyright file="MyEntityOptionAppService.cs" company="MyCompany">
//     Copyright (c) MyCompany. All rights reserved.
// </copyright>

namespace MyCompany.MyProject.Application.MyDomain
{
    using BIA.Net.Core.Application.Services;
    using BIA.Net.Core.Domain.Dto.Option;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.Service;
    using MyCompany.MyProject.Domain.MyDomain.Entities;
    using MyCompany.MyProject.MyDomain.Mappers;

    /// <summary>
    /// The application service used for myentity option.
    /// </summary>
    public class MyEntityOptionAppService : OptionAppServiceBase<OptionDto, MyEntity, int, MyEntityOptionMapper>, IMyEntityOptionAppService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyEntityOptionAppService"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        public MyEntityOptionAppService(ITGenericRepository<MyEntity, int> repository)
            : base(repository)
        {
        }
    }
}