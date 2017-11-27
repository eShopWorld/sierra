namespace Sierra.Api.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    /// <summary>
    /// Manages tenants in the platform.
    /// </summary>
    [Route("/v1/tenants")]
    public class TenantsController : Controller
    {
        /// <summary>
        /// Gets all the tenant information from the platform.
        /// </summary>
        /// <returns>The list with all the tenants on the platform.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Tenant>), 200)]
        public IEnumerable<Tenant> Get()
        {
            return new Tenant[] {};
        }

        /// <summary>
        /// Gets a specific tenant from the platform.
        /// </summary>
        /// <param name="id">The ID of the tenant we want to get.</param>
        /// <returns>The tenant data for the tenant ID that we want to get.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Tenant), 200)]
        public Tenant Get(int id)
        {
            return new Tenant();
        }

        /// <summary>
        /// Updates a tenant in the platform
        /// </summary>
        /// <param name="tenant">The tenant that we want to update.</param>
        [HttpPost]
        public void Post([FromBody]Tenant tenant)
        {
        }

        /// <summary>
        /// Adds a tenant to the platform.
        /// </summary>
        /// <param name="tenant">The tenant that we want to add to the platform.</param>
        [HttpPut]
        public void Put([FromBody]Tenant tenant)
        {
        }

        /// <summary>
        /// Removes a tenant from the platform.
        /// </summary>
        /// <param name="id">The ID of the tenant that we want to remove.</param>
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
        }
    }
}
