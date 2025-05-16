// <copyright file="MyCountryOptionsController.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Presentation.Api.Controllers.AircraftMaintenanceCompany
{
    using System.Threading.Tasks;
    using BIA.Net.Presentation.Api.Controllers.Base;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using TheBIADevCompany.BIADemo.Application.AircraftMaintenanceCompany;
    using TheBIADevCompany.BIADemo.Crosscutting.Common;

    /// <summary>
    /// The API controller used to manage MyCountry options.
    /// </summary>
    public class MyCountryOptionsController : BiaControllerBase
    {
        /// <summary>
        /// The MyCountry application service.
        /// </summary>
        private readonly IMyCountryOptionAppService myCountryOptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyCountryOptionsController"/> class.
        /// </summary>
        /// <param name="myCountryOptionService">The my country application service.</param>
        public MyCountryOptionsController(IMyCountryOptionAppService myCountryOptionService)
        {
            this.myCountryOptionService = myCountryOptionService;
        }

        /// <summary>
        /// Gets all option that I can see.
        /// </summary>
        /// /// <returns>The list of my countries.</returns>
        [HttpGet("allOptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = Rights.MyCountryOptions.Options)]
        public async Task<IActionResult> GetAllOptions()
        {
            var results = await this.myCountryOptionService.GetAllOptionsAsync();
            return this.Ok(results);
        }
    }
}