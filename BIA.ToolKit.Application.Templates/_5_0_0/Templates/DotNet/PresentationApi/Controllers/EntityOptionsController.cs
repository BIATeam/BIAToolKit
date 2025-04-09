// <copyright file="MyEntityOptionsController.cs" company="MyCompany">
//     Copyright (c) MyCompany. All rights reserved.
// </copyright>

namespace MyCompany.MyProject.Presentation.Api.Controllers.MyDomain
{
    using System.Threading.Tasks;
    using BIA.Net.Presentation.Api.Controllers.Base;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using MyCompany.MyProject.Application.MyDomain;
    using MyCompany.MyProject.Crosscutting.Common;

    /// <summary>
    /// The API controller used to manage myentity options.
    /// </summary>
    public class MyEntityOptionsController : BiaControllerBase
    {
        /// <summary>
        /// The myentity application service.
        /// </summary>
        private readonly IMyEntityOptionAppService myentityOptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyEntityOptionsController"/> class.
        /// </summary>
        /// <param name="myentityOptionService">The myentity application service.</param>
        public MyEntityOptionsController(IMyEntityOptionAppService myentityOptionService)
        {
            this.myentityOptionService = myentityOptionService;
        }

        /// <summary>
        /// Gets all option that I can see.
        /// </summary>
        /// /// <returns>The list of myentities.</returns>
        [HttpGet("allOptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = Rights.MyEntityOptions.Options)]
        public async Task<IActionResult> GetAllOptions()
        {
            var results = await this.myentityOptionService.GetAllOptionsAsync();
            return this.Ok(results);
        }
    }
}