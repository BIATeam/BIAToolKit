// <copyright file="EntityOptionsController.cs" company="Company">
//     Copyright (c) Company. All rights reserved.
// </copyright>

namespace Company.Project.Presentation.Api.Controllers.Domain
{
    using System.Threading.Tasks;
    using BIA.Net.Presentation.Api.Controllers.Base;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Company.Project.Application.Domain;
    using Company.Project.Crosscutting.Common;

    /// <summary>
    /// The API controller used to manage entity options.
    /// </summary>
    public class EntityOptionsController : BiaControllerBase
    {
        /// <summary>
        /// The entity application service.
        /// </summary>
        private readonly IEntityOptionAppService entityOptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityOptionsController"/> class.
        /// </summary>
        /// <param name="entityOptionService">The entity application service.</param>
        public EntityOptionsController(IEntityOptionAppService entityOptionService)
        {
            this.entityOptionService = entityOptionService;
        }

        /// <summary>
        /// Gets all option that I can see.
        /// </summary>
        /// /// <returns>The list of entities.</returns>
        [HttpGet("allOptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = Rights.EntityOptions.Options)]
        public async Task<IActionResult> GetAllOptions()
        {
            var results = await this.entityOptionService.GetAllOptionsAsync();
            return this.Ok(results);
        }
    }
}