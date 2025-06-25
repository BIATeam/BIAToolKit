// <copyright file="MyCountryOptionMapper.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Mappers
{
    using System;
    using System.Linq.Expressions;
    using BIA.Net.Core.Common.Extensions;
    using BIA.Net.Core.Domain.Dto.Option;
    using BIA.Net.Core.Domain.Mapper;
    using TheBIADevCompany.BIADemo.Domain.AircraftMaintenanceCompany.Entities;

    /// <summary>
    /// The mapper used for my country option.
    /// </summary>
    public class MyCountryOptionMapper : BaseMapper<OptionDto, MyCountry, int>
    {
        /// <inheritdoc />
        public override Expression<Func<MyCountry, OptionDto>> EntityToDto()
        {
            return base.EntityToDto().CombineMapping(entity => new OptionDto
            {
                Display = entity.Name,
            });
        }
    }
}
