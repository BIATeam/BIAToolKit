// <copyright file="PlaneDto.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Dto.Fleet
{
    using System;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.CustomAttribute;
    using BIA.Net.Core.Domain.Dto.Option;

    /// <summary>
    /// The DTO used to represent a Plane.
    /// </summary>
    [BiaDtoClass(AncestorTeam = "Site")]
    public class PlaneDto : BaseDto<int>
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        [BiaDtoField(Required = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Option.
        /// </summary>
        [BiaDtoField(Required = false, ItemType = "Option")]
        public OptionDto Option { get; set; }
    }
}
