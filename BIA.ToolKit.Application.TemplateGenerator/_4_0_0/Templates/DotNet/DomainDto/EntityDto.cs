// <copyright file="EntityDto.cs" company="Company">
//     Copyright (c) Company. All rights reserved.
// </copyright>

namespace Company.Project.Domain.Dto.Domain
{
    using System;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.CustomAttribute;
    using BIA.Net.Core.Domain.Dto.Option;

    /// <summary>g
    /// The DTO used to represent an Entity.
    /// </summary>
    public class EntityDto : BaseDto<int>
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
