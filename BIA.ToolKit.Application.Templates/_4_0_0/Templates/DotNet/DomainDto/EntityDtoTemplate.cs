// <copyright file="MyEntityDto.cs" company="MyCompany">
//     Copyright (c) MyCompany. All rights reserved.
// </copyright>

namespace MyCompany.MyProject.Domain.Dto.MyDomain
{
    using System;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.CustomAttribute;
    using BIA.Net.Core.Domain.Dto.Option;

    /// <summary>
    /// The DTO used to represent an MyEntity.
    /// </summary>
    public class MyEntityDto : BaseDto<int>
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
